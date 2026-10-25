using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Runs;
using MysteriousMapMod.Generation;

namespace MysteriousMapMod.Map;

/// <summary>
/// A flattened representation of the "Spire inside rooms" design. Each cell
/// in the five-by-five Isaac-style floor owns a small one-way Spire route in a
/// separate coordinate block. The coordinate encoding survives vanilla saves.
/// </summary>
public sealed class MysteriousMapActMap : ActMap
{
    public const int Width = NestedMapCoordinates.GridSize;
    public const int Height = NestedMapCoordinates.GridSize;
    public const int WeakEncounterDistance = 2;

    private readonly Dictionary<MapCoord, int> _distances = new();

    public override MapPoint BossMapPoint { get; }
    public override MapPoint StartingMapPoint { get; }
    public override MapPoint? SecondBossMapPoint { get; }
    protected override MapPoint?[,] Grid { get; }

    public MysteriousMapActMap(RunState runState, int actIndex)
    {
        var seed = runState.Rng.Seed ^ ((ulong)(actIndex + 1) * 0xA24BAED4963EE407UL);
        var floor = NestedFloorGenerator.Generate(seed,
            extraSingleplayerTreasures: runState.Players.Count == 1 && actIndex > 0);
        var bossOuter = floor.Boss;
        // The first chest is part of the floor layout, not a side effect of
        // the optional starting Dataminer. Keep it even when that toggle is off.
        var guaranteedFirstTreasure = ShouldGuaranteeFirstTreasure(actIndex, runState.Players.Count);

        StartingMapPoint = NewPoint(floor.Start, InnerNodeRole.Entry, MapPointType.Ancient);
        BossMapPoint = NewPoint(bossOuter, InnerNodeRole.Boss, MapPointType.Boss);
        if (runState.Act.HasSecondBoss)
            SecondBossMapPoint = NewPoint(bossOuter, InnerNodeRole.SecondBoss, MapPointType.Boss);

        Grid = new MapPoint?[Width, Height];
        var routes = new Dictionary<GridCell, Dictionary<InnerNodeRole, MapPoint>>();

        foreach (var room in floor.Rooms)
        {
            var roomSeed = seed ^ ((ulong)(room.Cell.Col + 1) * 0x9E3779B97F4A7C15UL) ^
                           ((ulong)(room.Cell.Row + 1) * 0xD1B54A32D192ED03UL);
            var route = room.Kind == OuterRoomKind.Boss
                ? BuildBossRoute(room.Cell)
                : BuildStandardRoute(room.Cell, room.Kind, roomSeed,
                    guaranteedFirstTreasure && room.Kind == OuterRoomKind.Start,
                    StartingMapPoint);
            routes[room.Cell] = route;

            foreach (var (role, point) in route)
            {
                if (point != StartingMapPoint && point != BossMapPoint && point != SecondBossMapPoint)
                    Grid[point.coord.col, point.coord.row] = point;
                _distances[point.coord] = room.DistanceFromStart * 5 + RoleDistance(role);
            }

            if (room.Kind is OuterRoomKind.Secret or OuterRoomKind.SuperSecret)
                MysteriousMapRules.MarkHidden(route[InnerNodeRole.Entry]);
        }

        foreach (var room in floor.Rooms.Where(room => room.Kind != OuterRoomKind.Boss))
        {
            var exit = routes[room.Cell][InnerNodeRole.Exit];
            foreach (var neighbor in room.Neighbors)
                exit.AddChildPoint(routes[neighbor][InnerNodeRole.Entry]);
        }

        foreach (var child in StartingMapPoint.Children)
            startMapPoints.Add(child);
    }

    public int DistanceFromStart(MapCoord coord) =>
        _distances.TryGetValue(coord, out var value) ? value : int.MaxValue;

    internal static bool ShouldGuaranteeFirstTreasure(int actIndex, int playerCount) =>
        actIndex == 0 && playerCount > 0;

    private Dictionary<InnerNodeRole, MapPoint> BuildBossRoute(GridCell outer)
    {
        var entry = NewPoint(outer, InnerNodeRole.Entry, MapPointType.RestSite);
        var route = new Dictionary<InnerNodeRole, MapPoint>
        {
            [InnerNodeRole.Entry] = entry,
            [InnerNodeRole.Boss] = BossMapPoint
        };
        entry.AddChildPoint(BossMapPoint);
        if (SecondBossMapPoint != null)
        {
            route[InnerNodeRole.SecondBoss] = SecondBossMapPoint;
            BossMapPoint.AddChildPoint(SecondBossMapPoint);
        }
        return route;
    }

    internal static Dictionary<InnerNodeRole, MapPoint> BuildStandardRoute(
        GridCell outer,
        OuterRoomKind kind,
        ulong seed,
        bool guaranteedFirstTreasure,
        MapPoint startingMapPoint)
    {
        var random = new StableRandom(seed);
        var route = new Dictionary<InnerNodeRole, MapPoint>();
        MapPoint Point(InnerNodeRole role, MapPointType type)
        {
            var point = role == InnerNodeRole.Entry && kind == OuterRoomKind.Start
                ? startingMapPoint
                : NewPoint(outer, role, type);
            route[role] = point;
            return point;
        }

        var entry = Point(InnerNodeRole.Entry, MapPointType.Monster);
        var choiceLeft = Point(InnerNodeRole.ChoiceLeft, guaranteedFirstTreasure
            ? MapPointType.Treasure : RollRouteType(random, [MapPointType.Monster]));
        var choiceRight = Point(InnerNodeRole.ChoiceRight, RollRouteType(random, [MapPointType.Monster]));
        var branchLeft = Point(InnerNodeRole.BranchLeft, RollRouteType(random, [choiceLeft.PointType]));
        var branchMiddle = Point(
            InnerNodeRole.BranchMiddle,
            RollRouteType(random, [choiceLeft.PointType, choiceRight.PointType]));
        var branchRight = Point(InnerNodeRole.BranchRight, RollRouteType(random, [choiceRight.PointType]));
        var rewardType = kind is OuterRoomKind.Treasure or OuterRoomKind.Secret or OuterRoomKind.SuperSecret
            ? MapPointType.Treasure
            : random.Next(3) switch
            {
                0 => MapPointType.Monster,
                1 => MapPointType.Unknown,
                _ => MapPointType.RestSite
            };
        var reward = Point(InnerNodeRole.Reward, rewardType);
        var exit = Point(InnerNodeRole.Exit, MapPointType.Unassigned);

        entry.AddChildPoint(choiceLeft);
        if (guaranteedFirstTreasure)
            choiceLeft.AddChildPoint(choiceRight);
        else
            entry.AddChildPoint(choiceRight);
        choiceLeft.AddChildPoint(branchLeft);
        choiceLeft.AddChildPoint(branchMiddle);
        choiceRight.AddChildPoint(branchMiddle);
        choiceRight.AddChildPoint(branchRight);
        branchLeft.AddChildPoint(reward);
        branchMiddle.AddChildPoint(reward);
        branchRight.AddChildPoint(reward);
        reward.AddChildPoint(exit);
        return route;
    }

    private static MapPointType RollRouteType(StableRandom random, IReadOnlyCollection<MapPointType> parents)
    {
        var candidates = new List<MapPointType>
        {
            MapPointType.Monster,
            MapPointType.Monster,
            MapPointType.Unknown,
            MapPointType.Unknown,
            MapPointType.RestSite,
            MapPointType.Shop,
            MapPointType.Elite
        };
        candidates.RemoveAll(type =>
            type is MapPointType.RestSite or MapPointType.Shop or MapPointType.Elite && parents.Contains(type));
        return candidates[random.Next(candidates.Count)];
    }

    private static MapPoint NewPoint(GridCell outer, InnerNodeRole role, MapPointType type)
    {
        var coord = NestedMapCoordinates.ToCoord(outer, role);
        return new MapPoint(coord.col, coord.row)
        {
            PointType = type,
            CanBeModified = false
        };
    }

    private static int RoleDistance(InnerNodeRole role) => role switch
    {
        InnerNodeRole.Entry => 0,
        InnerNodeRole.ChoiceLeft or InnerNodeRole.ChoiceRight or InnerNodeRole.Boss => 1,
        InnerNodeRole.BranchLeft or InnerNodeRole.BranchMiddle or InnerNodeRole.BranchRight or
            InnerNodeRole.SecondBoss => 2,
        InnerNodeRole.Reward => 3,
        InnerNodeRole.Exit => 4,
        _ => 0
    };
}
