using System.Runtime.CompilerServices;
using System.Text.Json;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MysteriousMapMod.Relics;

namespace MysteriousMapMod.Map;

public sealed class MysteriousMapHiddenRoomStateModifier : ModifierModel
{
    [SavedProperty]
    public string RevealedRooms { get; set; } = string.Empty;

    public override bool ShouldReceiveCombatHooks => false;
}

internal sealed record HiddenRoomReveal(int ActIndex, int Col, int Row);
internal sealed record HiddenRoomRevealEnvelope(
    int Schema,
    IReadOnlyList<HiddenRoomReveal>? Rooms,
    IReadOnlyList<HiddenRoomReveal>? HiddenRooms = null,
    IReadOnlyList<BombActSnapshot>? Bombs = null);
internal sealed record BombActSnapshot(
    int ActIndex,
    int Count,
    IReadOnlyList<HiddenRoomReveal>? ClaimedDrops = null,
    IReadOnlyList<HiddenRoomReveal>? PurchasedShops = null,
    IReadOnlyList<HiddenRoomReveal>? BombedWalls = null);

internal static class HiddenRoomState
{
    private const int SchemaVersion = 3;
    private const string PayloadProperty = nameof(MysteriousMapHiddenRoomStateModifier.RevealedRooms);
    private static readonly ConditionalWeakTable<RunState, HashSet<(int Act, MapCoord Coord)>> Revealed = new();
    private static readonly ConditionalWeakTable<RunState, Dictionary<int, BombActRuntime>> Bombs = new();

    private sealed class BombActRuntime
    {
        public int Count;
        public HashSet<MapCoord> ClaimedDrops { get; } = [];
        public HashSet<MapCoord> PurchasedShops { get; } = [];
        public HashSet<MapCoord> BombedWalls { get; } = [];
    }

    internal static bool IsRevealed(RunState runState, MapPoint point) =>
        runState.VisitedMapCoords.Contains(point.coord) ||
        Revealed.TryGetValue(runState, out var rooms) &&
        rooms.Contains((runState.CurrentActIndex, point.coord));

    internal static IReadOnlyList<MapCoord> RevealAdjacent(RunState runState, MapCoord source)
    {
        var sourcePoint = runState.Map.GetPoint(source);
        if (sourcePoint == null)
            return Array.Empty<MapCoord>();

        var rooms = Revealed.GetValue(runState, _ => []);
        var newlyRevealed = new List<MapCoord>();
        foreach (var neighbor in MysteriousMapRules.GetNeighbors(sourcePoint)
                     .Where(MysteriousMapRules.IsHiddenRoom)
                     .OrderBy(point => point.coord))
        {
            if (rooms.Add((runState.CurrentActIndex, neighbor.coord)))
                newlyRevealed.Add(neighbor.coord);
        }
        return newlyRevealed;
    }

    internal static int GetBombCount(RunState runState) => GetBombAct(runState).Count;

    internal static void AddGlitchBomb(RunState runState) => GetBombAct(runState).Count++;

    internal static bool TryConsumeBomb(RunState runState)
    {
        var state = GetBombAct(runState);
        if (state.Count <= 0)
            return false;
        state.Count--;
        return true;
    }

    internal static bool CanBuyBomb(RunState runState, MapCoord shop) =>
        MysteriousMapRules.IsNestedMap(runState.Map) && !GetBombAct(runState).PurchasedShops.Contains(shop);

    internal static bool AddPurchasedBomb(RunState runState, MapCoord shop)
    {
        var state = GetBombAct(runState);
        if (!state.PurchasedShops.Add(shop))
            return false;
        state.Count += BombPickupAmount(runState);
        return true;
    }

    internal static bool TryClaimBombDrop(RunState runState, MapPoint exit)
    {
        if (!MysteriousMapRules.IsNestedMap(runState.Map) ||
            !MysteriousMapRules.IsNestedExit(exit) ||
            runState.VisitedMapCoords.Contains(exit.coord) ||
            !GetBombDropExits(runState).Contains(exit.coord))
            return false;
        var state = GetBombAct(runState);
        if (!state.ClaimedDrops.Add(exit.coord))
            return false;
        state.Count += BombPickupAmount(runState);
        return true;
    }

    internal static bool CanBombWall(RunState runState, int deltaCol, int deltaRow)
    {
        if (GetBombCount(runState) <= 0 || runState.VisitedMapCoords.Count == 0)
            return false;
        var current = runState.Map.GetPoint(runState.VisitedMapCoords[^1]);
        if (current == null ||
            !NestedMapCoordinates.TryDecode(current.coord, out var outer, out _))
            return false;
        var target = new Generation.GridCell(outer.Col + deltaCol, outer.Row + deltaRow);
        if (target.Col < 0 || target.Col >= Generation.NestedFloorGenerator.OuterSize ||
            target.Row < 0 || target.Row >= Generation.NestedFloorGenerator.OuterSize)
            return false;
        var targetCoord = NestedMapCoordinates.ToCoord(target, Generation.InnerNodeRole.Entry);
        if (GetBombAct(runState).BombedWalls.Contains(targetCoord))
            return false;
        var entry = runState.Map.GetPoint(targetCoord);
        return entry == null || MysteriousMapRules.IsHiddenRoom(entry) && !IsRevealed(runState, entry);
    }

    internal static bool TryBombWall(RunState runState, int deltaCol, int deltaRow, out bool discovered)
    {
        discovered = false;
        if (!CanBombWall(runState, deltaCol, deltaRow))
            return false;
        var current = runState.Map.GetPoint(runState.VisitedMapCoords[^1])!;
        NestedMapCoordinates.TryDecode(current.coord, out var outer, out _);
        var target = new Generation.GridCell(outer.Col + deltaCol, outer.Row + deltaRow);
        var targetCoord = NestedMapCoordinates.ToCoord(target, Generation.InnerNodeRole.Entry);
        var entry = runState.Map.GetPoint(targetCoord);
        var bombState = GetBombAct(runState);
        bombState.Count--;
        bombState.BombedWalls.Add(targetCoord);
        if (entry != null && MysteriousMapRules.IsHiddenRoom(entry))
        {
            var rooms = Revealed.GetValue(runState, _ => []);
            discovered = rooms.Add((runState.CurrentActIndex, entry.coord));
        }
        return true;
    }

    internal static string? Capture(RunState runState)
    {
        var revealed = Revealed.TryGetValue(runState, out var rooms)
            ? rooms.OrderBy(item => item.Act)
            .ThenBy(item => item.Coord)
            .Select(item => new HiddenRoomReveal(item.Act, item.Coord.col, item.Coord.row))
            .ToArray()
            : [];
        var identities = MysteriousMapRules.IsActive(runState.Map)
            ? MysteriousMapRules.GetHiddenRoomCoords(runState.Map)
                .Select(coord => new HiddenRoomReveal(
                    runState.CurrentActIndex, coord.col, coord.row))
                .ToArray()
            : [];
        var bombs = Bombs.TryGetValue(runState, out var bombActs)
            ? bombActs.OrderBy(pair => pair.Key)
                .Select(pair => new BombActSnapshot(
                    pair.Key,
                    pair.Value.Count,
                    pair.Value.ClaimedDrops.Order()
                        .Select(coord => new HiddenRoomReveal(pair.Key, coord.col, coord.row))
                        .ToArray(),
                    pair.Value.PurchasedShops.Order()
                        .Select(coord => new HiddenRoomReveal(pair.Key, coord.col, coord.row))
                        .ToArray(),
                    pair.Value.BombedWalls.Order()
                        .Select(coord => new HiddenRoomReveal(pair.Key, coord.col, coord.row))
                        .ToArray()))
                .ToArray()
            : [];
        if (revealed.Length == 0 && identities.Length == 0 && bombs.Length == 0)
            return null;
        return JsonSerializer.Serialize(
            new HiddenRoomRevealEnvelope(SchemaVersion, revealed, identities, bombs));
    }

    internal static void Restore(RunState runState, string? payload)
    {
        Revealed.Remove(runState);
        Bombs.Remove(runState);
        if (!MysteriousMapRules.IsActive(runState.Map))
            return;
        if (string.IsNullOrWhiteSpace(payload))
        {
            RecoverHiddenRoomIdentities(runState, runState.Map, runState.CurrentActIndex);
            return;
        }
        var envelope = JsonSerializer.Deserialize<HiddenRoomRevealEnvelope>(payload)
            ?? throw new InvalidDataException("Hidden-room reveal payload is empty.");
        if (envelope.Schema is not 1 and not 2 and not SchemaVersion)
            throw new InvalidDataException($"Unsupported hidden-room reveal schema {envelope.Schema}.");
        var rooms = Revealed.GetValue(runState, _ => []);
        foreach (var room in envelope.Rooms ?? [])
            rooms.Add((room.ActIndex, new MapCoord(room.Col, room.Row)));

        var bombActs = Bombs.GetValue(runState, _ => []);
        foreach (var snapshot in envelope.Bombs ?? [])
        {
            var state = new BombActRuntime { Count = Math.Max(0, snapshot.Count) };
            foreach (var claim in snapshot.ClaimedDrops ?? [])
                state.ClaimedDrops.Add(new MapCoord(claim.Col, claim.Row));
            foreach (var shop in snapshot.PurchasedShops ?? [])
                state.PurchasedShops.Add(new MapCoord(shop.Col, shop.Row));
            foreach (var wall in snapshot.BombedWalls ?? [])
                state.BombedWalls.Add(new MapCoord(wall.Col, wall.Row));
            bombActs[snapshot.ActIndex] = state;
        }

        var hiddenCoords = (envelope.HiddenRooms ?? [])
            .Where(room => room.ActIndex == runState.CurrentActIndex)
            .Select(room => new MapCoord(room.Col, room.Row))
            .Where(runState.Map.HasPoint)
            .Distinct()
            .ToArray();
        if (hiddenCoords.Length > 0)
            MysteriousMapRules.SetHiddenRooms(runState.Map, hiddenCoords);
        else
            RecoverHiddenRoomIdentities(runState, runState.Map, runState.CurrentActIndex);
    }

    internal static void EnsureHiddenRoomIdentities(RunState runState, ActMap map, int actIndex)
    {
        if (!MysteriousMapRules.IsActive(map))
            return;
        var existing = MysteriousMapRules.GetHiddenRoomCoords(map);
        if (existing.Count == 2)
        {
            MysteriousMapRules.SetHiddenRooms(map, existing);
            return;
        }
        RecoverHiddenRoomIdentities(runState, map, actIndex);
    }

    private static void RecoverHiddenRoomIdentities(RunState runState, ActMap map, int actIndex)
    {
        try
        {
            // The layout and both hidden placements depend only on the run seed
            // and act index. Regenerating a reference map safely migrates saves
            // made before exact hidden coordinates were serialized, even when
            // CanBeModified has already been polluted on most nodes.
            var reference = new MysteriousMapActMap(runState, actIndex);
            var recovered = MysteriousMapRules.GetHiddenRoomCoords(reference)
                .Where(map.HasPoint)
                .ToArray();
            if (recovered.Length != 2)
                throw new InvalidDataException($"Expected two recoverable hidden rooms, got {recovered.Length}.");
            MysteriousMapRules.SetHiddenRooms(map, recovered);
        }
        catch (Exception exception)
        {
            // Failing closed is preferable to treating every ordinary room as
            // hidden and making the exploration frontier disappear.
            MysteriousMapRules.SetHiddenRooms(map, []);
            MegaCrit.Sts2.Core.Logging.Log.Warn(
                $"[MysteriousMap] Could not recover hidden-room identities; disabled hidden markers for this act: " +
                exception.Message);
        }
    }

    internal static SerializableModifier ToSerializableModifier(string payload) => new()
    {
        Id = ModelDb.Modifier<MysteriousMapHiddenRoomStateModifier>().Id,
        Props = new SavedProperties
        {
            strings = [new SavedProperties.SavedProperty<string>(PayloadProperty, payload)]
        }
    };

    internal static string? ReadFrom(SerializableRun save, bool remove)
    {
        var id = ModelDb.Modifier<MysteriousMapHiddenRoomStateModifier>().Id;
        string? payload = null;
        for (var index = save.Modifiers.Count - 1; index >= 0; index--)
        {
            var modifier = save.Modifiers[index];
            if (modifier.Id != id)
                continue;
            payload ??= modifier.Props?.strings?
                .FirstOrDefault(property => property.name == PayloadProperty).value;
            if (remove)
                save.Modifiers.RemoveAt(index);
        }
        return payload;
    }

    private static BombActRuntime GetBombAct(RunState runState)
    {
        var acts = Bombs.GetValue(runState, _ => []);
        if (!acts.TryGetValue(runState.CurrentActIndex, out var state))
            acts[runState.CurrentActIndex] = state = new BombActRuntime();
        return state;
    }

    // Bombs belong to the shared floor, so every peer must compute the same
    // pickup amount regardless of which player is local on that machine.
    private static int BombPickupAmount(RunState runState) =>
        runState.Players.Any(player => player.Relics.Any(relic => relic is MysteriousBogoBombs))
            ? 2
            : 1;

    private static HashSet<MapCoord> GetBombDropExits(RunState runState)
    {
        var candidates = MysteriousMapRules.GetAllPointsIncludingSpecial(runState.Map)
            .Where(MysteriousMapRules.IsNestedExit)
            .Where(exit => NestedMapCoordinates.TryDecode(exit.coord, out var outer, out _) &&
                           runState.Map.GetPoint(NestedMapCoordinates.ToCoord(
                               outer, Generation.InnerNodeRole.Entry)) is { } entry &&
                           entry != runState.Map.StartingMapPoint &&
                           !MysteriousMapRules.IsHiddenRoom(entry) &&
                           !entry.Children.Contains(runState.Map.BossMapPoint) &&
                           runState.Map.GetPoint(NestedMapCoordinates.ToCoord(
                               outer, Generation.InnerNodeRole.Reward))?.PointType != MapPointType.Treasure)
            .OrderBy(exit => Mix(runState.Rng.Seed, exit.coord))
            .ThenBy(exit => exit.coord)
            .ToList();
        var count = Math.Min(candidates.Count, 2 + (int)(runState.Rng.Seed & 1UL));
        return candidates.Take(count).Select(point => point.coord).ToHashSet();
    }

    private static ulong Mix(ulong seed, MapCoord coord)
    {
        var value = seed ^ ((ulong)(uint)coord.col << 32) ^ (uint)coord.row;
        value ^= value >> 30;
        value *= 0xBF58476D1CE4E5B9UL;
        value ^= value >> 27;
        value *= 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }
}
