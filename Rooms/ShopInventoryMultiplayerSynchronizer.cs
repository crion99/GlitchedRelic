using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Runs;
using MysteriousMapMod.Map;
using MysteriousMapMod.Patches;

namespace MysteriousMapMod.Rooms;

public sealed class MysteriousMapBombActionMessage : INetMessage
{
    public ulong PlayerId;
    public int Sequence;
    public int ActIndex;
    public int SourceCol;
    public int SourceRow;
    public int Kind; // 1: shop purchase, 2: wall, 3: combat use
    public int DeltaCol;
    public int DeltaRow;
    public int RoundNumber;
    public int Amount;

    public bool ShouldBroadcast => true;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.VeryDebug;
    public bool ShouldBuffer => false;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteULong(PlayerId);
        writer.WriteInt(Sequence);
        writer.WriteInt(ActIndex);
        writer.WriteInt(SourceCol);
        writer.WriteInt(SourceRow);
        writer.WriteInt(Kind);
        writer.WriteInt(DeltaCol);
        writer.WriteInt(DeltaRow);
        writer.WriteInt(RoundNumber);
        writer.WriteInt(Amount);
    }

    public void Deserialize(PacketReader reader)
    {
        PlayerId = reader.ReadULong();
        Sequence = reader.ReadInt();
        ActIndex = reader.ReadInt();
        SourceCol = reader.ReadInt();
        SourceRow = reader.ReadInt();
        Kind = reader.ReadInt();
        DeltaCol = reader.ReadInt();
        DeltaRow = reader.ReadInt();
        RoundNumber = reader.ReadInt();
        Amount = reader.ReadInt();
    }
}

public sealed class MysteriousMapMapPotionConsumedMessage : INetMessage
{
    public int ActIndex;
    public int Col;
    public int Row;
    public uint PotionSlot;
    public ModelId PotionId = null!;

    public bool ShouldBroadcast => true;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.VeryDebug;
    public bool ShouldBuffer => false;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(ActIndex);
        writer.WriteInt(Col);
        writer.WriteInt(Row);
        writer.WriteUInt(PotionSlot, 4);
        writer.WriteString(PotionId.Category);
        writer.WriteString(PotionId.Entry);
    }

    public void Deserialize(PacketReader reader)
    {
        ActIndex = reader.ReadInt();
        Col = reader.ReadInt();
        Row = reader.ReadInt();
        PotionSlot = reader.ReadUInt(4);
        PotionId = new ModelId(reader.ReadString(), reader.ReadString());
    }
}

public sealed class MysteriousMapShopInventorySyncMessage : INetMessage
{
    public int ActIndex;
    public int Col;
    public int Row;
    public int Revision;
    public string InventoryPayload = string.Empty;

    public bool ShouldBroadcast => true;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.VeryDebug;
    public bool ShouldBuffer => true;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteInt(ActIndex);
        writer.WriteInt(Col);
        writer.WriteInt(Row);
        writer.WriteInt(Revision);
        writer.WriteString(InventoryPayload);
    }

    public void Deserialize(PacketReader reader)
    {
        ActIndex = reader.ReadInt();
        Col = reader.ReadInt();
        Row = reader.ReadInt();
        Revision = reader.ReadInt();
        InventoryPayload = reader.ReadString();
    }
}

internal sealed class ShopInventoryMultiplayerSynchronizer : IDisposable
{
    private static ShopInventoryMultiplayerSynchronizer? _active;

    private readonly RunManager _runManager;
    private readonly INetGameService _netService;
    private int _nextBombSequence;
    private readonly HashSet<(ulong PlayerId, int Sequence)> _seenBombActions = [];

    private ShopInventoryMultiplayerSynchronizer(RunManager runManager)
    {
        _runManager = runManager;
        _netService = runManager.NetService;
        _netService.RegisterMessageHandler<MysteriousMapShopInventorySyncMessage>(OnInventoryReceived);
        _netService.RegisterMessageHandler<MysteriousMapMapPotionConsumedMessage>(OnMapPotionConsumed);
        _netService.RegisterMessageHandler<MysteriousMapBombActionMessage>(OnBombAction);
    }

    internal static void Initialize(RunManager runManager)
    {
        _active?.Dispose();
        _active = runManager.NetService.Type.IsMultiplayer()
            ? new ShopInventoryMultiplayerSynchronizer(runManager)
            : null;
    }

    internal static void DisposeActive()
    {
        _active?.Dispose();
        _active = null;
    }

    internal static void PublishMapPotionConsumption(
        RunState runState,
        Player player,
        uint potionSlot,
        ModelId potionId,
        MapCoord target)
    {
        var synchronizer = _active;
        if (synchronizer == null || !ReferenceEquals(synchronizer._runManager.DebugOnlyGetState(), runState) ||
            !synchronizer._netService.IsConnected)
        {
            return;
        }

        try
        {
            synchronizer._netService.SendMessage(new MysteriousMapMapPotionConsumedMessage
            {
                ActIndex = runState.CurrentActIndex,
                Col = target.col,
                Row = target.row,
                PotionSlot = potionSlot,
                PotionId = potionId
            });
            Log.Info($"[MysteriousMap] Synchronized player {player.NetId}'s map-targeted potion " +
                     $"{potionId.Entry} in slot {potionSlot} at {target}.");
        }
        catch (Exception exception)
        {
            Log.Warn($"[MysteriousMap] Could not synchronize map-targeted potion consumption: " +
                     exception.Message);
        }
    }

    internal static void PublishBombAction(RunState state, Player player, int kind,
        MapCoord source, int deltaCol = 0, int deltaRow = 0, int roundNumber = 0,
        int amount = 0)
    {
        var active = _active;
        if (active == null || !active._netService.IsConnected ||
            !ReferenceEquals(active._runManager.DebugOnlyGetState(), state) ||
            active._netService.NetId != player.NetId)
            return;
        active._netService.SendMessage(new MysteriousMapBombActionMessage
        {
            PlayerId = player.NetId,
            Sequence = ++active._nextBombSequence,
            ActIndex = state.CurrentActIndex,
            SourceCol = source.col,
            SourceRow = source.row,
            Kind = kind,
            DeltaCol = deltaCol,
            DeltaRow = deltaRow,
            RoundNumber = roundNumber,
            Amount = amount
        });
    }

    internal static bool ShouldApplyOwnerSide(Player player) =>
        _active == null || !_active._netService.IsConnected ||
        _active._netService.NetId == player.NetId;

    internal static void PublishLocalInventory(
        RunState runState,
        int actIndex,
        MapCoord coord,
        ReusableMerchantRoom room)
    {
        var synchronizer = _active;
        if (synchronizer == null || !ReferenceEquals(synchronizer._runManager.DebugOnlyGetState(), runState))
            return;

        try
        {
            var inventory = ShopInventoryPersistence.CaptureInventoryForPlayer(
                room, synchronizer._netService.NetId);
            var revision = runState.VisitedMapCoords.Count;
            RoomFactory.StoreSyncedShopInventory(runState, actIndex, coord, revision, inventory);

            if (!synchronizer._netService.IsConnected)
                return;

            synchronizer._netService.SendMessage(new MysteriousMapShopInventorySyncMessage
            {
                ActIndex = actIndex,
                Col = coord.col,
                Row = coord.row,
                Revision = revision,
                InventoryPayload = ShopInventoryPersistence.EncodeInventoryForSync(inventory)
            });
            Log.Info($"[MysteriousMap] Synchronized player {inventory.PlayerId}'s shop inventory " +
                     $"for act {actIndex}, coord {coord}, revision {revision}.");
        }
        catch (Exception exception)
        {
            Log.Warn($"[MysteriousMap] Could not synchronize the local reusable shop inventory: {exception.Message}");
        }
    }

    public void Dispose()
    {
        _netService.UnregisterMessageHandler<MysteriousMapShopInventorySyncMessage>(OnInventoryReceived);
        _netService.UnregisterMessageHandler<MysteriousMapMapPotionConsumedMessage>(OnMapPotionConsumed);
        _netService.UnregisterMessageHandler<MysteriousMapBombActionMessage>(OnBombAction);
    }

    private void OnBombAction(MysteriousMapBombActionMessage message, ulong senderId)
    {
        var state = _runManager.DebugOnlyGetState();
        var source = new MapCoord(message.SourceCol, message.SourceRow);
        if (state == null || senderId == _netService.NetId ||
            senderId != message.PlayerId || message.Sequence <= 0 ||
            message.ActIndex != state.CurrentActIndex ||
            !MysteriousMapRules.IsNestedMap(state.Map) ||
            state.GetPlayer(senderId) is not { } player ||
            state.CurrentMapCoord != source ||
            !_seenBombActions.Add((senderId, message.Sequence)))
            return;

        switch (message.Kind)
        {
            case 1 when state.CurrentRoom is MegaCrit.Sts2.Core.Rooms.MerchantRoom:
                HiddenRoomState.AddPurchasedBomb(state, source);
                break;
            case 2 when !CombatManager.Instance.IsInProgress &&
                Math.Abs(message.DeltaCol) + Math.Abs(message.DeltaRow) == 1:
                if (HiddenRoomState.TryBombWall(state, message.DeltaCol, message.DeltaRow,
                        out _) && NMapScreen.Instance is { } screen)
                    NestedBombUi.RefreshAfterRemoteBomb(screen, state);
                break;
            case 3 when state.CurrentRoom is MegaCrit.Sts2.Core.Rooms.CombatRoom room &&
                message.RoundNumber <= room.CombatState.RoundNumber &&
                message.RoundNumber >= room.CombatState.RoundNumber - 3:
                if (HiddenRoomState.TryConsumeBomb(state))
                    NestedCombatBombs.Schedule(room.CombatState, player, message.RoundNumber);
                break;
            case 4 when message.Amount is >= 1 and <= 3:
                for (var i = 0; i < message.Amount; i++)
                    HiddenRoomState.AddGlitchBomb(state);
                break;
            case 5:
                HiddenRoomState.RevealAdjacent(state, source);
                if (NMapScreen.Instance is { } revealScreen)
                    MapVisibilityController.Apply(revealScreen);
                break;
        }
    }

    private void OnMapPotionConsumed(MysteriousMapMapPotionConsumedMessage message, ulong senderId)
    {
        var runState = _runManager.DebugOnlyGetState();
        var target = new MapCoord(message.Col, message.Row);
        var player = runState?.GetPlayer(senderId);
        if (runState == null || player == null || !MysteriousMapRules.IsActive(runState.Map) ||
            CombatManager.Instance.IsInProgress ||
            message.ActIndex != runState.CurrentActIndex ||
            !runState.VisitedMapCoords.Contains(target) ||
            message.PotionSlot >= player.PotionSlots.Count)
        {
            Log.Warn($"[MysteriousMap] Rejected invalid map-potion consumption from peer {senderId}.");
            return;
        }

        var potion = player.GetPotionAtSlotIndex((int)message.PotionSlot);
        if (potion != null && potion.Id != message.PotionId)
        {
            Log.Warn($"[MysteriousMap] Rejected map-potion consumption from peer {senderId}: " +
                     $"slot {message.PotionSlot} contains {potion.Id}, not {message.PotionId}.");
            return;
        }

        // Potion removal may reach a peer before this custom message. A missing
        // potion therefore means consumption is already reflected locally, not
        // that the shared hidden-room reveal should be discarded.
        if (potion != null)
        {
            potion.RemoveBeforeUse();
            runState.CurrentMapPointHistoryEntry?.GetEntry(player.NetId).PotionUsed.Add(potion.Id);
        }
        var revealedRooms = HiddenRoomState.RevealAdjacent(runState, target);
        if (revealedRooms.Count > 0)
        {
            Log.Info($"[MysteriousMap] Applied peer {senderId}'s shared hidden-room reveal: " +
                     string.Join(", ", revealedRooms));
        }
        if (NMapScreen.Instance is { } screen)
            MapVisibilityController.RevealWithFade(screen, revealedRooms);
    }

    private void OnInventoryReceived(MysteriousMapShopInventorySyncMessage message, ulong senderId)
    {
        var runState = _runManager.DebugOnlyGetState();
        if (runState == null || !MysteriousMapRules.IsActive(runState.Map) ||
            message.ActIndex != runState.CurrentActIndex || message.Revision < 0)
        {
            return;
        }

        var coord = new MapCoord(message.Col, message.Row);
        var point = runState.Map.GetPoint(coord);
        if (point == null || !MysteriousMapRules.IsRepeatableShopPoint(runState, point))
            return;

        try
        {
            var inventory = ShopInventoryPersistence.DecodeInventoryFromSync(message.InventoryPayload);
            if (inventory.PlayerId != senderId)
            {
                Log.Warn($"[MysteriousMap] Rejected a shop inventory for player {inventory.PlayerId} " +
                         $"sent by peer {senderId}.");
                return;
            }

            RoomFactory.StoreSyncedShopInventory(
                runState, message.ActIndex, coord, message.Revision, inventory);
        }
        catch (Exception exception)
        {
            Log.Warn($"[MysteriousMap] Ignoring an invalid synchronized shop inventory from " +
                     $"peer {senderId}: {exception.Message}");
        }
    }
}
