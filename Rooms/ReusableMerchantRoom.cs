using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Logging;

namespace MysteriousMapMod.Rooms;

/// <summary>
/// Reconstructs the exact saved inventory when a shop coordinate is revisited.
/// Vanilla MerchantRoom creates a newly rolled inventory on every EnterInternal
/// call, while retaining the old inventory objects lets later run mutations leak
/// into a revisit.
/// </summary>
internal sealed class ReusableMerchantRoom : MerchantRoom
{
    private static readonly FieldInfo RunStateField = AccessTools.Field(typeof(MerchantRoom), "_runState");
    private static readonly FieldInfo InventoriesField = AccessTools.Field(typeof(MerchantRoom), "<Inventories>k__BackingField");

    private ShopRoomSnapshot? _pendingSnapshot;
    private bool _hasEntered;

    public ReusableMerchantRoom(ShopRoomSnapshot? pendingSnapshot = null)
    {
        _pendingSnapshot = pendingSnapshot;
    }

    public bool HasInventory => Inventories.Count > 0;
    internal ShopRoomSnapshot? PendingSnapshot => _pendingSnapshot;

    internal void ReplacePendingSnapshot(ShopRoomSnapshot snapshot)
    {
        if (_hasEntered || HasInventory)
            throw new InvalidOperationException("Cannot replace the snapshot after a merchant visit has started.");
        _pendingSnapshot = snapshot;
    }

    public override async Task EnterInternal(IRunState? runState, bool isRestoringRoomStackBase)
    {
        if (!_hasEntered)
        {
            _hasEntered = true;
            if (_pendingSnapshot != null && runState is RunState concreteState)
            {
                var snapshot = _pendingSnapshot;
                _pendingSnapshot = null;
                if (ShopInventoryPersistence.TryRestoreInventories(
                        concreteState, snapshot, out var inventories, out var failure))
                {
                    InventoriesField.SetValue(this, inventories);
                    await EnterWithExistingInventory(runState, isRestoringRoomStackBase);
                    return;
                }

                Log.Warn($"[MysteriousMap] Could not restore the saved shop at " +
                         $"({snapshot.Col}, {snapshot.Row}); generating a replacement inventory: {failure}");
            }
            await base.EnterInternal(runState, isRestoringRoomStackBase);
            return;
        }

        await EnterWithExistingInventory(runState, isRestoringRoomStackBase);
    }

    public override async Task Exit(IRunState? runState)
    {
        await base.Exit(runState);
        if (runState is RunState concreteState)
            RoomFactory.PrepareReplacementAfterExit(concreteState, this);
    }

    private async Task EnterWithExistingInventory(IRunState? runState, bool isRestoringRoomStackBase)
    {
        if (isRestoringRoomStackBase)
            throw new InvalidOperationException("Reusable merchant cannot restore a room stack base.");

        // Mirror the non-inventory portion of vanilla MerchantRoom.EnterInternal.
        // Setting this on every visit also keeps Exit/GetLocalInventory valid after
        // a room instance has survived travel or been reconstructed from a save.
        RunStateField.SetValue(this, runState);
        Log.Info("[MysteriousMap] Entering reusable merchant room with its preserved inventory.");
        await PreloadManager.LoadRoomMerchantAssets();
        NRun.Instance?.SetCurrentRoom(NMerchantRoom.Create(this, runState?.Players ?? Array.Empty<MegaCrit.Sts2.Core.Entities.Players.Player>()));
        if (runState != null)
            await Hook.AfterRoomEntered(runState, this);
    }
}
