using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;
using MegaCrit.Sts2.Core.Multiplayer.Transport;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MysteriousMapMod.Map;
using MysteriousMapMod.Relics;

namespace MysteriousMapMod.Rooms;

public sealed class MysteriousMapDataminerArmedMessage : INetMessage
{
    public ulong PlayerId;
    public int ActIndex;
    public int RoomCol;
    public int RoomRow;

    public bool ShouldBroadcast => true;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.VeryDebug;
    public bool ShouldBuffer => false;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteULong(PlayerId);
        writer.WriteInt(ActIndex);
        writer.WriteInt(RoomCol);
        writer.WriteInt(RoomRow);
    }

    public void Deserialize(PacketReader reader)
    {
        PlayerId = reader.ReadULong();
        ActIndex = reader.ReadInt();
        RoomCol = reader.ReadInt();
        RoomRow = reader.ReadInt();
    }
}

public sealed class MysteriousMapDataminerCombinationMessage : INetMessage
{
    public ulong PlayerId;
    public int ActIndex;
    public int RoomCol;
    public int RoomRow;
    public ModelId SourceRelicId = null!;
    public string ComboCodes = string.Empty;
    public string ExternalComboCodes = string.Empty;

    public bool ShouldBroadcast => true;
    public NetTransferMode Mode => NetTransferMode.Reliable;
    public LogLevel LogLevel => LogLevel.VeryDebug;
    public bool ShouldBuffer => false;

    public void Serialize(PacketWriter writer)
    {
        writer.WriteULong(PlayerId);
        writer.WriteInt(ActIndex);
        writer.WriteInt(RoomCol);
        writer.WriteInt(RoomRow);
        writer.WriteString(SourceRelicId.Category);
        writer.WriteString(SourceRelicId.Entry);
        writer.WriteString(ComboCodes);
        writer.WriteString(ExternalComboCodes);
    }

    public void Deserialize(PacketReader reader)
    {
        PlayerId = reader.ReadULong();
        ActIndex = reader.ReadInt();
        RoomCol = reader.ReadInt();
        RoomRow = reader.ReadInt();
        SourceRelicId = new ModelId(reader.ReadString(), reader.ReadString());
        ComboCodes = reader.ReadString();
        ExternalComboCodes = reader.ReadString();
    }
}

/// <summary>
/// Mirrors the owner's active-relic choice and the rolled links. RelicCmd
/// handles the native relic inventory change; these messages carry only the
/// mod-specific decision and SavedProperty values.
/// </summary>
internal sealed class DataminerMultiplayerSynchronizer : IDisposable
{
    private static DataminerMultiplayerSynchronizer? _active;
    private readonly RunManager _manager;
    private readonly INetGameService _net;
    private readonly Dictionary<ulong, MysteriousMapDataminerCombinationMessage> _pending = new();

    private DataminerMultiplayerSynchronizer(RunManager manager)
    {
        _manager = manager;
        _net = manager.NetService;
        _net.RegisterMessageHandler<MysteriousMapDataminerArmedMessage>(OnArmed);
        _net.RegisterMessageHandler<MysteriousMapDataminerCombinationMessage>(OnCombinations);
    }

    internal static void Initialize(RunManager manager)
    {
        DisposeActive();
        if (manager.NetService.Type.IsMultiplayer())
            _active = new DataminerMultiplayerSynchronizer(manager);
    }

    internal static void DisposeActive()
    {
        _active?.Dispose();
        _active = null;
    }

    internal static void PublishArmed(RunState state, Player player)
    {
        var active = _active;
        if (active == null || !active._net.IsConnected ||
            !ReferenceEquals(active._manager.DebugOnlyGetState(), state) ||
            active._net.NetId != player.NetId)
            return;
        active._net.SendMessage(new MysteriousMapDataminerArmedMessage
        {
            PlayerId = player.NetId,
            ActIndex = state.CurrentActIndex,
            RoomCol = state.CurrentMapCoord?.col ?? -1,
            RoomRow = state.CurrentMapCoord?.row ?? -1
        });
    }

    internal static void PublishCombinations(RunState state, Player player,
        RelicModel source, MysteriousGlitchedRelic glitch)
    {
        var active = _active;
        if (active == null || !active._net.IsConnected ||
            !ReferenceEquals(active._manager.DebugOnlyGetState(), state) ||
            active._net.NetId != player.NetId)
            return;
        active._net.SendMessage(new MysteriousMapDataminerCombinationMessage
        {
            PlayerId = player.NetId,
            ActIndex = state.CurrentActIndex,
            RoomCol = state.CurrentMapCoord?.col ?? -1,
            RoomRow = state.CurrentMapCoord?.row ?? -1,
            SourceRelicId = source.Id,
            ComboCodes = glitch.ComboCodes,
            ExternalComboCodes = glitch.ExternalComboCodes
        });
    }

    internal static async Task<RelicModel> ApplyPendingAfterObtain(Player player, RelicModel obtained)
    {
        var active = _active;
        if (active == null || !active._pending.TryGetValue(player.NetId, out var message) ||
            message.SourceRelicId != obtained.Id ||
            player.RunState is not RunState state ||
            message.ActIndex != state.CurrentActIndex ||
            message.RoomCol != state.CurrentMapCoord?.col ||
            message.RoomRow != state.CurrentMapCoord?.row)
            return obtained;
        active._pending.Remove(player.NetId);
        return await ApplyCombinations(player, message, obtained);
    }

    public void Dispose()
    {
        _net.UnregisterMessageHandler<MysteriousMapDataminerArmedMessage>(OnArmed);
        _net.UnregisterMessageHandler<MysteriousMapDataminerCombinationMessage>(OnCombinations);
        _pending.Clear();
    }

    private void OnArmed(MysteriousMapDataminerArmedMessage message, ulong senderId)
    {
        var state = _manager.DebugOnlyGetState();
        if (state == null || senderId == _net.NetId || senderId != message.PlayerId ||
            message.ActIndex != state.CurrentActIndex ||
            message.RoomCol != state.CurrentMapCoord?.col ||
            message.RoomRow != state.CurrentMapCoord?.row ||
            state.CurrentRoom is not (TreasureRoom or MerchantRoom) ||
            !MysteriousMapRules.IsNestedMap(state.Map))
            return;
        var player = state.GetPlayer(senderId);
        player?.Relics.OfType<MysteriousDataminer>().FirstOrDefault()?.TryArm();
    }

    private async void OnCombinations(MysteriousMapDataminerCombinationMessage message, ulong senderId)
    {
        try
        {
            var state = _manager.DebugOnlyGetState();
            if (state == null || senderId == _net.NetId || senderId != message.PlayerId ||
                message.ActIndex != state.CurrentActIndex ||
                !MysteriousMapRules.IsNestedMap(state.Map) ||
                message.ComboCodes.Length > 4096 || message.ExternalComboCodes.Length > 4096)
                return;
            var player = state.GetPlayer(senderId);
            if (player == null) return;
            var source = player.Relics.FirstOrDefault(relic => relic.Id == message.SourceRelicId);
            if (source == null)
            {
                _pending[senderId] = message;
                if (player.Relics.OfType<MysteriousGlitchedRelic>().FirstOrDefault() is { } existing)
                {
                    existing.ComboCodes = message.ComboCodes;
                    existing.ExternalComboCodes = message.ExternalComboCodes;
                    player.Relics.OfType<MysteriousDataminer>().FirstOrDefault()?.Consume();
                }
                return;
            }
            await ApplyCombinations(player, message, source);
        }
        catch (Exception exception)
        {
            Log.Error($"[MysteriousMap] Could not synchronize Dataminer combinations: {exception}");
        }
    }

    private static async Task<RelicModel> ApplyCombinations(Player player,
        MysteriousMapDataminerCombinationMessage message, RelicModel? source)
    {
        var glitch = player.Relics.OfType<MysteriousGlitchedRelic>().FirstOrDefault();
        if (glitch == null && source != null && source is not MysteriousDataminer)
            glitch = (MysteriousGlitchedRelic)await RelicCmd.Replace(
                source, ModelDb.Relic<MysteriousGlitchedRelic>().ToMutable());
        else if (glitch != null && source != null && source != glitch &&
                 source is not MysteriousDataminer)
            await RelicCmd.Remove(source);
        if (glitch == null) return source!;
        glitch.ComboCodes = message.ComboCodes;
        glitch.ExternalComboCodes = message.ExternalComboCodes;
        player.Relics.OfType<MysteriousDataminer>().FirstOrDefault()?.Consume();
        return glitch;
    }
}
