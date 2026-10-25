using System.Runtime.CompilerServices;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;

namespace MysteriousMapMod.Map;

/// <summary>
/// A lobby/save transport marker. It remains just long enough for the game's
/// native run-created/run-loaded modifier lifecycle to confirm activation,
/// then removes itself before modifier UI or later gameplay hooks can use it.
/// </summary>
public sealed class MysteriousMapMultiplayerMarkerModifier : ModifierModel
{
    [SavedProperty]
    public int RevealOptionsMask { get; set; }

    [SavedProperty]
    public bool StartWithDataminer { get; set; }

    public override bool ShouldReceiveCombatHooks => false;

    protected override void AfterRunCreated(RunState runState) =>
        Activate(runState, "ModifierModel.AfterRunCreated");

    protected override void AfterRunLoaded(RunState runState) =>
        Activate(runState, "ModifierModel.AfterRunLoaded");

    private void Activate(RunState runState, string source)
    {
        var revealSettings = MysteriousMapRevealSettings.FromMask(RevealOptionsMask);
        MysteriousMapRunMode.MarkSynced(runState, revealSettings, StartWithDataminer);
        MysteriousMapSettings.ApplyMultiplayerRevealSettings(revealSettings);

        // The marker has now completed its native modifier lifecycle. Removing
        // it here keeps it out of modifier UI and later gameplay hooks while
        // retaining a fallback that does not depend on a Harmony patch to
        // RunState.CreateForNewRun/FromSerializable being observed.
        MysteriousMapRunMode.StripRuntimeMarker(runState);
        Log.Info($"[MysteriousMap] Activated host-authoritative multiplayer map mode via {source} " +
                 $"(players={runState.Players.Count}, mask={RevealOptionsMask}).");
    }
}

internal readonly record struct MysteriousMapRevealSettings(
    bool Mind,
    bool Compass,
    bool TreasureMap,
    bool BlueMap)
{
    private const int MindBit = 1 << 0;
    private const int CompassBit = 1 << 1;
    private const int TreasureMapBit = 1 << 2;
    private const int BlueMapBit = 1 << 3;

    internal int ToMask() =>
        (Mind ? MindBit : 0) |
        (Compass ? CompassBit : 0) |
        (TreasureMap ? TreasureMapBit : 0) |
        (BlueMap ? BlueMapBit : 0);

    internal static MysteriousMapRevealSettings FromMask(int mask) => new(
        (mask & MindBit) != 0,
        (mask & CompassBit) != 0,
        (mask & TreasureMapBit) != 0,
        (mask & BlueMapBit) != 0);
}

internal static class MysteriousMapRunMode
{
    private sealed class Tag
    {
        internal MysteriousMapRevealSettings RevealSettings { get; set; }
        internal bool StartWithDataminer { get; set; }
    }

    private static readonly ConditionalWeakTable<RunState, Tag> SyncedMultiplayerRuns = new();

    internal static bool IsEnabled(RunState runState)
    {
        // Singleplayer keeps using the local total-control setting. In a real
        // multiplayer run the host-synchronized marker is authoritative.
        return runState.Players.Count <= 1
            ? MysteriousMapSettings.Enabled
            : SyncedMultiplayerRuns.TryGetValue(runState, out _);
    }

    internal static bool IsSyncedMultiplayerRun(RunState runState) =>
        SyncedMultiplayerRuns.TryGetValue(runState, out _);

    internal static void MarkSynced(RunState runState, MysteriousMapRevealSettings revealSettings,
        bool startWithDataminer = false)
    {
        var tag = SyncedMultiplayerRuns.GetValue(runState, _ => new Tag());
        tag.RevealSettings = revealSettings;
        tag.StartWithDataminer = startWithDataminer;
    }

    internal static bool ShouldGrantStartingDataminer(RunState runState) =>
        (runState.Players.Count <= 1
            ? MysteriousMapSettings.StartWithDataminer
            : IsEnabled(runState) &&
              SyncedMultiplayerRuns.TryGetValue(runState, out var tag) && tag.StartWithDataminer);

    internal static MysteriousMapRevealSettings GetRevealSettings(RunState runState) =>
        SyncedMultiplayerRuns.TryGetValue(runState, out var tag)
            ? tag.RevealSettings
            : MysteriousMapSettings.CaptureLocalRevealSettings();

    internal static bool HasRuntimeMarker(IEnumerable<ModifierModel> modifiers) =>
        modifiers.Any(modifier => modifier is MysteriousMapMultiplayerMarkerModifier);

    internal static MysteriousMapRevealSettings ReadRuntimeRevealSettings(IEnumerable<ModifierModel> modifiers) =>
        modifiers.OfType<MysteriousMapMultiplayerMarkerModifier>()
            .Select(marker => MysteriousMapRevealSettings.FromMask(marker.RevealOptionsMask))
            .FirstOrDefault();

    internal static bool ReadRuntimeStartingDataminer(IEnumerable<ModifierModel> modifiers) =>
        modifiers.OfType<MysteriousMapMultiplayerMarkerModifier>()
            .LastOrDefault()?.StartWithDataminer == true;

    internal static bool HasSerializedMarker(SerializableRun save)
    {
        var markerId = ModelDb.Modifier<MysteriousMapMultiplayerMarkerModifier>().Id;
        return save.Modifiers.Any(modifier => modifier.Id == markerId);
    }

    internal static MysteriousMapRevealSettings ReadSerializedRevealSettings(SerializableRun save)
    {
        var markerId = ModelDb.Modifier<MysteriousMapMultiplayerMarkerModifier>().Id;
        var marker = save.Modifiers.FirstOrDefault(modifier => modifier.Id == markerId);
        var mask = marker?.Props?.ints?
            .FirstOrDefault(property => property.name == nameof(MysteriousMapMultiplayerMarkerModifier.RevealOptionsMask)).value ?? 0;
        return MysteriousMapRevealSettings.FromMask(mask);
    }

    internal static void StripRuntimeMarker(RunState runState)
    {
        var modifiers = runState.Modifiers
            .Where(modifier => modifier is not MysteriousMapMultiplayerMarkerModifier)
            .ToArray();
        if (modifiers.Length != runState.Modifiers.Count)
            AccessTools.Property(typeof(RunState), nameof(RunState.Modifiers)).SetValue(runState, modifiers);
    }

    internal static void SetSerializedMarker(
        SerializableRun save,
        bool enabled,
        MysteriousMapRevealSettings revealSettings)
    {
        var markerId = ModelDb.Modifier<MysteriousMapMultiplayerMarkerModifier>().Id;
        save.Modifiers.RemoveAll(modifier => modifier.Id == markerId);
        if (enabled)
        {
            save.Modifiers.Add(new SerializableModifier
            {
                Id = markerId,
                Props = new SavedProperties
                {
                    ints =
                    [
                        new SavedProperties.SavedProperty<int>(
                            nameof(MysteriousMapMultiplayerMarkerModifier.RevealOptionsMask),
                            revealSettings.ToMask())
                    ]
                }
            });
        }
    }
}
