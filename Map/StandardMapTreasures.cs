using MegaCrit.Sts2.Core.Map;

namespace MysteriousMapMod.Map;

/// <summary>
/// Solo standard maps retain every original branch. Three or four complete
/// rows become treasure rooms, so no branch is required to collect them.
/// </summary>
internal static class StandardMapTreasures
{
    internal static int Apply(ActMap map, ulong seed, int actIndex) =>
        ApplyToPoints(map.StartingMapPoint, map.BossMapPoint,
            map.GetRowCount(), seed, actIndex);

    internal static int ApplyToPoints(MapPoint start, MapPoint boss,
        int rowCount, ulong seed, int actIndex)
    {
        var points = CollectReachable(start)
            .Where(point => point != start && point != boss &&
                point.coord.row > 0 && point.coord.row < rowCount - 1)
            .ToArray();
        if (points.Length == 0) return 0;

        var rows = points.GroupBy(point => point.coord.row)
            .Where(group => group.All(point => point.PointType is not
                (MapPointType.Ancient or MapPointType.Boss)))
            .OrderBy(group => group.Key)
            .ToArray();
        if (rows.Length == 0) return 0;

        var target = 3 + (int)((seed ^ ((ulong)(actIndex + 1) *
            0x9E3779B97F4A7C15UL)) & 1UL);
        var selected = new HashSet<int> { rows[0].Key };

        // Keep the vanilla guaranteed treasure row among our chest rows.
        // This prevents an extra, unavoidable chest outside the 3-4 rows.
        foreach (var row in rows.Where(group => group.Any(point =>
                     point.PointType == MapPointType.Treasure)))
            selected.Add(row.Key);

        while (selected.Count < target && selected.Count < rows.Length)
        {
            var next = rows.Where(group => !selected.Contains(group.Key))
                .OrderByDescending(group => selected.Min(row => Math.Abs(row - group.Key)))
                .ThenBy(group => StableOrder(seed, actIndex, group.Key))
                .First();
            selected.Add(next.Key);
        }

        foreach (var point in points.Where(point => selected.Contains(point.coord.row)))
        {
            point.PointType = MapPointType.Treasure;
            point.CanBeModified = false;
        }
        return selected.Count;
    }

    private static IEnumerable<MapPoint> CollectReachable(MapPoint start)
    {
        var visited = new HashSet<MapPoint>();
        var pending = new Queue<MapPoint>();
        pending.Enqueue(start);
        while (pending.Count > 0)
        {
            var point = pending.Dequeue();
            if (!visited.Add(point)) continue;
            foreach (var child in point.Children)
                pending.Enqueue(child);
        }
        return visited;
    }

    private static ulong StableOrder(ulong seed, int actIndex, int row)
    {
        var value = seed ^ ((ulong)(actIndex + 1) << 48) ^ (uint)row;
        value ^= value >> 33;
        value *= 0xff51afd7ed558ccdUL;
        value ^= value >> 33;
        value *= 0xc4ceb9fe1a85ec53UL;
        return value ^ (value >> 33);
    }
}
