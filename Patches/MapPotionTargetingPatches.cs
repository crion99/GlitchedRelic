using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;
using MysteriousMapMod.Map;
using MysteriousMapMod.Rooms;

namespace MysteriousMapMod.Patches;

[HarmonyPatch(typeof(NPotionPopup), nameof(NPotionPopup._Ready))]
internal static class EnableTargetedPotionOnMapReadyPatch
{
    [HarmonyPostfix]
    private static void Postfix(NPotionPopup __instance) => MapPotionTargeting.TryEnableUseButton(__instance);
}

[HarmonyPatch(typeof(NPotionPopup), "RefreshButtons")]
internal static class EnableTargetedPotionOnMapRefreshPatch
{
    [HarmonyPostfix]
    private static void Postfix(NPotionPopup __instance) => MapPotionTargeting.TryEnableUseButton(__instance);
}

[HarmonyPatch(typeof(NPotionHolder), nameof(NPotionHolder.UsePotion))]
internal static class RedirectTargetedPotionToMapPatch
{
    [HarmonyPrefix]
    private static bool Prefix(NPotionHolder __instance, ref Task __result)
    {
        if (!MapPotionTargeting.CanStart(__instance.Potion?.Model))
            return true;

        __result = MapPotionTargeting.SelectAndThrow(__instance);
        return false;
    }
}

[HarmonyPatch(typeof(NMapPoint), "OnFocus")]
internal static class MapPotionTargetFocusPatch
{
    [HarmonyPostfix]
    private static void Postfix(NMapPoint __instance) => MapPotionTargeting.OnPointFocused(__instance);
}

[HarmonyPatch(typeof(NMapPoint), "OnUnfocus")]
internal static class MapPotionTargetUnfocusPatch
{
    [HarmonyPostfix]
    private static void Postfix(NMapPoint __instance) => MapPotionTargeting.OnPointUnfocused(__instance);
}

[HarmonyPatch(typeof(NMapPoint), "OnRelease")]
internal static class SuppressMapTravelDuringPotionTargetingPatch
{
    [HarmonyPrefix]
    private static bool Prefix() => !MapPotionTargeting.ShouldSuppressMapSelection;
}

[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.OnMapPointSelectedLocally))]
internal static class SuppressMapVoteDuringPotionTargetingPatch
{
    [HarmonyPrefix]
    private static bool Prefix() => !MapPotionTargeting.ShouldSuppressMapSelection;
}

[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.Close))]
internal static class CancelMapPotionTargetingOnClosePatch
{
    [HarmonyPrefix]
    private static void Prefix() => MapPotionTargeting.Cancel();
}

internal static class MapPotionTargeting
{
    private sealed record PointInputState(
        bool WasEnabled,
        NodePath Left,
        NodePath Right,
        NodePath Top,
        NodePath Bottom);

    private static readonly FieldInfo UseButtonField = AccessTools.Field(typeof(NPotionPopup), "_useButton");
    private static readonly FieldInfo PopupHolderField = AccessTools.Field(typeof(NPotionPopup), "_holder");
    private static readonly FieldInfo PointDictionaryField = AccessTools.Field(typeof(NMapScreen), "_mapPointDictionary");
    private static readonly FieldInfo PointReticleField =
        AccessTools.Field(typeof(NMapPoint), "_controllerSelectionReticle");
    private static readonly MethodInfo CanUseOrRemovePotionsGetter =
        AccessTools.PropertyGetter(typeof(Player), "CanUseOrRemovePotions");
    private static readonly MethodInfo IsFocusedGetter = AccessTools.PropertyGetter(typeof(NClickableControl), "IsFocused");
    private static readonly MethodInfo InputTypeGetter = AccessTools.PropertyGetter(typeof(NControllerManager), "InputType");
    private static readonly Dictionary<NMapPoint, PointInputState> SavedPointStates = new();
    private static readonly HashSet<NMapPoint> EligiblePoints = new();
    private static NPotionHolder? _activeHolder;
    private static long _lastTargetingFinishedFrame = -1;

    public static bool IsActive => _activeHolder != null;
    public static bool ShouldSuppressMapSelection
    {
        get
        {
            if (IsActive)
                return true;
            var screen = NMapScreen.Instance;
            return screen != null && _lastTargetingFinishedFrame == screen.GetTree().GetFrame();
        }
    }

    internal static bool IsSupportedTargetType(TargetType targetType) => targetType is
        TargetType.AnyEnemy or
        TargetType.AnyPlayer or
        TargetType.AnyAlly or
        TargetType.TargetedNoCreature;

    public static bool CanStart(PotionModel? potion)
    {
        var screen = NMapScreen.Instance;
        return potion != null &&
               !IsActive &&
               screen is { IsOpen: true, IsTraveling: false } &&
               MysteriousMapRules.IsActive(potion.Owner.RunState.Map) &&
               !MysteriousMapRules.IsNestedMap(potion.Owner.RunState.Map) &&
               !CombatManager.Instance.IsInProgress &&
               // AnyTime potions such as Fruit Juice and Blood Potion already
               // have legitimate vanilla map-screen behavior; never redirect them.
               potion.Usage == PotionUsage.CombatOnly &&
               IsSupportedTargetType(potion.TargetType) &&
               !potion.IsQueued &&
               !potion.Owner.Creature.IsDead &&
               (bool)CanUseOrRemovePotionsGetter.Invoke(potion.Owner, null)! &&
               screen.Drawings.GetLocalDrawingMode() == DrawingMode.None &&
               !NTargetManager.Instance.IsInSelection;
    }

    public static void TryEnableUseButton(NPotionPopup popup)
    {
        var holder = (NPotionHolder)PopupHolderField.GetValue(popup)!;
        if (!CanStart(holder.Potion?.Model))
            return;

        var useButton = (NPotionPopupButton)UseButtonField.GetValue(popup)!;
        useButton.SetLocKey("POTION_POPUP.throw");
        useButton.Enable();
        useButton.TryGrabFocus();
    }

    public static async Task SelectAndThrow(NPotionHolder holder)
    {
        var potionNode = holder.Potion;
        var screen = NMapScreen.Instance;
        if (potionNode == null || screen == null || !CanStart(potionNode.Model))
            return;

        var points = (Dictionary<MegaCrit.Sts2.Core.Map.MapCoord, NMapPoint>)PointDictionaryField.GetValue(screen)!;
        var runState = (RunState)potionNode.Model.Owner.RunState;
        var visited = runState.VisitedMapCoords.ToHashSet();
        var eligible = points.Values
            .Where(point => point.IsVisibleInTree() && visited.Contains(point.Point.coord))
            .OrderBy(point => point.Point.coord)
            .ToList();
        if (eligible.Count == 0)
        {
            Log.Warn("[MysteriousMap] No visited map point is available for map potion targeting.");
            return;
        }

        _activeHolder = holder;
        SaveAndEnablePoints(eligible);
        ConfigureDirectionalFocus(eligible);

        var targetManager = NTargetManager.Instance;
        var selectionCompleted = false;
        RunManager.Instance.HoveredModelTracker.OnLocalPotionSelected(potionNode.Model);
        try
        {
            var startPosition = holder.GetGlobalRect().GetCenter();
            var controller = (InputType)InputTypeGetter.Invoke(NControllerManager.Instance, null)! == InputType.Controller;
            targetManager.StartTargeting(
                TargetType.TargetedNoCreature,
                startPosition,
                controller ? TargetMode.Controller : TargetMode.ClickMouseToTarget,
                () => !CanContinue(screen, holder),
                node => node is NMapPoint point && EligiblePoints.Contains(point));

            var initial = eligible.FirstOrDefault(point => point.Point.coord == potionNode.Model.Owner.RunState.CurrentMapCoord)
                          ?? eligible[0];
            if (controller)
                initial.TryGrabFocus();

            var focused = eligible.FirstOrDefault(point => (bool)IsFocusedGetter.Invoke(point, null)!) ?? initial;
            targetManager.OnNodeHovered(focused);
            ShowTargetReticle(focused);

            var selected = await targetManager.SelectionFinished();
            selectionCompleted = true;
            _lastTargetingFinishedFrame = screen.GetTree().GetFrame();
            if (selected is NMapPoint target && EligiblePoints.Contains(target))
            {
                PlayThrowVfx(screen, holder, potionNode.Model, target);
                var revealedRooms = ConsumeWithoutEffect(potionNode.Model, target.Point.coord);
                MapVisibilityController.RevealWithFade(screen, revealedRooms);
                Log.Info($"[MysteriousMap] Threw and consumed {potionNode.Model.Id.Entry} at visited map point " +
                         $"{target.Point.coord}; the potion effect was intentionally skipped.");
            }
        }
        finally
        {
            // SelectionFinished may resume synchronously from inside NTargetManager.FinishTargeting,
            // before the vanilla method changes its targeting mode back to None. Do not re-enter
            // CancelTargeting after a normal result, or the same completion source is completed twice.
            if (!selectionCompleted && targetManager.IsInSelection)
                targetManager.CancelTargeting();
            RunManager.Instance.HoveredModelTracker.OnLocalPotionDeselected();
            RestorePoints();
            _activeHolder = null;
            if (GodotObject.IsInstanceValid(holder))
                holder.TryGrabFocus();
        }
    }

    public static void OnPointFocused(NMapPoint point)
    {
        if (IsActive && EligiblePoints.Contains(point) && NTargetManager.Instance.IsInSelection)
        {
            NTargetManager.Instance.OnNodeHovered(point);
            ShowTargetReticle(point);
        }
    }

    public static void OnPointUnfocused(NMapPoint point)
    {
        if (IsActive && EligiblePoints.Contains(point) && NTargetManager.Instance.IsInSelection)
        {
            NTargetManager.Instance.OnNodeUnhovered(point);
            HideTargetReticle(point);
        }
    }

    public static void Cancel()
    {
        if (IsActive && NTargetManager.Instance.IsInSelection)
            NTargetManager.Instance.CancelTargeting();
    }

    private static bool CanContinue(NMapScreen screen, NPotionHolder holder) =>
        IsActive &&
        GodotObject.IsInstanceValid(screen) &&
        screen.IsOpen &&
        !screen.IsTraveling &&
        GodotObject.IsInstanceValid(holder) &&
        holder.Potion != null &&
        !CombatManager.Instance.IsInProgress;

    private static void SaveAndEnablePoints(IEnumerable<NMapPoint> points)
    {
        SavedPointStates.Clear();
        EligiblePoints.Clear();
        foreach (var point in points)
        {
            SavedPointStates[point] = new PointInputState(
                point.IsEnabled,
                point.FocusNeighborLeft,
                point.FocusNeighborRight,
                point.FocusNeighborTop,
                point.FocusNeighborBottom);
            EligiblePoints.Add(point);
            point.Enable();
        }
    }

    private static void RestorePoints()
    {
        foreach (var (point, state) in SavedPointStates)
        {
            if (!GodotObject.IsInstanceValid(point))
                continue;

            if (state.WasEnabled)
                point.Enable();
            else
                point.Disable();
            point.FocusNeighborLeft = state.Left;
            point.FocusNeighborRight = state.Right;
            point.FocusNeighborTop = state.Top;
            point.FocusNeighborBottom = state.Bottom;
            HideTargetReticle(point);
        }
        SavedPointStates.Clear();
        EligiblePoints.Clear();
    }

    private static void ConfigureDirectionalFocus(IReadOnlyList<NMapPoint> points)
    {
        foreach (var point in points)
        {
            point.FocusNeighborLeft = Find(point, points, candidate => candidate.Point.coord.col < point.Point.coord.col).GetPath();
            point.FocusNeighborRight = Find(point, points, candidate => candidate.Point.coord.col > point.Point.coord.col).GetPath();
            point.FocusNeighborTop = Find(point, points, candidate => candidate.Point.coord.row > point.Point.coord.row).GetPath();
            point.FocusNeighborBottom = Find(point, points, candidate => candidate.Point.coord.row < point.Point.coord.row).GetPath();
        }
    }

    private static NMapPoint Find(
        NMapPoint origin,
        IEnumerable<NMapPoint> points,
        Func<NMapPoint, bool> predicate)
    {
        return points.Where(predicate)
                     .OrderBy(point => SquaredDistance(origin, point))
                     .ThenBy(point => point.Point.coord)
                     .FirstOrDefault() ?? origin;
    }

    private static int SquaredDistance(NMapPoint a, NMapPoint b)
    {
        var x = a.Point.coord.col - b.Point.coord.col;
        var y = a.Point.coord.row - b.Point.coord.row;
        return x * x + y * y;
    }

    private static void PlayThrowVfx(
        NMapScreen screen,
        NPotionHolder holder,
        PotionModel potion,
        NMapPoint target)
    {
        var vfx = NItemThrowVfx.Create(
            holder.GetGlobalRect().GetCenter(),
            target.GetGlobalRect().GetCenter(),
            potion.Image);
        if (vfx != null)
            screen.AddChildSafely(vfx);
    }

    private static void ShowTargetReticle(NMapPoint point)
    {
        var reticle = (NSelectionReticle)PointReticleField.GetValue(point)!;
        if (!reticle.IsSelected)
            reticle.OnSelect();
    }

    private static void HideTargetReticle(NMapPoint point)
    {
        var reticle = (NSelectionReticle)PointReticleField.GetValue(point)!;
        if (reticle.IsSelected)
            reticle.OnDeselect();
    }

    private static IReadOnlyList<MegaCrit.Sts2.Core.Map.MapCoord> ConsumeWithoutEffect(
        PotionModel potion,
        MegaCrit.Sts2.Core.Map.MapCoord target)
    {
        var owner = potion.Owner;
        var slot = owner.GetPotionSlotIndex(potion);
        if (slot < 0)
        {
            Log.Warn($"[MysteriousMap] Could not consume map-targeted potion {potion.Id}: it is no longer in the belt.");
            return Array.Empty<MegaCrit.Sts2.Core.Map.MapCoord>();
        }

        var runState = (RunState)owner.RunState;
        potion.RemoveBeforeUse();
        runState.CurrentMapPointHistoryEntry?.GetEntry(owner.NetId).PotionUsed.Add(potion.Id);
        var revealedRooms = HiddenRoomState.RevealAdjacent(runState, target);
        ShopInventoryMultiplayerSynchronizer.PublishMapPotionConsumption(
            runState, owner, (uint)slot, potion.Id, target);
        return revealedRooms;
    }
}
