using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MysteriousMapMod.Map;
using MysteriousMapMod.Rooms;

namespace MysteriousMapMod.Patches;

[HarmonyPatch(typeof(RunManager), "RollRoomTypeFor")]
internal static class MysteriousMapUnknownRoomTypePatch
{
    [HarmonyPrefix]
    private static bool Prefix(RunManager __instance, MapPointType pointType, ref RoomType __result)
    {
        var state = __instance.DebugOnlyGetState();
        if (state?.CurrentMapPoint is not { } point ||
            !MysteriousMapRules.IsActive(state.Map) ||
            pointType != MapPointType.Unknown)
        {
            return true;
        }

        if (MysteriousMapRules.IsRepeatableShopPoint(state, point))
        {
            // Unknown points normally roll again every time EnterMapPointInternal
            // runs. A discovered question-mark shop must instead retain its
            // original room type and recover the saved reusable inventory.
            __result = RoomType.Shop;
            return false;
        }

        if (MysteriousMapRules.IsHiddenRoom(point))
        {
            __result = RoomType.Event;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(RunManager), "CreateRoom")]
internal static class MysteriousMapRoomFactoryPatch
{
    [HarmonyPrefix]
    private static bool Prefix(
        RunManager __instance,
        RoomType roomType,
        AbstractModel? model,
        ref AbstractRoom __result)
    {
        var state = __instance.DebugOnlyGetState();
        var coord = state?.CurrentMapCoord;
        if (state == null || !coord.HasValue || !MysteriousMapRules.IsActive(state.Map) || model != null)
            return true;

        if (state.CurrentMapPoint is { } currentPoint && MysteriousMapRules.IsNestedExit(currentPoint))
        {
            // The unique exit clears the outer room and immediately returns to
            // the map. It intentionally has no encounter or repeatable reward.
            __result = new MapRoom();
            return false;
        }

        if (roomType == RoomType.Shop)
        {
            __result = RoomFactory.GetShop(state, coord.Value);
            return false;
        }

        // Unknown map points can also roll a normal combat. Route every top-level
        // map combat through the same pool and repeat-avoidance logic so an
        // unknown-room fight cannot immediately repeat the preceding encounter.
        if (roomType == RoomType.Monster && state.CurrentMapPoint != null)
        {
            __result = RoomFactory.CreateMonsterRoom(state, coord.Value);
            return false;
        }

        return true;
    }
}
