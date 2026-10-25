using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Nodes.Screens.ModdingScreen;

namespace MysteriousMapMod.Patches;

/// <summary>
/// Localizes the manifest immediately before the vanilla mod-info panel reads
/// it. Keeping the original Fill implementation preserves the same author,
/// version, error, font, color, and spacing style used by Chaos Mod.
/// </summary>
[HarmonyPatch(typeof(NModInfoContainer), nameof(NModInfoContainer.Fill))]
internal static class MysteriousMapModInfoLocalizationPatch
{
    private static void Prefix(Mod mod) => MysteriousMapModInfoLocalization.Apply(mod);
}

/// <summary>
/// The list row reads the manifest name during _Ready, so it needs the same
/// current-language substitution as the details panel.
/// </summary>
[HarmonyPatch(typeof(NModMenuRow), nameof(NModMenuRow._Ready))]
internal static class MysteriousMapModMenuNameLocalizationPatch
{
    private static void Prefix(NModMenuRow __instance)
    {
        if (__instance.Mod is { } mod)
            MysteriousMapModInfoLocalization.Apply(mod);
    }
}

internal static class MysteriousMapModInfoLocalization
{
    public static void Apply(Mod mod)
    {
        if (mod.manifest?.id != "MysteriousMap")
            return;

        mod.manifest.name = new LocString("main_menu_ui", "MYSTERIOUS_MAP_MOD_NAME").GetFormattedText();
        mod.manifest.description = new LocString("main_menu_ui", "MYSTERIOUS_MAP_MOD_DESCRIPTION").GetFormattedText();
    }
}
