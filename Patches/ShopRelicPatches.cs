using HarmonyLib;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MysteriousMapMod.Map;

namespace MysteriousMapMod.Patches;

internal static class ShopRelicVisitRules
{
    public static bool IsRepeatedMysteriousShopEntry(RunState runState, AbstractRoom room)
    {
        if (room is not MerchantRoom || !MysteriousMapRules.IsActive(runState.Map))
            return false;

        var coord = runState.CurrentMapCoord;
        return coord.HasValue && runState.VisitedMapCoords.Count(visited => visited == coord.Value) > 1;
    }
}

[HarmonyPatch(typeof(MawBank), nameof(MawBank.AfterRoomEntered))]
internal static class MawBankFirstShopVisitOnlyPatch
{
    [HarmonyPrefix]
    private static bool Prefix(MawBank __instance, AbstractRoom room, ref Task __result)
    {
        if (__instance.Owner.RunState is not RunState runState ||
            !ShopRelicVisitRules.IsRepeatedMysteriousShopEntry(runState, room))
        {
            return true;
        }

        // Harmony leaves reference-type results at null when a prefix skips an
        // async original. Hook.AfterRoomEntered awaits every returned Task, so
        // return a real completed task instead of causing a post-fade await-null
        // exception and a permanently black screen.
        __result = Task.CompletedTask;
        return false;
    }
}

[HarmonyPatch(typeof(MealTicket), nameof(MealTicket.AfterRoomEntered))]
internal static class MealTicketFirstShopVisitOnlyPatch
{
    [HarmonyPrefix]
    private static bool Prefix(MealTicket __instance, AbstractRoom room, ref Task __result)
    {
        if (__instance.Owner.RunState is not RunState runState ||
            !ShopRelicVisitRules.IsRepeatedMysteriousShopEntry(runState, room))
        {
            return true;
        }

        __result = Task.CompletedTask;
        return false;
    }
}
