using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using MysteriousMapMod.Map;

namespace MysteriousMapMod.Patches;

[HarmonyPatch(typeof(RunManager), nameof(RunManager.EnterMapCoord))]
internal static class ValidateMysteriousMapTravelPatch
{
    private static readonly FieldInfo VisitedField = AccessTools.Field(typeof(RunState), "_visitedMapCoords");
    private static readonly FieldInfo NextRoomIdField = AccessTools.Field(typeof(RunState), "<NextRoomId>k__BackingField");
    private static readonly MethodInfo EnterMapCoordInternalMethod =
        AccessTools.Method(typeof(RunManager), "EnterMapCoordInternal")
        ?? throw new MissingMethodException(typeof(RunManager).FullName, "EnterMapCoordInternal");

    [HarmonyPrefix]
    private static bool Prefix(RunManager __instance, MapCoord coord, ref Task __result)
    {
        var state = __instance.DebugOnlyGetState();
        if (state == null || !MysteriousMapRules.IsActive(state.Map))
            return true;

        // Debug travel is intentionally local-only. Never let one multiplayer
        // peer's console state change whether a synchronized winning vote can
        // be executed on that peer.
        var debugTravel = __instance.NetService.Type == NetGameType.Singleplayer &&
                          (NMapScreen.Instance?.IsDebugTravelEnabled ?? false);
        if (!MysteriousMapRules.CanEnter(state, coord, debugTravel))
        {
            __result = Task.CompletedTask;
            return false;
        }

        var point = state.Map.GetPoint(coord);
        if (point != null && HiddenRoomState.TryClaimBombDrop(state, point))
            Log.Info($"[MysteriousMap] A bomb dropped after clearing outer room {coord}; inventory is now {HiddenRoomState.GetBombCount(state)}.");
        var visited = (List<MapCoord>)VisitedField.GetValue(state)!;
        var isRepeatableExit = point != null && MysteriousMapRules.IsNestedExit(point);
        if (point == null ||
            (!MysteriousMapRules.IsRepeatableShopPoint(state, point) && !isRepeatableExit) ||
            !visited.Contains(coord))
            return true;

        // Do not rely on patching RunState.AddVisitedMapCoord here. That tiny
        // method can be inlined into RunManager.EnterMapCoord, leaving its
        // original duplicate-coordinate rejection active at the real callsite.
        // Reproduce the successful branch of EnterMapCoord at this stable outer
        // boundary, then continue through the game's own internal room flow.
        Log.Info($"[MysteriousMap] Accepting repeated traversal coordinate {coord} at RunManager.EnterMapCoord.");
        visited.Add(coord);
        NextRoomIdField.SetValue(state, 0);
        __result = (Task)EnterMapCoordInternalMethod.Invoke(__instance, [coord, null, true])!;
        return false;
    }
}

[HarmonyPatch(typeof(RunManager), nameof(RunManager.EnterMapPointInternal))]
internal static class ExplorationOrderFloorPatch
{
    [HarmonyPrefix]
    private static void Prefix(RunManager __instance, ref int actFloor)
    {
        var state = __instance.DebugOnlyGetState();
        if (state != null && MysteriousMapRules.IsActive(state.Map) && state.VisitedMapCoords.Count > 0)
            actFloor = state.VisitedMapCoords.Distinct().Count();
    }
}
