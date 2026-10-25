namespace MysteriousMapMod.Generation;

internal readonly record struct GridCell(int Col, int Row) : IComparable<GridCell>
{
    public int CompareTo(GridCell other) => (Col, Row).CompareTo((other.Col, other.Row));
}

internal readonly record struct GridEdge(GridCell Parent, GridCell Child);

internal sealed record GridTreeLayout(
    IReadOnlyList<GridCell> Cells,
    IReadOnlyList<GridEdge> Edges,
    GridCell Start,
    IReadOnlyList<GridCell> Leaves,
    IReadOnlyDictionary<GridCell, int> Distances);

/// <summary>
/// Builds a connected cardinal grid graph. Expansion prefers cells that touch
/// only one existing room, keeping branches apart. If contact is unavoidable,
/// every positional adjacency is added as an edge and may form a small mesh.
/// Degree-one rooms are the only dead ends.
/// </summary>
internal static class GridTreeGenerator
{
    internal const int PreferredMaxDiameter = 16;
    internal const double DiameterRerollChancePerStep = 0.25;
    internal const double MaximumDiameterRerollChance = 0.90;

    private static readonly GridCell[] Directions =
    [
        new(-1, 0), new(1, 0), new(0, -1), new(0, 1)
    ];

    public static GridTreeLayout Generate(int width, int height, int nodeCount, int targetLeaves, ulong seed)
    {
        if (nodeCount <= targetLeaves + 2)
            throw new ArgumentOutOfRangeException(nameof(nodeCount));

        if (width < 4 || height < 4)
            throw new ArgumentOutOfRangeException(nameof(width));

        for (var attempt = 0; attempt < 1024; attempt++)
        {
            var random = new StableRandom(seed + (ulong)attempt * 0x9E3779B97F4A7C15UL);
            var start = new GridCell(width / 2, height / 2);
            var cells = new List<GridCell>(nodeCount) { start };
            var occupied = new HashSet<GridCell> { start };
            var edges = new List<GridEdge>(nodeCount - 1);
            var degree = new Dictionary<GridCell, int> { [start] = 0 };

            while (cells.Count < nodeCount)
            {
                var leafCount = GetLeaves(cells, degree, start).Count;
                var moves = GetCandidateMoves(occupied, degree, start, width, height);
                if (moves.Count == 0)
                    break;

                List<CandidateMove> viable;
                if (leafCount < targetLeaves)
                {
                    viable = moves.Where(move => move.ResultingLeafCount <= targetLeaves).ToList();
                    if (viable.Count == 0)
                        break;
                    var bestLeafCount = viable.Max(move => move.ResultingLeafCount);
                    viable = viable.Where(move => move.ResultingLeafCount == bestLeafCount).ToList();
                }
                else
                {
                    viable = moves.Where(move => move.ResultingLeafCount == targetLeaves).ToList();
                    if (viable.Count == 0)
                        break;
                }

                // Among equally useful moves, touching fewer rooms always wins.
                // Extra contacts—and therefore mesh edges—are only a fallback.
                var fewestContacts = viable.Min(move => move.Neighbors.Count);
                viable = viable.Where(move => move.Neighbors.Count == fewestContacts).ToList();
                var selected = viable[random.Next(viable.Count)];
                var child = selected.Cell;
                occupied.Add(child);
                cells.Add(child);
                degree[child] = selected.Neighbors.Count;
                foreach (var neighbor in selected.Neighbors)
                {
                    edges.Add(new GridEdge(neighbor, child));
                    degree[neighbor]++;
                }
            }

            var finalLeaves = GetLeaves(cells, degree, start);
            if (cells.Count != nodeCount || finalLeaves.Count != targetLeaves)
                continue;

            if (edges.Count < cells.Count - 1 || HasUnconnectedAdjacentRooms(cells, edges))
                continue;

            var distances = ComputeDistances(start, cells, edges);
            var layout = new GridTreeLayout(cells, edges, start, finalLeaves.Order().ToList(), distances);
            var farthestLeaf = layout.Leaves
                .OrderByDescending(cell => distances[cell])
                .ThenBy(cell => cell)
                .First();
            // Reserve enough isolated grid space for the full boss chain. Normal
            // acts use the first cell; double-boss acts use both cells.
            if (FindSpacedExtensionPath(layout, farthestLeaf, 2, width, height) is null)
                continue;

            var diameter = ComputeDiameter(layout);
            if (random.NextDouble() < GetDiameterRerollChance(diameter))
                continue;

            return layout;
        }

        throw new InvalidOperationException(
            $"Could not generate a {nodeCount}-node spaced grid graph with {targetLeaves} leaves.");
    }

    private static HashSet<GridCell> GetLeaves(
        IEnumerable<GridCell> cells,
        IReadOnlyDictionary<GridCell, int> degree,
        GridCell start)
    {
        return cells.Where(c => c != start && degree[c] == 1).ToHashSet();
    }

    public static GridCell? FindSpacedExtension(
        GridTreeLayout layout,
        GridCell leaf,
        int width,
        int height)
        => FindSpacedExtensionPath(layout, leaf, 1, width, height)?.Single();

    public static IReadOnlyList<GridCell>? FindSpacedExtensionPath(
        GridTreeLayout layout,
        GridCell leaf,
        int length,
        int width,
        int height)
    {
        if (length <= 0)
            throw new ArgumentOutOfRangeException(nameof(length));

        var connectedNeighbor = layout.Edges
            .Where(edge => edge.Parent == leaf || edge.Child == leaf)
            .Select(edge => edge.Parent == leaf ? edge.Child : edge.Parent)
            .Single();
        var occupied = layout.Cells.ToHashSet();
        var path = new List<GridCell>(length);

        return TryExtend(connectedNeighbor, leaf) ? path.ToList() : null;

        bool TryExtend(GridCell previous, GridCell current)
        {
            if (path.Count == length)
                return true;

            var forward = new GridCell(current.Col - previous.Col, current.Row - previous.Row);
            var directions = Directions
                .OrderByDescending(direction => direction == forward)
                .ThenBy(direction => direction)
                .ToList();

            foreach (var direction in directions)
            {
                var candidate = new GridCell(current.Col + direction.Col, current.Row + direction.Row);
                if (candidate.Col < 0 || candidate.Col >= width || candidate.Row < 0 || candidate.Row >= height ||
                    occupied.Contains(candidate))
                {
                    continue;
                }

                var occupiedNeighbors = Directions
                    .Select(offset => new GridCell(candidate.Col + offset.Col, candidate.Row + offset.Row))
                    .Where(occupied.Contains)
                    .ToList();
                if (occupiedNeighbors.Count != 1 || occupiedNeighbors[0] != current)
                    continue;

                occupied.Add(candidate);
                path.Add(candidate);
                if (TryExtend(current, candidate))
                    return true;
                path.RemoveAt(path.Count - 1);
                occupied.Remove(candidate);
            }

            return false;
        }
    }

    private sealed record CandidateMove(
        GridCell Cell,
        IReadOnlyList<GridCell> Neighbors,
        int ResultingLeafCount);

    private static List<CandidateMove> GetCandidateMoves(
        IReadOnlySet<GridCell> occupied,
        IReadOnlyDictionary<GridCell, int> degree,
        GridCell start,
        int width,
        int height)
    {
        var currentLeafCount = degree.Count(pair => pair.Key != start && pair.Value == 1);
        var candidates = new HashSet<GridCell>();
        foreach (var cell in occupied)
        {
            foreach (var direction in Directions)
            {
                var candidate = new GridCell(cell.Col + direction.Col, cell.Row + direction.Row);
                if (candidate.Col >= 0 && candidate.Col < width && candidate.Row >= 0 && candidate.Row < height &&
                    !occupied.Contains(candidate))
                {
                    candidates.Add(candidate);
                }
            }
        }

        var result = new List<CandidateMove>(candidates.Count);
        foreach (var candidate in candidates)
        {
            var neighbors = Directions
                .Select(offset => new GridCell(candidate.Col + offset.Col, candidate.Row + offset.Row))
                .Where(occupied.Contains)
                .Order()
                .ToList();
            var lostLeaves = neighbors.Count(neighbor => neighbor != start && degree[neighbor] == 1);
            var gainedLeaves = neighbors.Count == 1 ? 1 : 0;
            result.Add(new CandidateMove(candidate, neighbors, currentLeafCount - lostLeaves + gainedLeaves));
        }

        return result;
    }

    private static bool HasUnconnectedAdjacentRooms(
        IEnumerable<GridCell> cells,
        IEnumerable<GridEdge> edges)
    {
        var occupied = cells.ToHashSet();
        var connected = edges
            .Select(edge => Order(edge.Parent, edge.Child))
            .ToHashSet();
        foreach (var cell in occupied)
        {
            foreach (var direction in Directions)
            {
                var neighbor = new GridCell(cell.Col + direction.Col, cell.Row + direction.Row);
                if (occupied.Contains(neighbor) && !connected.Contains(Order(cell, neighbor)))
                    return true;
            }
        }
        return false;
    }

    private static (GridCell First, GridCell Second) Order(GridCell a, GridCell b) =>
        a.CompareTo(b) <= 0 ? (a, b) : (b, a);

    private static Dictionary<GridCell, int> ComputeDistances(
        GridCell start,
        IEnumerable<GridCell> cells,
        IEnumerable<GridEdge> edges)
    {
        var adjacency = cells.ToDictionary(c => c, _ => new List<GridCell>());
        foreach (var edge in edges)
        {
            adjacency[edge.Parent].Add(edge.Child);
            adjacency[edge.Child].Add(edge.Parent);
        }

        var distances = new Dictionary<GridCell, int> { [start] = 0 };
        var queue = new Queue<GridCell>();
        queue.Enqueue(start);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var neighbor in adjacency[current])
            {
                if (distances.TryAdd(neighbor, distances[current] + 1))
                    queue.Enqueue(neighbor);
            }
        }
        return distances;
    }

    internal static int ComputeDiameter(GridTreeLayout layout)
    {
        var diameter = 0;
        foreach (var origin in layout.Cells)
        {
            var distances = ComputeDistances(origin, layout.Cells, layout.Edges);
            diameter = Math.Max(diameter, distances.Values.Max());
        }
        return diameter;
    }

    internal static double GetDiameterRerollChance(int diameter)
    {
        var excess = diameter - PreferredMaxDiameter;
        return excess <= 0
            ? 0d
            : Math.Min(MaximumDiameterRerollChance, excess * DiameterRerollChancePerStep);
    }
}

internal sealed class StableRandom
{
    private ulong _state;

    public StableRandom(ulong seed) => _state = seed == 0 ? 0xD1B54A32D192ED03UL : seed;

    public int Next(int exclusiveMax)
    {
        if (exclusiveMax <= 0)
            throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
        return (int)(NextUlong() % (uint)exclusiveMax);
    }

    public double NextDouble() => (NextUlong() >> 11) * (1.0 / (1UL << 53));

    public void Shuffle<T>(IList<T> values)
    {
        for (var i = values.Count - 1; i > 0; i--)
        {
            var j = Next(i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }

    private ulong NextUlong()
    {
        _state += 0x9E3779B97F4A7C15UL;
        var z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}
