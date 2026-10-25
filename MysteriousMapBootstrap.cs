using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MysteriousMapMod.Relics;

namespace MysteriousMapMod;

[ModInitializer(nameof(Init))]
public static class MysteriousMapBootstrap
{
    private static bool _initialized;

    public static void Init()
    {
        if (_initialized)
            return;

        _initialized = true;
        MysteriousMapSettings.EnsureLoaded();
        IsaacRelicRegistration.Register();
        var harmony = new Harmony("local.mysterious_map.v111");
        var applied = 0;
        var failed = 0;

        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (type.GetCustomAttributes(typeof(HarmonyPatch), inherit: true).Length == 0)
                continue;

            try
            {
                new PatchClassProcessor(harmony, type).Patch();
                applied++;
            }
            catch (Exception exception)
            {
                failed++;
                Log.Error($"[MysteriousMap] Failed to patch {type.FullName}: {exception}");
            }
        }

        Log.Info($"[MysteriousMap] Initialized: {applied} patch classes applied, {failed} failed.");
    }
}
