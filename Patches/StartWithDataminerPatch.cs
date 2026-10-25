using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Runs;
using MysteriousMapMod.Map;
using MysteriousMapMod.Relics;

namespace MysteriousMapMod.Patches;

/// <summary>
/// Grant the testing relic before the first room is created. This native stage
/// runs on every peer of a new run, but is not called when loading a save.
/// </summary>
[HarmonyPatch(typeof(RunManager), nameof(RunManager.FinalizeStartingRelics))]
internal static class StartWithDataminerPatch
{
    [HarmonyPostfix]
    private static void Postfix(RunManager __instance, ref Task __result)
    {
        __result = GrantAfterStartingRelics(__result, __instance);
    }

    private static async Task GrantAfterStartingRelics(Task originalTask, RunManager manager)
    {
        await originalTask;
        var state = manager.DebugOnlyGetState();
        if (state == null || !MysteriousMapRunMode.ShouldGrantStartingDataminer(state))
            return;

        foreach (var player in state.Players.OrderBy(player => player.NetId))
        {
            if (player.Relics.Any(relic => relic is MysteriousDataminer))
                continue;
            try
            {
                await RelicCmd.Obtain<MysteriousDataminer>(player);
                Log.Info($"[MysteriousMap] Granted starting Dataminer to player {player.NetId}.");
            }
            catch (Exception exception)
            {
                Log.Error($"[MysteriousMap] Could not grant starting Dataminer " +
                          $"to player {player.NetId}: {exception}");
            }
        }
    }
}
