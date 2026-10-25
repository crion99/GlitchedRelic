namespace MysteriousMapMod.Generation;

internal sealed record HiddenRoomPlacementResult(GridCell Cell, IReadOnlyList<GridCell> Neighbors);

internal static class HiddenRoomPlacement
{
    private static readonly GridCell[] Directions =
    [
        new(1, 0),
        new(-1, 0),
        new(0, 1),
        new(0, -1)
    ];

    public static HiddenRoomPlacementResult Select(
        IReadOnlyCollection<GridCell> occupied,
        IReadOnlyCollection<GridCell> bossCells,
        int width,
        int height,
        StableRandom random)
    {
        var occupiedSet = occupied.ToHashSet();
        var bossSet = bossCells.ToHashSet();
        var candidates = new List<HiddenRoomPlacementResult>();

        for (var col = 0; col < width; col++)
        for (var row = 0; row < height; row++)
        {
            var cell = new GridCell(col, row);
            if (occupiedSet.Contains(cell))
                continue;

            var adjacent = Neighbors(cell)
                .Where(neighbor => occupiedSet.Contains(neighbor))
                .Order()
                .ToList();
            if (adjacent.Count == 0 || adjacent.Any(bossSet.Contains))
                continue;

            candidates.Add(new HiddenRoomPlacementResult(cell, adjacent));
        }

        if (candidates.Count == 0)
            throw new InvalidOperationException("No valid hidden-room position remains after excluding boss-adjacent cells.");

        candidates.Sort((left, right) => left.Cell.CompareTo(right.Cell));
        var totalWeight = candidates.Sum(candidate => Cube(candidate.Neighbors.Count));
        var roll = random.Next(totalWeight);
        foreach (var candidate in candidates)
        {
            roll -= Cube(candidate.Neighbors.Count);
            if (roll < 0)
                return candidate;
        }

        return candidates[^1];
    }

    private static IEnumerable<GridCell> Neighbors(GridCell cell)
    {
        foreach (var direction in Directions)
            yield return new GridCell(cell.Col + direction.Col, cell.Row + direction.Row);
    }

    private static int Cube(int value) => value * value * value;
}
