using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using MysteriousMapMod.Generation;
using MysteriousMapMod.Map;

namespace MysteriousMapMod.Patches;

/// <summary>
/// Draws an Isaac-style room shell around each compact route. The shell is
/// presentation-only: its visibility follows the map nodes and paths, so it
/// cannot reveal an undiscovered hidden room or a sealed connection.
/// </summary>
internal static class NestedRoomFrameRenderer
{
    private const string RootName = "MysteriousMapOuterRoomFrames";
    private const float InnerSpacing = 48f;
    private static readonly Vector2 RoomSize = new(244f, 244f);
    private static readonly FieldInfo PointsField = AccessTools.Field(typeof(NMapScreen), "_points");

    internal static void Apply(
        NMapScreen screen,
        RunState runState,
        IReadOnlyDictionary<MapCoord, NMapPoint> nodes,
        IReadOnlyDictionary<(MapCoord, MapCoord), IReadOnlyList<TextureRect>> paths)
    {
        var pointsContainer = (Control)PointsField.GetValue(screen)!;
        var oldRoot = pointsContainer.GetNodeOrNull<Control>(RootName);
        if (oldRoot != null)
        {
            pointsContainer.RemoveChild(oldRoot);
            oldRoot.QueueFree();
        }

        if (!MysteriousMapRules.IsNestedMap(runState.Map))
            return;

        var groups = nodes.Values
            .GroupBy(node => GetOuterCell(node.Point.coord))
            .ToDictionary(group => group.Key, group => group.ToList());
        var visibleCells = groups
            .Where(pair => pair.Value.Any(node => node.Visible))
            .Select(pair => pair.Key)
            .ToHashSet();
        if (visibleCells.Count == 0)
            return;

        var visibleDoors = paths
            .Where(pair => pair.Value.Any(dot => dot.Visible))
            .Select(pair => (First: GetOuterCell(pair.Key.Item1), Second: GetOuterCell(pair.Key.Item2)))
            .Where(pair => pair.First != pair.Second)
            .Select(pair => CanonicalDoor(pair.First, pair.Second))
            .ToHashSet();

        var root = new Control
        {
            Name = RootName,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        pointsContainer.AddChild(root);
        // Draw after the parchment but before every route icon. A negative
        // CanvasItem ZIndex puts the whole shell behind the parchment layer.
        pointsContainer.MoveChild(root, 0);

        GridCell? currentCell = runState.VisitedMapCoords.Count > 0
            ? GetOuterCell(runState.VisitedMapCoords[^1])
            : null;
        var toPointsLocal = pointsContainer.GetGlobalTransformWithCanvas().AffineInverse();

        foreach (var cell in visibleCells.OrderBy(cell => cell.Row).ThenBy(cell => cell.Col))
        {
            var roomNodes = groups[cell];
            var reference = roomNodes.FirstOrDefault(node => node is NNormalMapPoint) ?? roomNodes[0];
            var referenceCenter = toPointsLocal *
                                  (reference.GetGlobalTransformWithCanvas() * reference.PivotOffset);
            var blockCenterCoord = new Vector2(
                cell.Col * NestedMapCoordinates.BlockSize + 2f,
                cell.Row * NestedMapCoordinates.BlockSize + 2f);
            var roomCenter = new Vector2(
                referenceCenter.X - (reference.Point.coord.col - blockCenterCoord.X) * InnerSpacing,
                referenceCenter.Y + (reference.Point.coord.row - blockCenterCoord.Y) * InnerSpacing);

            var isCurrent = currentCell.HasValue && currentCell.Value == cell;
            var isCleared = roomNodes.Any(node =>
                runState.VisitedMapCoords.Contains(node.Point.coord) &&
                (MysteriousMapRules.IsNestedExit(node.Point) || node.Point == runState.Map.BossMapPoint));
            var panel = CreateRoomPanel(cell, roomCenter, isCurrent, isCleared);
            root.AddChild(panel);

            AddDoorIfConnected(panel, cell, new GridCell(cell.Col - 1, cell.Row), visibleCells, visibleDoors, DoorSide.Left);
            AddDoorIfConnected(panel, cell, new GridCell(cell.Col + 1, cell.Row), visibleCells, visibleDoors, DoorSide.Right);
            AddDoorIfConnected(panel, cell, new GridCell(cell.Col, cell.Row + 1), visibleCells, visibleDoors, DoorSide.Top);
            AddDoorIfConnected(panel, cell, new GridCell(cell.Col, cell.Row - 1), visibleCells, visibleDoors, DoorSide.Bottom);
        }
    }

    private static Panel CreateRoomPanel(GridCell cell, Vector2 center, bool current, bool cleared)
    {
        var border = current
            ? new Color(0.96f, 0.72f, 0.22f, 0.95f)
            : cleared
                ? new Color(0.38f, 0.73f, 0.67f, 0.78f)
                : new Color(0.42f, 0.37f, 0.55f, 0.72f);
        var background = current
            ? new Color(0.30f, 0.20f, 0.08f, 0.13f)
            : new Color(0.10f, 0.08f, 0.16f, 0.08f);
        var style = new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = current ? 5 : 3,
            BorderWidthTop = current ? 5 : 3,
            BorderWidthRight = current ? 5 : 3,
            BorderWidthBottom = current ? 5 : 3,
            CornerRadiusTopLeft = 18,
            CornerRadiusTopRight = 18,
            CornerRadiusBottomLeft = 18,
            CornerRadiusBottomRight = 18
        };
        var panel = new Panel
        {
            Name = $"Room_{cell.Col}_{cell.Row}",
            Position = center - RoomSize * 0.5f,
            Size = RoomSize,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        panel.AddThemeStyleboxOverride("panel", style);

        var label = new Label
        {
            Text = current ? "当前房间" : cleared ? "已清空" : "房间",
            Position = new Vector2(14f, 10f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeColorOverride("font_color", border);
        label.AddThemeFontSizeOverride("font_size", current ? 18 : 15);
        panel.AddChild(label);
        return panel;
    }

    private static void AddDoorIfConnected(
        Panel panel,
        GridCell room,
        GridCell neighbor,
        IReadOnlySet<GridCell> visibleCells,
        IReadOnlySet<(GridCell, GridCell)> visibleDoors,
        DoorSide side)
    {
        if (!visibleCells.Contains(neighbor) || !visibleDoors.Contains(CanonicalDoor(room, neighbor)))
            return;

        var vertical = side is DoorSide.Left or DoorSide.Right;
        var size = vertical ? new Vector2(12f, 42f) : new Vector2(42f, 12f);
        var position = side switch
        {
            DoorSide.Left => new Vector2(-4f, (RoomSize.Y - size.Y) * 0.5f),
            DoorSide.Right => new Vector2(RoomSize.X - 8f, (RoomSize.Y - size.Y) * 0.5f),
            DoorSide.Top => new Vector2((RoomSize.X - size.X) * 0.5f, -4f),
            _ => new Vector2((RoomSize.X - size.X) * 0.5f, RoomSize.Y - 8f)
        };
        panel.AddChild(new ColorRect
        {
            Position = position,
            Size = size,
            Color = new Color(0.82f, 0.68f, 0.38f, 0.95f),
            MouseFilter = Control.MouseFilterEnum.Ignore
        });
    }

    private static GridCell GetOuterCell(MapCoord coord) =>
        new(coord.col / NestedMapCoordinates.BlockSize, coord.row / NestedMapCoordinates.BlockSize);

    private static (GridCell, GridCell) CanonicalDoor(GridCell first, GridCell second) =>
        first.Col < second.Col || first.Col == second.Col && first.Row <= second.Row
            ? (first, second)
            : (second, first);

    private enum DoorSide
    {
        Left,
        Right,
        Top,
        Bottom
    }
}
