using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MysteriousMapMod.Map;

namespace MysteriousMapMod.Patches;

[HarmonyPatch(typeof(RunState), nameof(RunState.GetHistoryEntryFor))]
internal static class CoordinateHistoryLookupPatch
{
    [HarmonyPrefix]
    private static bool Prefix(RunState __instance, MapLocation location, ref MapPointHistoryEntry? __result)
    {
        if (!MysteriousMapRules.IsActive(__instance.Map) ||
            location.actIndex != __instance.CurrentActIndex ||
            !location.coord.HasValue ||
            location.actIndex < 0 ||
            location.actIndex >= __instance.MapPointHistory.Count)
        {
            return true;
        }

        var index = MysteriousMapRules.FindLatestVisitIndex(__instance, location.coord.Value);
        var history = __instance.MapPointHistory[location.actIndex];
        __result = index >= 0 && index < history.Count ? history[index] : null;
        return false;
    }
}
