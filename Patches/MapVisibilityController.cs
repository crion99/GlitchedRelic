using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using MysteriousMapMod.Map;
using MysteriousMapMod.Relics;

namespace MysteriousMapMod.Patches;

internal static class MapVisibilityController
{
    private const string QuestionMaskName = "MysteriousMapQuestionMask";
    private const string HiddenMarkerRootName = "MysteriousMapHiddenXMarkers";
    private const string PathDotTexturePath =
        "res://images/atlases/compressed.sprites/map/map_dot.tres";
    private static readonly FieldInfo RunStateField = AccessTools.Field(typeof(NMapScreen), "_runState");
    private static readonly FieldInfo PointDictionaryField = AccessTools.Field(typeof(NMapScreen), "_mapPointDictionary");
    private static readonly FieldInfo PathsField = AccessTools.Field(typeof(NMapScreen), "_paths");
    private static readonly FieldInfo PathsContainerField = AccessTools.Field(typeof(NMapScreen), "_pathsContainer");
    private static readonly MethodInfo RecalculateTravelabilityMethod =
        AccessTools.Method(typeof(NMapScreen), "RecalculateTravelability");
    private static Texture2D? _pathDotTexture;
    internal static void RefreshCurrentMap()
    {
        var screen = NMapScreen.Instance;
        if (GodotObject.IsInstanceValid(screen))
            Apply(screen!);
    }

    internal static void Apply(NMapScreen screen)
    {
        var runState = (RunState?)RunStateField.GetValue(screen);
        if (runState == null || !MysteriousMapRules.IsActive(runState.Map))
            return;

        var nodes = (Dictionary<MapCoord, NMapPoint>)PointDictionaryField.GetValue(screen)!;
        var paths = (Dictionary<(MapCoord, MapCoord), IReadOnlyList<TextureRect>>)PathsField.GetValue(screen)!;
        var pathsContainer = (Control)PathsContainerField.GetValue(screen)!;
        var revealAll = MysteriousMapSettings.Mind;
        var revealSpecialRooms = MysteriousMapSettings.Compass;
        var showTreasureMap = MysteriousMapSettings.TreasureMap;
        var showBlueMap = MysteriousMapSettings.BlueMap;
        var localPlayer = LocalContext.GetMe(runState.Players);
        if (localPlayer != null)
        {
            showBlueMap |= localPlayer.Relics.Any(relic => relic is MysteriousBlueMap);
            revealSpecialRooms |= localPlayer.Relics.Any(relic => relic is MysteriousCompass);
        }
        var explored = MysteriousMapRules.GetExplored(runState);

        foreach (var node in nodes.Values)
        {
            var hiddenNestedRoom = MysteriousMapRules.IsInHiddenNestedRoom(runState.Map, node.Point);
            var nestedEntry = hiddenNestedRoom
                ? MysteriousMapRules.GetNestedRoomEntry(runState.Map, node.Point)
                : null;
            var hiddenNestedRoomUnlocked = nestedEntry != null && HiddenRoomState.IsRevealed(runState, nestedEntry);
            if (hiddenNestedRoom && !MysteriousMapRules.IsHiddenRoom(node.Point) && !hiddenNestedRoomUnlocked)
            {
                node.Visible = false;
                continue;
            }

            if (MysteriousMapRules.IsHiddenRoom(node.Point))
            {
                var unlocked = HiddenRoomState.IsRevealed(runState, node.Point);
                var showHidden = unlocked || revealAll || showBlueMap;
                node.Visible = showHidden;
                // Reveal options disclose only the hidden room's position. Its
                // actual icon and connections remain sealed until potion use.
                SetRoomContentVisible(node, unlocked);
                SetHiddenXMarker(pathsContainer, node, showHidden && !unlocked, runState.Act.MapUntraveledColor);
                continue;
            }

            // Visibility options are presentation only. Natural discovery is
            // reconstructed from durable run data instead of NMapPoint.State,
            // which is transient UI state and may be refreshed independently.
            var naturallyOpen = explored.Contains(node.Point.coord) ||
                                MysteriousMapRules.CanEnter(runState, node.Point.coord, debugTravel: false);
            var specialRoomRevealed = revealSpecialRooms && IsSpecialRoom(node.Point.PointType);
            var showRealRoom = naturallyOpen || revealAll || specialRoomRevealed;
            var showBlankRoom = showTreasureMap && !showRealRoom;
            node.Visible = showRealRoom || showBlankRoom;
            SetRoomContentVisible(node, showRealRoom);
        }

        foreach (var pair in paths)
        {
            var visible = nodes.TryGetValue(pair.Key.Item1, out var first) && first.Visible &&
                          nodes.TryGetValue(pair.Key.Item2, out var second) && second.Visible &&
                          !IsLockedHiddenRoom(runState, first.Point) &&
                          !IsLockedHiddenRoom(runState, second.Point);
            foreach (var dot in pair.Value)
                dot.Visible = visible;
        }

        NestedRoomFrameRenderer.Apply(screen, runState, nodes, paths);
    }

    internal static void RevealWithFade(NMapScreen screen, IReadOnlyCollection<MapCoord> revealedRooms)
    {
        if (revealedRooms.Count == 0 || !GodotObject.IsInstanceValid(screen))
            return;

        RecalculateTravelabilityMethod.Invoke(screen, null);
        var nodes = (Dictionary<MapCoord, NMapPoint>)PointDictionaryField.GetValue(screen)!;
        var paths = (Dictionary<(MapCoord, MapCoord), IReadOnlyList<TextureRect>>)PathsField.GetValue(screen)!;
        var revealed = revealedRooms.ToHashSet();

        foreach (var coord in revealed)
        {
            if (!nodes.TryGetValue(coord, out var node) || !node.Visible)
                continue;
            FadeIn(node, screen);
        }

        var fadedDots = new HashSet<TextureRect>();
        foreach (var pair in paths)
        {
            if ((!revealed.Contains(pair.Key.Item1) && !revealed.Contains(pair.Key.Item2)) ||
                !nodes.TryGetValue(pair.Key.Item1, out var first) || !first.Visible ||
                !nodes.TryGetValue(pair.Key.Item2, out var second) || !second.Visible)
            {
                continue;
            }
            foreach (var dot in pair.Value)
                if (fadedDots.Add(dot))
                    FadeIn(dot, screen);
        }
    }

    private static void FadeIn(CanvasItem item, Node tweenOwner)
    {
        var color = item.Modulate;
        item.Modulate = new Color(color.R, color.G, color.B, 0f);
        tweenOwner.CreateTween()
            .TweenProperty(item, "modulate:a", color.A, 0.35)
            .SetEase(Tween.EaseType.Out)
            .SetTrans(Tween.TransitionType.Sine);
    }

    private static bool IsSpecialRoom(MapPointType type) =>
        type != MapPointType.Monster &&
        type != MapPointType.Unknown &&
        type != MapPointType.Unassigned;

    private static bool IsLockedHiddenRoom(RunState runState, MapPoint point) =>
        MysteriousMapRules.IsHiddenRoom(point) && !HiddenRoomState.IsRevealed(runState, point);

    private static void SetHiddenXMarker(
        Control pathsContainer,
        NMapPoint node,
        bool visible,
        Color color)
    {
        var root = pathsContainer.GetNodeOrNull<Control>(HiddenMarkerRootName);
        if (root == null)
        {
            root = new Control
            {
                Name = HiddenMarkerRootName,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };
            pathsContainer.AddChild(root);
        }

        var markerName = $"HiddenX_{node.Point.coord.col}_{node.Point.coord.row}";
        var marker = root.GetNodeOrNull<Control>(markerName);
        if (marker == null)
        {
            marker = CreateHiddenX(markerName);
            root.AddChild(marker);
        }

        marker.Visible = visible;
        if (!visible)
            return;

        var globalCenter = node.GetGlobalTransformWithCanvas() * node.PivotOffset;
        marker.Position = pathsContainer.GetGlobalTransformWithCanvas().AffineInverse() * globalCenter;
        foreach (var dot in marker.GetChildren().OfType<TextureRect>())
            dot.Modulate = color;
    }

    private static Control CreateHiddenX(string name)
    {
        var marker = new Control
        {
            Name = name,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _pathDotTexture ??= ResourceLoader.Load<Texture2D>(PathDotTexturePath);

        // Seven dots per diagonal produce the same hand-drawn dotted language
        // as ordinary map paths while remaining inside a single grid cell.
        for (var step = -3; step <= 3; step++)
        {
            AddMarkerDot(marker, new Vector2(step * 7f, step * 7f), -Mathf.Pi / 4f);
            if (step != 0)
                AddMarkerDot(marker, new Vector2(step * 7f, -step * 7f), Mathf.Pi / 4f);
        }
        return marker;
    }

    private static void AddMarkerDot(Control marker, Vector2 center, float rotation)
    {
        var dot = new TextureRect
        {
            Texture = _pathDotTexture,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Size = new Vector2(16f, 16f),
            PivotOffset = new Vector2(8f, 8f),
            Position = center - new Vector2(8f, 8f),
            Rotation = rotation,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        };
        marker.AddChild(dot);
    }

    private static void SetRoomContentVisible(NMapPoint node, bool visible)
    {
        var content = node switch
        {
            NNormalMapPoint => node.GetNodeOrNull<CanvasItem>("IconContainer"),
            NAncientMapPoint => node.GetNodeOrNull<CanvasItem>("Icon"),
            NBossMapPoint => node.GetNodeOrNull<CanvasItem>("SpriteContainer"),
            _ => null
        };
        if (content != null)
            content.Visible = visible;

        // Remove the legacy question-mark overlay from maps that were already
        // open when this version was hot-reloaded. New overlays are never made:
        // an undiscovered node is represented solely by its connecting paths.
        var overlay = node.GetNodeOrNull<TextureRect>(QuestionMaskName);
        if (overlay != null)
            overlay.Visible = false;
    }
}
