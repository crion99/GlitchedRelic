using MysteriousMapMod.Generation;
using MysteriousMapMod.Rooms;

var preferred = EncounterRotationSelector.SelectIndex(
    4,
    1,
    index => index == 3,
    index => index != 1);
if (preferred != 3)
    throw new InvalidOperationException("Encounter selector did not prefer a non-repeating, non-shared-tag candidate.");

var relaxed = EncounterRotationSelector.SelectIndex(
    4,
    1,
    _ => false,
    index => index != 1);
if (relaxed != 2)
    throw new InvalidOperationException("Encounter selector did not relax tag separation before exact repetition.");

var unavoidable = EncounterRotationSelector.SelectIndex(
    1,
    7,
    _ => false,
    _ => false);
if (unavoidable != 0)
    throw new InvalidOperationException("Encounter selector did not fall back when repetition was unavoidable.");

for (ulong seed = 1; seed <= 1000; seed++)
{
    var floor = NestedFloorGenerator.Generate(seed);
    var clientFloor = NestedFloorGenerator.Generate(seed);
    var singleFloor = NestedFloorGenerator.Generate(seed, extraSingleplayerTreasures: true);
    var repeatedSingleFloor = NestedFloorGenerator.Generate(seed, extraSingleplayerTreasures: true);
    var fingerprint = string.Join('|', floor.Rooms.OrderBy(room => room.Cell)
        .Select(room => $"{room.Cell.Col},{room.Cell.Row}:{room.Kind}:" +
                        string.Join(',', room.Neighbors.Order())));
    var clientFingerprint = string.Join('|', clientFloor.Rooms.OrderBy(room => room.Cell)
        .Select(room => $"{room.Cell.Col},{room.Cell.Row}:{room.Kind}:" +
                        string.Join(',', room.Neighbors.Order())));
    if (fingerprint != clientFingerprint)
        throw new InvalidOperationException($"Nested seed {seed}: host/client floor fingerprints differ.");
    if (!singleFloor.Rooms.Select(room => (room.Cell, room.Kind))
            .SequenceEqual(repeatedSingleFloor.Rooms.Select(room => (room.Cell, room.Kind))) ||
        singleFloor.Rooms.Count(room => room.Kind == OuterRoomKind.Treasure) is < 3 or > 4 ||
        singleFloor.Rooms.Count(room => room.Kind == OuterRoomKind.Treasure) -
        floor.Rooms.Count(room => room.Kind == OuterRoomKind.Treasure) != 1)
        throw new InvalidOperationException($"Nested seed {seed}: extra single-player treasures are invalid.");
    if (floor.Rooms.Count != NestedFloorGenerator.VisibleRoomCount + 2 ||
        floor.Rooms.Count(room => room.Kind == OuterRoomKind.Start) != 1 ||
        floor.Rooms.Count(room => room.Kind == OuterRoomKind.Boss) != 1 ||
        floor.Rooms.Count(room => room.Kind == OuterRoomKind.Treasure) is < 2 or > 3 ||
        floor.Rooms.Count(room => room.Kind == OuterRoomKind.Secret) != 1 ||
        floor.Rooms.Count(room => room.Kind == OuterRoomKind.SuperSecret) != 1)
    {
        throw new InvalidOperationException($"Nested seed {seed}: outer-room composition is invalid.");
    }

    var secret = floor.Rooms.Single(room => room.Kind == OuterRoomKind.Secret);
    var superSecret = floor.Rooms.Single(room => room.Kind == OuterRoomKind.SuperSecret);
    if (secret.Neighbors.Count == 0 || secret.Neighbors.Contains(floor.Boss) ||
        superSecret.Neighbors.Count != 1 ||
        floor.Rooms.Single(room => room.Cell == superSecret.Neighbors[0]).Kind != OuterRoomKind.Normal)
    {
        throw new InvalidOperationException($"Nested seed {seed}: Isaac hidden-room placement is invalid.");
    }

}

Console.WriteLine("Validated 1000 deterministic nested floors: 2-3 ordinary treasure rooms, 3-4 on later single-player acts, and both hidden rooms.");

const int width = 13;
const int height = 13;
const int minNodes = 18;
const int maxNodes = 22;
const int leaves = 10;
var meshMapCount = 0;
var maxExtraEdges = 0;
var diameters = new List<int>();

for (var ordinaryRooms = 7; ordinaryRooms <= 11; ordinaryRooms++)
{
    var previousUnknownCount = (int)Math.Round(
        ordinaryRooms * OrdinaryRoomAllocator.BaseUnknownRoomChance,
        MidpointRounding.AwayFromZero);
    var adjustedUnknownCount = OrdinaryRoomAllocator.GetUnknownRoomCount(ordinaryRooms);
    if (adjustedUnknownCount != previousUnknownCount + 1 ||
        ordinaryRooms - adjustedUnknownCount != ordinaryRooms - previousUnknownCount - 1)
    {
        throw new InvalidOperationException(
            $"Ordinary-room allocation {ordinaryRooms}: expected one monster-to-unknown conversion, " +
            $"got {adjustedUnknownCount} unknown rooms.");
    }
}

for (ulong seed = 1; seed <= 1000; seed++)
{
    var nodeCountRandom = new StableRandom(seed ^ 0x8CB92BA72F3D8DD7UL);
    var nodes = minNodes + nodeCountRandom.Next(maxNodes - minNodes + 1);
    var layout = GridTreeGenerator.Generate(width, height, nodes, leaves, seed);
    var clientLayout = GridTreeGenerator.Generate(width, height, nodes, leaves, seed);
    if (Fingerprint(layout) != Fingerprint(clientLayout))
        throw new InvalidOperationException($"Seed {seed}: simulated host/client map fingerprints differ.");
    if (layout.Cells.Count != nodes)
        throw new InvalidOperationException($"Seed {seed}: expected {nodes} nodes, got {layout.Cells.Count}.");
    if (layout.Edges.Count < nodes - 1)
        throw new InvalidOperationException($"Seed {seed}: connected graph has too few edges ({layout.Edges.Count}).");
    var extraEdges = layout.Edges.Count - (nodes - 1);
    if (extraEdges > 0)
        meshMapCount++;
    maxExtraEdges = Math.Max(maxExtraEdges, extraEdges);
    if (layout.Leaves.Count != leaves)
        throw new InvalidOperationException($"Seed {seed}: expected {leaves} leaves, got {layout.Leaves.Count}.");
    if (layout.Distances.Count != nodes)
        throw new InvalidOperationException($"Seed {seed}: graph is disconnected.");
    diameters.Add(GridTreeGenerator.ComputeDiameter(layout));
    if (layout.Edges.Any(edge => Math.Abs(edge.Parent.Col - edge.Child.Col) + Math.Abs(edge.Parent.Row - edge.Child.Row) != 1))
        throw new InvalidOperationException($"Seed {seed}: non-cardinal edge found.");
    if (layout.Cells.Any(cell => cell.Col < 0 || cell.Col >= width || cell.Row < 0 || cell.Row >= height))
        throw new InvalidOperationException($"Seed {seed}: out-of-bounds cell found.");

    var degree = layout.Cells.ToDictionary(cell => cell, _ => 0);
    foreach (var edge in layout.Edges)
    {
        degree[edge.Parent]++;
        degree[edge.Child]++;
    }
    var actualLeaves = degree.Count(pair => pair.Key != layout.Start && pair.Value == 1);
    if (actualLeaves != leaves)
        throw new InvalidOperationException($"Seed {seed}: degree-derived leaf count is {actualLeaves}.");

    var connectedPairs = layout.Edges
        .Select(edge => edge.Parent.CompareTo(edge.Child) <= 0
            ? (edge.Parent, edge.Child)
            : (edge.Child, edge.Parent))
        .ToHashSet();
    foreach (var cell in layout.Cells)
    {
        foreach (var neighbor in layout.Cells.Where(other =>
                     Math.Abs(cell.Col - other.Col) + Math.Abs(cell.Row - other.Row) == 1))
        {
            var pair = cell.CompareTo(neighbor) <= 0 ? (cell, neighbor) : (neighbor, cell);
            if (!connectedPairs.Contains(pair))
                throw new InvalidOperationException($"Seed {seed}: adjacent rooms {cell}/{neighbor} are not connected.");
        }
    }

    var bossCampfire = layout.Leaves
        .OrderByDescending(cell => layout.Distances[cell])
        .ThenBy(cell => cell)
        .First();
    var appendedBosses = GridTreeGenerator.FindSpacedExtensionPath(layout, bossCampfire, 2, width, height);
    if (appendedBosses is null || appendedBosses.Count != 2)
    {
        throw new InvalidOperationException($"Seed {seed}: pre-boss rest site cannot receive a two-room boss chain.");
    }

    var firstBoss = appendedBosses[0];
    var secondBoss = appendedBosses[1];
    var firstBossBaseNeighbors = layout.Cells.Count(cell => Manhattan(firstBoss, cell) == 1);
    var secondBossBaseNeighbors = layout.Cells.Count(cell => Manhattan(secondBoss, cell) == 1);
    if (Manhattan(firstBoss, bossCampfire) != 1 ||
        Manhattan(secondBoss, firstBoss) != 1 ||
        firstBossBaseNeighbors != 1 ||
        secondBossBaseNeighbors != 0 ||
        degree[bossCampfire] != 1)
    {
        throw new InvalidOperationException($"Seed {seed}: isolated rest-to-boss-to-boss chain is invalid.");
    }

    var superHidden = layout.Leaves
        .OrderByDescending(cell => layout.Distances[cell])
        .ThenBy(cell => cell)
        .Skip(1)
        .First();
    var occupied = layout.Cells.Concat([firstBoss, secondBoss]).ToArray();
    var hiddenPlacement = HiddenRoomPlacement.Select(
        occupied, [firstBoss, secondBoss], width, height,
        new StableRandom(seed ^ 0xD1B54A32D192ED03UL));
    var clientHiddenPlacement = HiddenRoomPlacement.Select(
        occupied, [firstBoss, secondBoss], width, height,
        new StableRandom(seed ^ 0xD1B54A32D192ED03UL));
    if (hiddenPlacement.Cell != clientHiddenPlacement.Cell ||
        !hiddenPlacement.Neighbors.SequenceEqual(clientHiddenPlacement.Neighbors) ||
        occupied.Contains(hiddenPlacement.Cell) ||
        hiddenPlacement.Neighbors.Count == 0 ||
        hiddenPlacement.Neighbors.Contains(firstBoss) ||
        hiddenPlacement.Neighbors.Contains(secondBoss))
    {
        throw new InvalidOperationException($"Seed {seed}: weighted hidden-room placement is invalid.");
    }
}

Console.WriteLine($"Validated 1000 deterministic maps: 18-22 base-grid nodes with 10 degree-one leaves, a second-farthest super-secret leaf, one cube-weighted hidden room, complete cardinal adjacency, and an isolated rest-to-boss-to-boss chain. Incidental meshes: {meshMapCount}/1000; max extra edges: {maxExtraEdges}.");
Console.WriteLine($"Diameter distribution: min {diameters.Min()}, average {diameters.Average():F2}, max {diameters.Max()}; " +
                  string.Join(", ", diameters.GroupBy(value => value).OrderBy(group => group.Key)
                      .Select(group => $"{group.Key}:{group.Count()}")));

if (GridTreeGenerator.GetDiameterRerollChance(16) != 0d ||
    GridTreeGenerator.GetDiameterRerollChance(17) != 0.25d ||
    GridTreeGenerator.GetDiameterRerollChance(18) != 0.50d ||
    GridTreeGenerator.GetDiameterRerollChance(19) != 0.75d ||
    GridTreeGenerator.GetDiameterRerollChance(20) != 0.90d ||
    GridTreeGenerator.GetDiameterRerollChance(30) != 0.90d)
{
    throw new InvalidOperationException("Diameter reroll probability curve is invalid.");
}

static string Fingerprint(GridTreeLayout layout)
{
    var cells = string.Join(';', layout.Cells.Order().Select(cell => $"{cell.Col},{cell.Row}"));
    var edges = string.Join(';', layout.Edges
        .Select(edge => edge.Parent.CompareTo(edge.Child) <= 0
            ? (edge.Parent, edge.Child)
            : (edge.Child, edge.Parent))
        .OrderBy(edge => edge.Item1)
        .ThenBy(edge => edge.Item2)
        .Select(edge => $"{edge.Item1.Col},{edge.Item1.Row}>{edge.Item2.Col},{edge.Item2.Row}"));
    var distances = string.Join(';', layout.Distances.OrderBy(pair => pair.Key)
        .Select(pair => $"{pair.Key.Col},{pair.Key.Row}:{pair.Value}"));
    return $"{layout.Start.Col},{layout.Start.Row}|{cells}|{edges}|{distances}";
}

static int Manhattan(GridCell a, GridCell b) => Math.Abs(a.Col - b.Col) + Math.Abs(a.Row - b.Row);
