namespace MysteriousMapMod.Generation;

internal enum OuterRoomKind
{
    Start,
    Normal,
    Treasure,
    Boss,
    Secret,
    SuperSecret
}

internal enum InnerNodeRole
{
    Entry,
    ChoiceLeft,
    ChoiceRight,
    BranchLeft,
    BranchMiddle,
    BranchRight,
    Reward,
    Exit,
    Boss,
    SecondBoss
}

internal sealed record OuterRoomLayout(
    GridCell Cell,
    OuterRoomKind Kind,
    IReadOnlyList<GridCell> Neighbors,
    int DistanceFromStart);

internal sealed record NestedFloorLayout(
    IReadOnlyList<OuterRoomLayout> Rooms,
    GridCell Start,
    GridCell Boss,
    GridCell Treasure,
    GridCell Secret,
    GridCell SuperSecret);

/// <summary>
/// Generates the five-by-five outer floor used by the interactive prototype.
/// Eleven visible rooms form a cardinal tree. A regular secret room prefers
/// cells touching several visible rooms, while the super-secret room touches
/// exactly one non-special room as far from the start as possible.
/// </summary>
internal static class NestedFloorGenerator
{
    public const int OuterSize = 5;
    public const int VisibleRoomCount = 11;

    private static readonly GridCell[] Directions =
    [
        new(-1, 0), new(1, 0), new(0, -1), new(0, 1)
    ];

    public static NestedFloorLayout Generate(ulong seed, bool extraSingleplayerTreasures = false)
    {
        for (var attempt = 0; attempt < 2048; attempt++)
        {
            var random = new StableRandom(seed + (ulong)attempt * 0x9E3779B97F4A7C15UL);
            var start = new GridCell(OuterSize / 2, OuterSize / 2);
            var cells = new List<GridCell> { start };
            var occupied = new HashSet<GridCell> { start };

            while (cells.Count < VisibleRoomCount)
            {
                var candidates = cells
                    .SelectMany(Neighbors)
                    .Where(IsInside)
                    .Where(cell => !occupied.Contains(cell))
                    .Distinct()
                    .Where(cell => Neighbors(cell).Count(occupied.Contains) == 1)
                    .Order()
                    .ToList();
                if (candidates.Count == 0)
                    break;

                var chosen = candidates[random.Next(candidates.Count)];
                cells.Add(chosen);
                occupied.Add(chosen);
            }

            if (cells.Count != VisibleRoomCount)
                continue;

            var distances = ComputeDistances(start, occupied);
            var leaves = cells
                .Where(cell => cell != start && Neighbors(cell).Count(occupied.Contains) == 1)
                .OrderByDescending(cell => distances[cell])
                .ThenBy(cell => cell)
                .ToList();
            if (leaves.Count < 2)
                continue;

            var boss = leaves[0];
            var treasure = leaves[1];
            var empty = AllCells().Where(cell => !occupied.Contains(cell)).ToList();

            var superCandidates = empty
                .Select(cell => new { Cell = cell, Adjacent = Neighbors(cell).Where(occupied.Contains).ToList() })
                .Where(candidate => candidate.Adjacent.Count == 1)
                .Where(candidate => candidate.Adjacent[0] != boss && candidate.Adjacent[0] != treasure &&
                                    candidate.Adjacent[0] != start)
                .ToList();
            if (superCandidates.Count == 0)
                continue;

            var farthestDistance = superCandidates.Max(candidate => distances[candidate.Adjacent[0]]);
            var farthestSuperCandidates = superCandidates
                .Where(candidate => distances[candidate.Adjacent[0]] == farthestDistance)
                .OrderBy(candidate => candidate.Cell)
                .ToList();
            var superSecret = farthestSuperCandidates[random.Next(farthestSuperCandidates.Count)].Cell;

            var secretCandidates = empty
                .Where(cell => cell != superSecret)
                .Select(cell => new
                {
                    Cell = cell,
                    Adjacent = Neighbors(cell).Where(occupied.Contains).Order().ToList()
                })
                .Where(candidate => candidate.Adjacent.Count > 0)
                .Where(candidate => !candidate.Adjacent.Contains(boss))
                .Where(candidate => !Neighbors(candidate.Cell).Contains(superSecret))
                .OrderBy(candidate => candidate.Cell)
                .ToList();
            if (secretCandidates.Count == 0)
                continue;

            // Isaac-style preference: 3-4 neighbors are best, two neighbors
            // receive -3 weight, and a single neighbor receives -6.
            var weighted = secretCandidates
                .Select(candidate => new
                {
                    candidate.Cell,
                    candidate.Adjacent,
                    Weight = 10 + random.Next(5) - (candidate.Adjacent.Count == 2 ? 3 :
                        candidate.Adjacent.Count == 1 ? 6 : 0)
                })
                .ToList();
            var bestWeight = weighted.Max(candidate => candidate.Weight);
            var bestSecretCandidates = weighted.Where(candidate => candidate.Weight == bestWeight).ToList();
            var secret = bestSecretCandidates[random.Next(bestSecretCandidates.Count)].Cell;

            // The first act already has a separate opening chest. Later
            // single-player acts get one more room for 3–4 chests total.
            var treasureRooms = new HashSet<GridCell> { treasure };
            var extraCandidates = cells.Where(cell => cell != start && cell != boss &&
                    cell != treasure && cell != superCandidates.Single(candidate =>
                        candidate.Cell == superSecret).Adjacent[0])
                .Order().ToList();
            var extraCount = 1 + random.Next(2) + (extraSingleplayerTreasures ? 1 : 0);
            for (var i = 0; i < extraCount; i++)
            {
                var distant = extraCandidates.Where(cell => distances[cell] >= 2).ToArray();
                var chosen = distant.Length > 0
                    ? distant[random.Next(distant.Length)]
                    : extraCandidates[random.Next(extraCandidates.Count)];
                treasureRooms.Add(chosen);
                extraCandidates.Remove(chosen);
            }

            var all = occupied.Concat([secret, superSecret]).ToHashSet();
            var roomLayouts = all
                .Order()
                .Select(cell => new OuterRoomLayout(
                    cell,
                    cell == start ? OuterRoomKind.Start :
                    cell == boss ? OuterRoomKind.Boss :
                    treasureRooms.Contains(cell) ? OuterRoomKind.Treasure :
                    cell == secret ? OuterRoomKind.Secret :
                    cell == superSecret ? OuterRoomKind.SuperSecret : OuterRoomKind.Normal,
                    Neighbors(cell).Where(all.Contains).Order().ToArray(),
                    cell == secret
                        ? weighted.Single(candidate => candidate.Cell == secret).Adjacent
                            .Min(neighbor => distances[neighbor]) + 1
                        : cell == superSecret
                            ? distances[superCandidates.Single(candidate => candidate.Cell == superSecret).Adjacent[0]] + 1
                            : distances[cell]))
                .ToArray();

            return new NestedFloorLayout(roomLayouts, start, boss, treasure, secret, superSecret);
        }

        throw new InvalidOperationException("Could not generate an Isaac-style outer floor with two hidden rooms.");
    }

    private static Dictionary<GridCell, int> ComputeDistances(GridCell start, IReadOnlySet<GridCell> occupied)
    {
        var result = new Dictionary<GridCell, int> { [start] = 0 };
        var queue = new Queue<GridCell>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighbor in Neighbors(current).Where(occupied.Contains))
            {
                if (result.TryAdd(neighbor, result[current] + 1))
                    queue.Enqueue(neighbor);
            }
        }
        return result;
    }

    private static IEnumerable<GridCell> AllCells()
    {
        for (var col = 0; col < OuterSize; col++)
        for (var row = 0; row < OuterSize; row++)
            yield return new GridCell(col, row);
    }

    private static IEnumerable<GridCell> Neighbors(GridCell cell) =>
        Directions.Select(direction => new GridCell(cell.Col + direction.Col, cell.Row + direction.Row));

    private static bool IsInside(GridCell cell) =>
        cell.Col >= 0 && cell.Col < OuterSize && cell.Row >= 0 && cell.Row < OuterSize;
}
