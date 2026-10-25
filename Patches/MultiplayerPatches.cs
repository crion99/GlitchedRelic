using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MysteriousMapMod.Map;
using MysteriousMapMod.Rooms;

namespace MysteriousMapMod.Patches;

[HarmonyPatch(typeof(RunManager), "InitializeShared")]
internal static class InitializeShopInventoryMultiplayerSyncPatch
{
    [HarmonyPostfix]
    private static void Postfix(RunManager __instance)
    {
        ShopInventoryMultiplayerSynchronizer.Initialize(__instance);
        DataminerMultiplayerSynchronizer.Initialize(__instance);
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.CleanUp))]
internal static class DisposeShopInventoryMultiplayerSyncPatch
{
    [HarmonyPrefix]
    private static void Prefix()
    {
        ShopInventoryMultiplayerSynchronizer.DisposeActive();
        DataminerMultiplayerSynchronizer.DisposeActive();
        MysteriousMapSettings.ClearMultiplayerRevealSettings();
    }
}

/// <summary>
/// Adds the host's map-mode choice to the already synchronized lobby modifier
/// payload. Clients therefore generate the same map even if their local total
/// control setting differs from the host's setting.
/// </summary>
[HarmonyPatch(typeof(StartRunLobby), "BeginRunForAllPlayers")]
internal static class SyncMysteriousMapModeIntoLobbyPatch
{
    [HarmonyPrefix]
    private static void Prefix(StartRunLobby __instance, List<ModifierModel> modifiers)
    {
        if (__instance.NetService.Type != NetGameType.Host)
            return;

        modifiers.RemoveAll(modifier => modifier is MysteriousMapMultiplayerMarkerModifier);
        if (!MysteriousMapSettings.Enabled)
            return;

        var marker = (MysteriousMapMultiplayerMarkerModifier)
            ModelDb.Modifier<MysteriousMapMultiplayerMarkerModifier>().ToMutable();
        marker.RevealOptionsMask = MysteriousMapSettings.CaptureLocalRevealSettings().ToMask();
        marker.StartWithDataminer = MysteriousMapSettings.StartWithDataminer;
        modifiers.Add(marker);
        Log.Info($"[MysteriousMap] Host synchronized map mode and reveal settings " +
                 $"(mask={marker.RevealOptionsMask}, dataminer={marker.StartWithDataminer}) " +
                 "into the multiplayer start payload.");
    }
}

/// <summary>
/// The standard character-select listener intentionally starts runs with an
/// empty modifier list. Preserve only our transient multiplayer carrier across
/// that boundary, while keeping it out of the listener's vanilla standard-run
/// validation.
/// </summary>
[HarmonyPatch(typeof(StartRunLobby), "BeginRunLocally")]
internal static class MysteriousMapMultiplayerMarkerTransportPatch
{
    private sealed class PendingMarker(MysteriousMapMultiplayerMarkerModifier marker)
    {
        internal MysteriousMapMultiplayerMarkerModifier Marker { get; } = marker;
    }

    private static readonly ConditionalWeakTable<StartRunLobby, PendingMarker> Pending = new();
    private static readonly object Gate = new();

    [HarmonyPrefix]
    private static void Prefix(StartRunLobby __instance, ref List<ModifierModel> modifiers)
    {
        CaptureForLobbyListener(__instance, ref modifiers);
    }

    internal static void CaptureForLobbyListener(
        StartRunLobby lobby,
        ref List<ModifierModel> modifiers,
        bool writeLog = true)
    {
        var marker = modifiers.OfType<MysteriousMapMultiplayerMarkerModifier>().LastOrDefault();
        if (marker is null)
            return;

        lock (Gate)
        {
            Pending.Remove(lobby);
            Pending.Add(lobby, new PendingMarker(marker));
        }

        modifiers = modifiers
            .Where(modifier => modifier is not MysteriousMapMultiplayerMarkerModifier)
            .ToList();
        if (writeLog)
            Log.Info("[MysteriousMap] Captured the multiplayer map-mode carrier before the lobby listener.");
    }

    internal static IReadOnlyList<ModifierModel> ReattachForRunCreation(
        StartRunLobby lobby,
        IReadOnlyList<ModifierModel> modifiers,
        bool writeLog = true)
    {
        if (modifiers.OfType<MysteriousMapMultiplayerMarkerModifier>().Any())
            return modifiers;

        MysteriousMapMultiplayerMarkerModifier? marker;
        lock (Gate)
        {
            if (!Pending.TryGetValue(lobby, out var pending))
                return modifiers;

            Pending.Remove(lobby);
            marker = pending.Marker;
        }

        if (writeLog)
            Log.Info("[MysteriousMap] Reattached the host map-mode carrier at multiplayer run creation.");
        return modifiers.Append(marker).ToArray();
    }
}

[HarmonyPatch(typeof(NGame), nameof(NGame.StartNewMultiplayerRun))]
internal static class RestoreMysteriousMapMultiplayerMarkerAtRunCreationPatch
{
    [HarmonyPrefix]
    private static void Prefix(
        StartRunLobby lobby,
        ref IReadOnlyList<ModifierModel> modifiers)
    {
        modifiers = MysteriousMapMultiplayerMarkerTransportPatch
            .ReattachForRunCreation(lobby, modifiers);
    }
}

[HarmonyPatch(typeof(RunState), nameof(RunState.CreateForNewRun))]
internal static class RecognizeNewMultiplayerModePatch
{
    [HarmonyPostfix]
    private static void Postfix(IReadOnlyList<ModifierModel> modifiers, RunState __result)
    {
        if (!MysteriousMapRunMode.HasRuntimeMarker(modifiers))
            return;

        var revealSettings = MysteriousMapRunMode.ReadRuntimeRevealSettings(modifiers);
        var startWithDataminer = MysteriousMapRunMode.ReadRuntimeStartingDataminer(modifiers);
        MysteriousMapRunMode.MarkSynced(__result, revealSettings, startWithDataminer);
        MysteriousMapSettings.ApplyMultiplayerRevealSettings(revealSettings);
        // Keep the transport marker until RunManager.InitializeNewRun invokes
        // its native ModifierModel.AfterRunCreated lifecycle. That provides a
        // second, non-Harmony activation path before map generation.
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.ToSave))]
internal static class SaveMultiplayerMapModePatch
{
    [HarmonyPostfix]
    private static void Postfix(RunManager __instance, SerializableRun __result)
    {
        var state = __instance.DebugOnlyGetState();
        if (state != null)
            MysteriousMapRunMode.SetSerializedMarker(
                __result,
                MysteriousMapRunMode.IsSyncedMultiplayerRun(state),
                MysteriousMapRunMode.GetRevealSettings(state));
    }
}

[HarmonyPatch(typeof(RunState), nameof(RunState.FromSerializable))]
internal static class RestoreMultiplayerMapModePatch
{
    [HarmonyPrefix]
    private static void Prefix(
        SerializableRun save,
        out (bool HasMarker, MysteriousMapRevealSettings RevealSettings) __state)
    {
        __state = (
            MysteriousMapRunMode.HasSerializedMarker(save),
            MysteriousMapRunMode.ReadSerializedRevealSettings(save));
    }

    [HarmonyPostfix]
    private static void Postfix(
        RunState __result,
        (bool HasMarker, MysteriousMapRevealSettings RevealSettings) __state)
    {
        if (__state.HasMarker)
        {
            MysteriousMapRunMode.MarkSynced(__result, __state.RevealSettings);
            MysteriousMapSettings.ApplyMultiplayerRevealSettings(__state.RevealSettings);
        }
        // Saved-run setup invokes ModifierModel.AfterRunLoaded before restoring
        // the saved map. The marker removes itself from runtime modifiers there.
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.CanonicalizeSave))]
internal static class PreserveMultiplayerMapModeDuringCanonicalizationPatch
{
    [HarmonyPrefix]
    private static void Prefix(
        SerializableRun save,
        out (bool HasMarker, MysteriousMapRevealSettings RevealSettings) __state)
    {
        __state = (
            MysteriousMapRunMode.HasSerializedMarker(save),
            MysteriousMapRunMode.ReadSerializedRevealSettings(save));
    }

    [HarmonyPostfix]
    private static void Postfix(
        SerializableRun __result,
        (bool HasMarker, MysteriousMapRevealSettings RevealSettings) __state)
    {
        MysteriousMapRunMode.SetSerializedMarker(
            __result,
            __state.HasMarker,
            __state.RevealSettings);
    }
}

/// <summary>
/// VoteForMapCoordAction runs on every peer. Validate its destination before
/// MapSelectionSynchronizer mutates the vote set or lets the host choose a
/// winner, keeping host and clients on the same legal frontier.
/// </summary>
[HarmonyPatch(typeof(MapSelectionSynchronizer), nameof(MapSelectionSynchronizer.PlayerVotedForMapCoord))]
internal static class ValidateMysteriousMapVotePatch
{
    private static readonly FieldInfo RunStateField =
        AccessTools.Field(typeof(MapSelectionSynchronizer), "_runState");

    [HarmonyPrefix]
    private static bool Prefix(
        MapSelectionSynchronizer __instance,
        Player player,
        MapLocation source,
        MapVote? destination)
    {
        if (!destination.HasValue)
            return true;

        var runState = (RunState)RunStateField.GetValue(__instance)!;
        if (!MysteriousMapRules.IsActive(runState.Map))
            return true;

        var netType = RunManager.Instance.NetService.Type;
        var debugTravel = netType == NetGameType.Singleplayer &&
                          (NMapScreen.Instance?.IsDebugTravelEnabled ?? false);
        if (!MysteriousMapRules.CanEnter(runState, destination.Value.coord, debugTravel))
        {
            Log.Warn($"[MysteriousMap] Rejected map vote from player {player.NetId}: " +
                     $"{source} -> {destination.Value.coord} is not currently reachable.");
            return false;
        }

        if (netType is NetGameType.Host or NetGameType.Client)
        {
            Log.Info($"[MysteriousMap] Accepted multiplayer map vote from player {player.NetId}: " +
                     $"{source} -> {destination.Value.coord}.");
        }
        return true;
    }
}
