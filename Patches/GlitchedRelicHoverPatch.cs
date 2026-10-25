using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MysteriousMapMod.Relics;

namespace MysteriousMapMod.Patches;

// The relic bar requests HoverTip, not ExtraHoverTips. Put the rolled effects
// into the primary tooltip so the settings switch has a visible result there.
[HarmonyPatch(typeof(RelicModel), "get_HoverTip")]
internal static class GlitchedRelicHoverPatch
{
    [HarmonyPostfix]
    private static void Postfix(RelicModel __instance, ref HoverTip __result)
    {
        if (__instance is not MysteriousGlitchedRelic glitched)
            return;
        var details = glitched.GetVisibleCombinationDescription();
        if (details.Length == 0)
            return;
        __result = new HoverTip(
            new LocString("relics", "MYSTERIOUS_GLITCHED_RELIC.title"),
            $"{__result.Description}\n\n{details}",
            __result.Icon);
    }
}
