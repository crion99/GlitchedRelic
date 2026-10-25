using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Runs;
using MysteriousMapMod.Map;
using MysteriousMapMod.Relics;
using MysteriousMapMod.Rooms;

namespace MysteriousMapMod.Patches;

[HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Open))]
internal static class NestedBombShopPatch
{
    private const string ButtonName = "MysteriousMapBombPurchase";
    private const int BombPrice = 30;

    [HarmonyPostfix]
    private static void Postfix(NMerchantInventory __instance)
    {
        __instance.GetNodeOrNull<Button>(ButtonName)?.QueueFree();
        var manager = RunManager.Instance;
        var state = manager?.DebugOnlyGetState();
        if (state == null || !MysteriousMapRules.IsNestedMap(state.Map) ||
            !state.CurrentMapCoord.HasValue)
            return;

        var shopCoord = state.CurrentMapCoord.Value;
        var player = LocalContext.GetMe(state.Players);
        if (player == null)
            return;
        var quantity = player.Relics.Any(relic => relic is MysteriousBogoBombs) ? 2 : 1;
        var button = new Button
        {
            Name = ButtonName,
            Position = new Vector2(1460f, 760f),
            Size = new Vector2(300f, 72f),
            Text = $"💣 炸弹 ×{quantity} · {BombPrice} 金币",
            Disabled = player.Gold < BombPrice || !HiddenRoomState.CanBuyBomb(state, shopCoord),
            ZIndex = 60
        };
        button.Pressed += async () =>
        {
            if (button.Disabled || player.Gold < BombPrice ||
                !HiddenRoomState.CanBuyBomb(state, shopCoord))
                return;
            button.Disabled = true;
            await PlayerCmd.LoseGold(BombPrice, player, GoldLossType.Spent);
            manager!.RewardSynchronizer.SyncLocalGoldLost(BombPrice);
            HiddenRoomState.AddPurchasedBomb(state, shopCoord);
            ShopInventoryMultiplayerSynchronizer.PublishBombAction(state, player, 1, shopCoord);
            button.Text = $"已购买 · 炸弹 {HiddenRoomState.GetBombCount(state)}";
        };
        __instance.AddChild(button);
    }
}
