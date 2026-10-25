using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;
using MysteriousMapMod.Map;

namespace MysteriousMapMod.Rooms;

internal static class RoomFactory
{
    private sealed record VersionedShopInventory(int Revision, ShopInventorySnapshot Snapshot);

    private static readonly ConditionalWeakTable<RunState, Dictionary<(int Act, MapCoord Coord), ReusableMerchantRoom>> Shops = new();
    private static readonly ConditionalWeakTable<RunState, Dictionary<(int Act, MapCoord Coord), ShopRoomSnapshot>> PendingShopSnapshots = new();
    private static readonly ConditionalWeakTable<RunState,
        Dictionary<(int Act, MapCoord Coord, ulong PlayerId), VersionedShopInventory>> SyncedShopInventories = new();

    public static MerchantRoom GetShop(RunState runState, MapCoord coord)
    {
        var rooms = Shops.GetValue(runState, _ => new Dictionary<(int, MapCoord), ReusableMerchantRoom>());
        var key = (runState.CurrentActIndex, coord);
        if (!rooms.TryGetValue(key, out var room))
        {
            ShopRoomSnapshot? snapshot = null;
            if (PendingShopSnapshots.TryGetValue(runState, out var pending) &&
                pending.Remove(key, out var restored))
            {
                snapshot = restored;
            }
            room = new ReusableMerchantRoom(snapshot);
            rooms[key] = room;
        }
        else if (room.PendingSnapshot is { } staged)
        {
            room.ReplacePendingSnapshot(MergeSyncedInventories(runState, key, staged));
        }
        return room;
    }

    public static void PrepareReplacementAfterExit(RunState runState, ReusableMerchantRoom exitedRoom)
    {
        if (!Shops.TryGetValue(runState, out var rooms) || !exitedRoom.HasInventory)
            return;

        foreach (var pair in rooms.ToArray())
        {
            if (!ReferenceEquals(pair.Value, exitedRoom))
                continue;

            // Freeze concrete merchandise immediately on exit. Reusing the old
            // MerchantInventory object leaves its relic entries coupled to the
            // player's mutable relic grab-bag lifecycle; reconstructing from IDs
            // makes later relic rewards unable to shift this shop's shelves.
            ShopInventoryMultiplayerSynchronizer.PublishLocalInventory(
                runState, pair.Key.Act, pair.Key.Coord, exitedRoom);
            var snapshot = ShopInventoryPersistence.Capture(
                pair.Key.Act, pair.Key.Coord, exitedRoom);
            snapshot = MergeSyncedInventories(runState, pair.Key, snapshot);
            ShopInventoryPersistence.ReleaseTransientCards(runState, exitedRoom);
            rooms[pair.Key] = new ReusableMerchantRoom(snapshot);
            return;
        }
    }

    public static void StoreSyncedShopInventory(
        RunState runState,
        int actIndex,
        MapCoord coord,
        int revision,
        ShopInventorySnapshot snapshot)
    {
        var inventories = SyncedShopInventories.GetValue(runState,
            _ => new Dictionary<(int, MapCoord, ulong), VersionedShopInventory>());
        var key = (actIndex, coord, snapshot.PlayerId);
        if (inventories.TryGetValue(key, out var existing) && existing.Revision > revision)
            return;
        inventories[key] = new VersionedShopInventory(revision, snapshot);
    }

    public static string? CaptureShopSnapshots(RunState runState)
    {
        var snapshots = new Dictionary<(int Act, MapCoord Coord), ShopRoomSnapshot>();
        if (PendingShopSnapshots.TryGetValue(runState, out var pending))
        {
            foreach (var pair in pending)
                snapshots[pair.Key] = pair.Value;
        }

        if (Shops.TryGetValue(runState, out var rooms))
        {
            foreach (var pair in rooms)
            {
                ShopRoomSnapshot? snapshot = null;
                if (pair.Value.HasInventory)
                {
                    snapshot = ShopInventoryPersistence.Capture(
                        pair.Key.Act, pair.Key.Coord, pair.Value);
                }
                else if (pair.Value.PendingSnapshot is { } pendingSnapshot)
                {
                    snapshot = pendingSnapshot;
                }

                if (snapshot != null)
                    snapshots[pair.Key] = MergeSyncedInventories(runState, pair.Key, snapshot);
            }
        }

        return snapshots.Count == 0 ? null : ShopInventoryPersistence.Encode(snapshots.Values);
    }

    public static void RestoreShopSnapshots(RunState runState, string? payload)
    {
        PendingShopSnapshots.Remove(runState);
        if (string.IsNullOrWhiteSpace(payload))
            return;

        var snapshots = ShopInventoryPersistence.Decode(payload);
        var pending = PendingShopSnapshots.GetValue(runState,
            _ => new Dictionary<(int Act, MapCoord Coord), ShopRoomSnapshot>());
        foreach (var snapshot in snapshots)
            pending[(snapshot.ActIndex, new MapCoord(snapshot.Col, snapshot.Row))] = snapshot;
    }

    private static ShopRoomSnapshot MergeSyncedInventories(
        RunState runState,
        (int Act, MapCoord Coord) key,
        ShopRoomSnapshot snapshot)
    {
        if (!SyncedShopInventories.TryGetValue(runState, out var synced))
            return snapshot;

        return ShopInventoryPersistence.MergeInventories(snapshot,
            synced.Where(item => item.Key.Act == key.Act && item.Key.Coord == key.Coord)
                .Select(item => item.Value.Snapshot));
    }

    public static CombatRoom CreateMonsterRoom(RunState runState, MapCoord coord)
    {
        var distances = MysteriousMapRules.ComputeDistances(runState.Map);
        var distance = distances.TryGetValue(coord, out var measuredDistance)
            ? measuredDistance : int.MaxValue;
        var completedNormalEncounters = runState.CurrentActIndex < runState.MapPointHistory.Count
            ? runState.MapPointHistory[runState.CurrentActIndex]
                .SelectMany(entry => entry.Rooms)
                .Count(room => room.RoomType == RoomType.Monster)
            : 0;
        var useWeakPool = ShouldUseWeakEncounter(runState.Players.Count == 1,
            completedNormalEncounters, distance);
        var pool = (useWeakPool ? runState.Act.AllWeakEncounters : runState.Act.AllRegularEncounters)
            .OrderBy(encounter => encounter.Id.ToString(), StringComparer.Ordinal)
            .ToList();
        if (pool.Count == 0)
            pool = runState.Act.AllRegularEncounters.OrderBy(e => e.Id.ToString(), StringComparer.Ordinal).ToList();
        if (pool.Count == 0)
            pool = runState.Act.AllWeakEncounters.OrderBy(e => e.Id.ToString(), StringComparer.Ordinal).ToList();
        if (pool.Count == 0)
            throw new InvalidOperationException($"Act {runState.Act.Id} has no normal encounters.");

        var previous = GetPreviousNormalEncounter(runState);
        var hash = StableCoordinateHash(runState.Rng.Seed, runState.CurrentActIndex, coord);
        var index = EncounterRotationSelector.SelectIndex(
            pool.Count,
            hash,
            candidateIndex => previous == null ||
                              (pool[candidateIndex].Id != previous.Id &&
                               !pool[candidateIndex].SharesTagsWith(previous)),
            candidateIndex => previous == null || pool[candidateIndex].Id != previous.Id);
        var encounter = pool[index];
        return new CombatRoom(encounter.ToMutable(), runState);
    }

    internal static bool ShouldUseWeakEncounter(bool singlePlayer,
        int completedNormalEncounters, int distance) =>
        singlePlayer
            ? completedNormalEncounters < 3
            : distance <= MysteriousMapActMap.WeakEncounterDistance;

    private static EncounterModel? GetPreviousNormalEncounter(RunState runState)
    {
        if (runState.CurrentActIndex >= runState.MapPointHistory.Count)
            return null;

        return runState.MapPointHistory[runState.CurrentActIndex]
            .SelectMany(entry => entry.Rooms)
            .Reverse()
            .Where(room => room.RoomType == RoomType.Monster)
            .Select(GetEncounterOrNull)
            .FirstOrDefault(encounter => encounter != null);
    }

    private static EncounterModel? GetEncounterOrNull(MapPointRoomHistoryEntry room) =>
        room.ModelId is { } id ? ModelDb.GetByIdOrNull<EncounterModel>(id) : null;

    private static uint StableCoordinateHash(ulong seed, int act, MapCoord coord)
    {
        var value = seed ^ ((ulong)(act + 1) << 48) ^ ((ulong)(uint)coord.col << 24) ^ (uint)coord.row;
        value ^= value >> 33;
        value *= 0xff51afd7ed558ccdUL;
        value ^= value >> 33;
        value *= 0xc4ceb9fe1a85ec53UL;
        value ^= value >> 33;
        return (uint)value;
    }
}
