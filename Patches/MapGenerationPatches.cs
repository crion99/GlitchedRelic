using HarmonyLib;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Runs;
using MysteriousMapMod.Map;

namespace MysteriousMapMod.Patches;

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyGeneratedMap))]
internal static class ReplaceGeneratedMapPatch
{
    [HarmonyPostfix]
    private static void Postfix(IRunState runState, int actIndex, ref ActMap __result)
    {
        if (runState is not RunState concrete)
            return;

        if (!MysteriousMapRunMode.IsEnabled(concrete))
        {
            if (concrete.Players.Count == 1 && __result is StandardActMap)
                StandardMapTreasures.Apply(__result, concrete.Rng.Seed, actIndex);
            return;
        }

        __result = new MysteriousMapActMap(concrete, actIndex);
        MysteriousMapRules.Mark(__result);
        SpoilsMapRetargeter.Retarget(runState, __result, actIndex);
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.ModifyGeneratedMapLate))]
internal static class RecognizeLoadedMapPatch
{
    [HarmonyPostfix]
    private static void Postfix(IRunState runState, int actIndex, ref ActMap __result)
    {
        if (MysteriousMapRules.IsActive(__result))
        {
            MysteriousMapRules.Mark(__result);
            if (runState is RunState concreteState)
                HiddenRoomState.EnsureHiddenRoomIdentities(concreteState, __result, actIndex);
            SpoilsMapRetargeter.Retarget(runState, __result, actIndex);
            if (runState is RunState concrete && concrete.Players.Count > 1 &&
                !MysteriousMapRunMode.IsSyncedMultiplayerRun(concrete))
            {
                var revealSettings = MysteriousMapSettings.CaptureLocalRevealSettings();
                MysteriousMapRunMode.MarkSynced(concrete, revealSettings);
                MysteriousMapSettings.ApplyMultiplayerRevealSettings(revealSettings);
            }
        }
    }
}

internal static class SpoilsMapRetargeter
{
    private static readonly System.Reflection.MethodInfo SpoilsCoordSetter =
        AccessTools.PropertySetter(typeof(SpoilsMap), nameof(SpoilsMap.SpoilsCoord))
        ?? throw new MissingMethodException(typeof(SpoilsMap).FullName, $"set_{nameof(SpoilsMap.SpoilsCoord)}");

    public static void Retarget(IRunState runState, ActMap map, int actIndex)
    {
        var cards = runState.Players
            .SelectMany(player => player.Deck.Cards)
            .OfType<SpoilsMap>()
            .Where(card => card.SpoilsActIndex == actIndex)
            .ToList();
        if (cards.Count == 0)
            return;

        // AssignPointTypes makes the nearest non-boss leaf the guaranteed
        // treasure. Reapplying the same distance/coordinate ordering also
        // identifies it after the concrete map has been restored as SavedActMap.
        var distances = MysteriousMapRules.ComputeDistances(map);
        var target = map.GetAllMapPoints()
            .Where(point => point.PointType == MapPointType.Treasure &&
                            !MysteriousMapRules.IsInHiddenNestedRoom(map, point))
            .OrderBy(point => distances.GetValueOrDefault(point.coord, int.MaxValue))
            .ThenBy(point => point.coord)
            .FirstOrDefault();
        if (target == null)
        {
            Log.Warn($"[MysteriousMap] Could not retarget {cards.Count} Spoils Map card(s): no treasure room exists.");
            return;
        }

        foreach (var card in cards)
            SpoilsCoordSetter.Invoke(card, [target.coord]);

        Log.Info($"[MysteriousMap] Retargeted {cards.Count} Spoils Map card(s) to guaranteed treasure {target.coord}.");
    }
}

/// <summary>
/// RunManager normally converts act-one's Ancient start to a monster when the
/// run did not begin with Neow. Our starting coordinate is always an Ancient.
/// </summary>
[HarmonyPatch(typeof(MapPoint), nameof(MapPoint.PointType), MethodType.Setter)]
internal static class PreserveAncientStartingPointPatch
{
    [HarmonyPrefix]
    private static void Prefix(MapPoint __instance, ref MapPointType value)
    {
        if (value == MapPointType.Monster && MysteriousMapRules.IsStartingPoint(__instance))
            value = MapPointType.Ancient;
    }
}
