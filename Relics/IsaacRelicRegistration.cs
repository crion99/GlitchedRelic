using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models.RelicPools;

namespace MysteriousMapMod.Relics;

internal static class IsaacRelicRegistration
{
    private static bool _registered;

    internal static void Register()
    {
        if (_registered)
            return;
        _registered = true;
        VanillaRelicFragments.Register();
        GlitchCurseOutcomes.Register();
        // Keep the old model classes so existing saves can still load them,
        // but do not offer these extra Isaac relics in new rewards or shops.
        ModHelper.AddModelToPool<SharedRelicPool, MysteriousDataminer>();
        ModHelper.AddModelToPool<SharedRelicPool, MysteriousGlitchedRelic>();
    }
}
