using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MysteriousMapMod.Map;

namespace MysteriousMapMod.Patches;

[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.OnMapPointSelectedLocally))]
internal static class ReusableShopSelectionPatch
{
    private static readonly FieldInfo RunStateField = AccessTools.Field(typeof(NMapScreen), "_runState");

    [HarmonyPrefix]
    private static bool Prefix(NMapScreen __instance, NMapPoint point)
    {
        var runState = (RunState)RunStateField.GetValue(__instance)!;
        if (!MysteriousMapRules.IsActive(runState.Map) ||
            !MysteriousMapRules.IsRepeatableShopPoint(runState, point.Point) ||
            !runState.VisitedMapCoords.Contains(point.Point.coord))
        {
            return true;
        }

        if (runState.CurrentRoom is MerchantRoom && runState.CurrentMapCoord == point.Point.coord)
        {
            Log.Info($"[MysteriousMap] Ignoring immediate re-entry into the current shop at {point.Point.coord}.");
            return false;
        }

        // Keep the complete vanilla pipeline here. VoteForMapCoordAction is an
        // Any action and is responsible for asking MapSelectionSynchronizer to
        // enqueue MoveToMapCoordAction in the correct queue context. The only
        // vanilla assumption we need to relax is duplicate map coordinates,
        // which is handled at RunManager.EnterMapCoord on every peer.
        Log.Info($"[MysteriousMap] Submitting shop revisit to {point.Point.coord} through the vanilla map vote pipeline.");
        return true;
    }
}

[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.RefreshAllPointVisuals))]
internal static class RefreshReusableShopAndSpecialScalesPatch
{
    private static readonly FieldInfo RunStateField = AccessTools.Field(typeof(NMapScreen), "_runState");
    private static readonly FieldInfo PointDictionaryField = AccessTools.Field(typeof(NMapScreen), "_mapPointDictionary");

    [HarmonyPostfix]
    private static void Postfix(NMapScreen __instance)
    {
        var runState = (RunState)RunStateField.GetValue(__instance)!;
        if (!MysteriousMapRules.IsActive(runState.Map))
            return;

        var nodes = (Dictionary<MapCoord, NMapPoint>)PointDictionaryField.GetValue(__instance)!;
        foreach (var node in nodes.Values)
        {
            if (runState.VisitedMapCoords.Contains(node.Point.coord) &&
                (MysteriousMapRules.IsRepeatableShopPoint(runState, node.Point) ||
                 MysteriousMapRules.IsNestedMap(runState.Map) &&
                 MysteriousMapRules.IsNestedExit(node.Point)) &&
                MysteriousMapRules.CanEnter(runState, node.Point.coord, debugTravel: false))
            {
                node.State = MapPointState.Travelable;
            }

            node.Scale = Vector2.One * (node is NNormalMapPoint
                ? MysteriousMapRules.IsNestedMap(runState.Map) ? 0.62f : 0.85f
                : 0.30f);
        }
    }
}

[HarmonyPatch(typeof(NAncientMapPoint), "_Process")]
internal static class LockAncientMapPointScalePatch
{
    private static readonly FieldInfo RunStateField = AccessTools.Field(typeof(NMapPoint), "_runState");

    [HarmonyPostfix]
    private static void Postfix(NAncientMapPoint __instance)
    {
        var runState = (IRunState)RunStateField.GetValue(__instance)!;
        if (MysteriousMapRules.IsActive(runState.Map))
            __instance.Scale = Vector2.One * 0.30f;
    }
}

[HarmonyPatch(typeof(NMapScreen), "RecalculateTravelability")]
internal static class FrontierTravelabilityPatch
{
    private static readonly FieldInfo RunStateField = AccessTools.Field(typeof(NMapScreen), "_runState");
    private static readonly FieldInfo PointDictionaryField = AccessTools.Field(typeof(NMapScreen), "_mapPointDictionary");
    private static readonly FieldInfo PathsField = AccessTools.Field(typeof(NMapScreen), "_paths");

    [HarmonyPrefix]
    private static bool Prefix(NMapScreen __instance)
    {
        var runState = (RunState)RunStateField.GetValue(__instance)!;
        if (!MysteriousMapRules.IsActive(runState.Map))
            return true;

        var nodes = (Dictionary<MapCoord, NMapPoint>)PointDictionaryField.GetValue(__instance)!;
        var explored = MysteriousMapRules.GetExplored(runState);

        foreach (var node in nodes.Values)
            node.State = MapPointState.Untravelable;

        foreach (var coord in explored)
        {
            if (!nodes.TryGetValue(coord, out var node))
                continue;
            node.State = (MysteriousMapRules.IsRepeatableShopPoint(runState, node.Point) ||
                         MysteriousMapRules.IsNestedMap(runState.Map) &&
                         MysteriousMapRules.IsNestedExit(node.Point)) &&
                         MysteriousMapRules.CanEnter(runState, coord, debugTravel: false)
                ? MapPointState.Travelable
                : MapPointState.Traveled;
        }

        if (explored.Count == 0)
        {
            nodes[runState.Map.StartingMapPoint.coord].State = MapPointState.Travelable;
            ConfigureDirectionalFocus(nodes.Values);
            return false;
        }

        if (runState.Map.SecondBossMapPoint != null && explored.Contains(runState.Map.BossMapPoint.coord))
        {
            if (!explored.Contains(runState.Map.SecondBossMapPoint.coord))
                nodes[runState.Map.SecondBossMapPoint.coord].State = MapPointState.Travelable;
            ConfigureDirectionalFocus(nodes.Values);
            return false;
        }

        foreach (var node in nodes.Values.OrderBy(n => n.Point.coord))
        {
            if (!explored.Contains(node.Point.coord) &&
                MysteriousMapRules.CanEnter(runState, node.Point.coord, debugTravel: false))
            {
                node.State = MapPointState.Travelable;
            }
        }

        AddAnimationAliases(runState, nodes, explored,
            (Dictionary<(MapCoord, MapCoord), IReadOnlyList<TextureRect>>)PathsField.GetValue(__instance)!);
        AddVisitedExitAnimationAliases(runState, nodes,
            (Dictionary<(MapCoord, MapCoord), IReadOnlyList<TextureRect>>)PathsField.GetValue(__instance)!);
        ConfigureDirectionalFocus(nodes.Values);
        return false;
    }

    [HarmonyPostfix]
    private static void Postfix(NMapScreen __instance)
    {
        MapVisibilityController.Apply(__instance);
    }

    private static void AddAnimationAliases(
        RunState runState,
        IReadOnlyDictionary<MapCoord, NMapPoint> nodes,
        IReadOnlySet<MapCoord> explored,
        Dictionary<(MapCoord, MapCoord), IReadOnlyList<TextureRect>> paths)
    {
        if (runState.VisitedMapCoords.Count == 0)
            return;

        foreach (var node in nodes.Values.Where(n => n.State == MapPointState.Travelable && !explored.Contains(n.Point.coord)))
        {
            var source = MysteriousMapRules.GetNeighbors(node.Point)
                .Where(n => explored.Contains(n.coord))
                .OrderByDescending(n => MysteriousMapRules.FindLatestVisitIndex(runState, n.coord))
                .ThenBy(n => n.coord)
                .FirstOrDefault();
            if (source == null)
                continue;

            if (!paths.TryGetValue((source.coord, node.Point.coord), out var path) &&
                !paths.TryGetValue((node.Point.coord, source.coord), out path))
            {
                continue;
            }

            // A repeatable shop can become the latest coordinate without
            // changing the explored set. Pre-create aliases from every explored
            // origin so the map screen never lacks the animation path required
            // for the next freely selected frontier node.
            foreach (var origin in explored)
                paths[(origin, node.Point.coord)] = path;
        }
    }

    private static void AddVisitedExitAnimationAliases(
        RunState runState,
        IReadOnlyDictionary<MapCoord, NMapPoint> nodes,
        Dictionary<(MapCoord, MapCoord), IReadOnlyList<TextureRect>> paths)
    {
        if (!MysteriousMapRules.IsNestedMap(runState.Map) || runState.VisitedMapCoords.Count == 0)
            return;
        var currentCoord = runState.VisitedMapCoords[^1];
        var current = runState.Map.GetPoint(currentCoord);
        if (current == null || !MysteriousMapRules.IsNestedExit(current))
            return;

        foreach (var node in nodes.Values.Where(node =>
                     node.State == MapPointState.Travelable &&
                     MysteriousMapRules.IsNestedExit(node.Point)))
        {
            var targetEntry = MysteriousMapRules.GetNestedRoomEntry(runState.Map, node.Point);
            var currentEntry = MysteriousMapRules.GetNestedRoomEntry(runState.Map, current);
            if (targetEntry == null || currentEntry == null)
                continue;
            if (!paths.TryGetValue((currentCoord, targetEntry.coord), out var path) &&
                !paths.TryGetValue((targetEntry.coord, currentCoord), out path) &&
                !paths.TryGetValue((node.Point.coord, currentEntry.coord), out path) &&
                !paths.TryGetValue((currentEntry.coord, node.Point.coord), out path))
                continue;
            paths[(currentCoord, node.Point.coord)] = path;
        }
    }

    internal static void ConfigureDirectionalFocus(IEnumerable<NMapPoint> allNodes)
    {
        var nodes = allNodes.Where(n => n.State == MapPointState.Travelable).ToList();
        foreach (var node in nodes)
        {
            node.FocusNeighborLeft = Find(node, nodes, p => p.Point.coord.col < node.Point.coord.col).GetPath();
            node.FocusNeighborRight = Find(node, nodes, p => p.Point.coord.col > node.Point.coord.col).GetPath();
            node.FocusNeighborTop = Find(node, nodes, p => p.Point.coord.row > node.Point.coord.row).GetPath();
            node.FocusNeighborBottom = Find(node, nodes, p => p.Point.coord.row < node.Point.coord.row).GetPath();
        }
    }

    private static NMapPoint Find(NMapPoint origin, IEnumerable<NMapPoint> nodes, Func<NMapPoint, bool> predicate)
    {
        return nodes.Where(predicate)
                   .OrderBy(n => SquaredDistance(origin.Point.coord, n.Point.coord))
                   .ThenBy(n => n.Point.coord)
                   .FirstOrDefault() ?? origin;
    }

    private static int SquaredDistance(MapCoord a, MapCoord b)
    {
        var x = a.col - b.col;
        var y = a.row - b.row;
        return x * x + y * y;
    }
}

/// <summary>
/// Runs before NMapScreen creates path dots. Repositioning after SetMap would
/// leave all dots at the old vanilla coordinates.
/// </summary>
[HarmonyPatch(typeof(NMapScreen), "DrawPaths")]
internal static class CompactGridLayoutPatch
{
    private const float LegacySpacing = 90f;
    private const float NestedSpacing = 48f;
    private static readonly FieldInfo MapField = AccessTools.Field(typeof(NMapScreen), "_map");
    private static readonly FieldInfo PointDictionaryField = AccessTools.Field(typeof(NMapScreen), "_mapPointDictionary");
    private static readonly FieldInfo PointsField = AccessTools.Field(typeof(NMapScreen), "_points");
    private static readonly FieldInfo DistXField = AccessTools.Field(typeof(NMapScreen), "_distX");
    private static readonly FieldInfo DistYField = AccessTools.Field(typeof(NMapScreen), "_distY");

    [HarmonyPrefix]
    private static void Prefix(NMapScreen __instance)
    {
        var map = (ActMap)MapField.GetValue(__instance)!;
        if (!MysteriousMapRules.IsActive(map))
            return;

        var spacing = MysteriousMapRules.IsNestedMap(map) ? NestedSpacing : LegacySpacing;
        DistXField.SetValue(__instance, spacing);
        DistYField.SetValue(__instance, spacing);
        var nodes = (Dictionary<MapCoord, NMapPoint>)PointDictionaryField.GetValue(__instance)!;
        var pointsContainer = (Control)PointsField.GetValue(__instance)!;
        var containerWidth = pointsContainer.Size.X;
        if (containerWidth <= 0f)
            containerWidth = __instance.Size.X;
        if (containerWidth <= 0f)
            containerWidth = __instance.GetViewportRect().Size.X;

        // These nodes are already parented, so Position is relative to the
        // Points container's top-left corner—not an offset from screen center.
        var screenCenterX = containerWidth * 0.5f;
        var minCol = nodes.Keys.Min(coord => coord.col);
        var maxCol = nodes.Keys.Max(coord => coord.col);
        var mapCenterCol = (minCol + maxCol) * 0.5f;
        foreach (var node in nodes.Values)
        {
            var center = new Vector2(
                screenCenterX + (node.Point.coord.col - mapCenterCol) * spacing,
                1040f - node.Point.coord.row * spacing);
            node.Position = center - node.Size * 0.5f;
            if (node is NNormalMapPoint)
            {
                node.Scale = Vector2.One * (MysteriousMapRules.IsNestedMap(map) ? 0.62f : 0.85f);
            }
            else
            {
                node.Scale = Vector2.One * 0.30f;
            }
        }
    }
}

[HarmonyPatch(typeof(NMapScreen), "GetLineEndpoint")]
internal static class CenteredMapLineEndpointPatch
{
    // Map dots are 16x16 controls anchored to the screen center. CreatePath
    // compensates their centered anchor with +20 rather than their 8px
    // half-size, so a supplied endpoint is rendered 28px down and right.
    // Vanilla normal points deliberately supply their top-left corner and use
    // that implicit 28px shift as the icon-center offset.
    private static readonly Vector2 PathDotCenterOffset = new(28f, 28f);
    private static readonly FieldInfo MapField = AccessTools.Field(typeof(NMapScreen), "_map");

    [HarmonyPostfix]
    private static void Postfix(NMapScreen __instance, NMapPoint point, ref Vector2 __result)
    {
        var map = (ActMap)MapField.GetValue(__instance)!;
        if (MysteriousMapRules.IsActive(map))
        {
            // Preserve the normal-point convention. For special points, first
            // target the art pivot and then cancel CreatePath's implicit 28px
            // shift so the rendered dots—not merely the supplied endpoint—are
            // centered on the Ancient/Neow or boss icon.
            __result = point is NNormalMapPoint
                ? point.Position
                : point.Position + point.PivotOffset - PathDotCenterOffset;
        }
    }
}

[HarmonyPatch(typeof(NMapScreen), nameof(NMapScreen.SetMap))]
internal static class FinalizeMysteriousMapUiPatch
{
    private static readonly FieldInfo RunStateField = AccessTools.Field(typeof(NMapScreen), "_runState");
    private static readonly FieldInfo PointDictionaryField = AccessTools.Field(typeof(NMapScreen), "_mapPointDictionary");
    private static readonly FieldInfo PathsField = AccessTools.Field(typeof(NMapScreen), "_paths");
    private static readonly FieldInfo PathsContainerField = AccessTools.Field(typeof(NMapScreen), "_pathsContainer");

    [HarmonyPostfix]
    private static void Postfix(NMapScreen __instance, ActMap map)
    {
        if (!MysteriousMapRules.IsActive(map))
            return;

        var runState = (RunState)RunStateField.GetValue(__instance)!;
        var nodes = (Dictionary<MapCoord, NMapPoint>)PointDictionaryField.GetValue(__instance)!;
        var paths = (Dictionary<(MapCoord, MapCoord), IReadOnlyList<TextureRect>>)PathsField.GetValue(__instance)!;
        var pathsContainer = (Control)PathsContainerField.GetValue(__instance)!;

        foreach (var pair in paths.ToList())
        {
            if (!paths.ContainsKey((pair.Key.Item2, pair.Key.Item1)))
                paths[(pair.Key.Item2, pair.Key.Item1)] = pair.Value;
        }

        AlignSpecialNodePaths(nodes, paths, pathsContainer);

        // Vanilla first colors consecutive visit pairs. Free exploration can
        // jump from the current room to a frontier grown from an older room,
        // and the old fallback colored every edge whose endpoints were both
        // explored. Reset that broad highlight and retain only the physical
        // edge through which each room was first discovered.
        foreach (var dot in paths.Values.SelectMany(path => path).Distinct())
        {
            dot.Modulate = runState.Act.MapUntraveledColor;
            dot.Scale = Vector2.One;
        }
        var traversedEdges = GetDiscoveryEdges(runState);
        foreach (var pair in paths)
        {
            if (!traversedEdges.Contains(CanonicalEdge(pair.Key.Item1, pair.Key.Item2)))
                continue;
            foreach (var dot in pair.Value)
            {
                dot.Modulate = runState.Act.MapTraveledColor;
                dot.Scale = Vector2.One * 1.2f;
            }
        }

        FrontierTravelabilityPatch.ConfigureDirectionalFocus(nodes.Values);
        MapVisibilityController.Apply(__instance);
    }

    private static void AlignSpecialNodePaths(
        IReadOnlyDictionary<MapCoord, NMapPoint> nodes,
        IReadOnlyDictionary<(MapCoord, MapCoord), IReadOnlyList<TextureRect>> paths,
        Control pathsContainer)
    {
        var aligned = new HashSet<IReadOnlyList<TextureRect>>(ReferenceEqualityComparer.Instance);
        var toPathLocal = pathsContainer.GetGlobalTransformWithCanvas().AffineInverse();
        foreach (var pair in paths)
        {
            if (!aligned.Add(pair.Value) || pair.Value.Count == 0 ||
                !nodes.TryGetValue(pair.Key.Item1, out var first) ||
                !nodes.TryGetValue(pair.Key.Item2, out var second) ||
                first is NNormalMapPoint && second is NNormalMapPoint)
            {
                continue;
            }

            // Work from the nodes' final rendered transforms. This accounts
            // for anchors, the 0.30 special-node scale, and any scene-specific
            // pivot without relying on CreatePath's pre-parent coordinates.
            var start = toPathLocal * (first.GetGlobalTransformWithCanvas() * first.PivotOffset);
            var end = toPathLocal * (second.GetGlobalTransformWithCanvas() * second.PivotOffset);
            for (var index = 0; index < pair.Value.Count; index++)
            {
                var dot = pair.Value[index];
                var t = (index + 1f) / (pair.Value.Count + 1f);
                dot.Position = start.Lerp(end, t) - dot.Size * 0.5f;
            }
        }
    }

    private static HashSet<(MapCoord, MapCoord)> GetDiscoveryEdges(RunState runState)
    {
        var result = new HashSet<(MapCoord, MapCoord)>();
        var firstSeen = new HashSet<MapCoord>();
        var visits = runState.VisitedMapCoords;
        for (var index = 0; index < visits.Count; index++)
        {
            var destination = visits[index];
            if (!firstSeen.Add(destination))
                continue;
            var destinationPoint = runState.Map.GetPoint(destination);
            if (destinationPoint == null)
                continue;

            MapCoord? source = null;
            var latestSourceVisit = -1;
            foreach (var neighbor in MysteriousMapRules.GetNeighbors(destinationPoint))
            {
                for (var visitIndex = index - 1; visitIndex >= 0; visitIndex--)
                {
                    if (visits[visitIndex] != neighbor.coord)
                        continue;
                    if (visitIndex > latestSourceVisit)
                    {
                        latestSourceVisit = visitIndex;
                        source = neighbor.coord;
                    }
                    break;
                }
            }
            if (source.HasValue)
                result.Add(CanonicalEdge(source.Value, destination));
        }
        return result;
    }

    private static (MapCoord, MapCoord) CanonicalEdge(MapCoord first, MapCoord second) =>
        Comparer<MapCoord>.Default.Compare(first, second) <= 0 ? (first, second) : (second, first);
}

[HarmonyPatch(typeof(NNormalMapPoint), "UpdateIcon")]
internal static class UnknownRoomHistoryIconPatch
{
    private static readonly FieldInfo RunStateField = AccessTools.Field(typeof(NMapPoint), "_runState");
    private static readonly FieldInfo IconField = AccessTools.Field(typeof(NNormalMapPoint), "_icon");
    private static readonly FieldInfo OutlineField = AccessTools.Field(typeof(NNormalMapPoint), "_outline");
    private static readonly MethodInfo UnknownIconPath = AccessTools.Method(typeof(NNormalMapPoint), "UnknownIconPath");
    private static readonly MethodInfo UnknownOutlinePath = AccessTools.Method(typeof(NNormalMapPoint), "UnknownOutlinePath");

    [HarmonyPostfix]
    private static void Postfix(NNormalMapPoint __instance)
    {
        var runState = (RunState)RunStateField.GetValue(__instance)!;
        if (!MysteriousMapRules.IsActive(runState.Map) ||
            __instance.Point.PointType != MapPointType.Unknown ||
            !runState.VisitedMapCoords.Contains(__instance.Point.coord))
        {
            return;
        }

        MapPointHistoryEntry? history = runState.GetHistoryEntryFor(
            new MapLocation(__instance.Point.coord, runState.CurrentActIndex));
        var roomType = history?.Rooms.FirstOrDefault()?.RoomType;
        if (!roomType.HasValue)
            return;

        var iconPath = (string)UnknownIconPath.Invoke(null, [roomType.Value])!;
        var outlinePath = (string)UnknownOutlinePath.Invoke(null, [roomType.Value])!;
        ((TextureRect)IconField.GetValue(__instance)!).Texture = ResourceLoader.Load<Texture2D>(iconPath);
        ((TextureRect)OutlineField.GetValue(__instance)!).Texture = ResourceLoader.Load<Texture2D>(outlinePath);
    }
}
