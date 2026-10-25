using HarmonyLib;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Runs;
using MysteriousMapMod.Map;

namespace MysteriousMapMod.Patches;

[HarmonyPatch(typeof(Neow), "get_PositiveOptions")]
internal static class RemoveWingedBootsFromNeowPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref IEnumerable<EventOption> __result)
    {
        if (IsMapModeEnabled())
            __result = __result.Where(option => option.Relic is not WingedBoots).ToList();
    }

    internal static bool IsMapModeEnabled()
    {
        var state = RunManager.Instance?.DebugOnlyGetState();
        return state != null
            ? MysteriousMapRules.IsActive(state.Map) || MysteriousMapRunMode.IsEnabled(state)
            : MysteriousMapSettings.Enabled;
    }
}

[HarmonyPatch(typeof(Tezcatara), "get_OptionPool3")]
internal static class RemoveGoldenCompassFromTezcataraPatch
{
    [HarmonyPostfix]
    private static void Postfix(ref List<EventOption> __result)
    {
        if (RemoveWingedBootsFromNeowPatch.IsMapModeEnabled())
            __result = __result.Where(option => option.Relic is not GoldenCompass).ToList();
    }
}
