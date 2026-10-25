using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Relics;
using MegaCrit.Sts2.Core.Nodes.Screens;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MysteriousMapMod.Map;
using MysteriousMapMod.Relics;
using MysteriousMapMod.Rooms;

namespace MysteriousMapMod.Patches;

[HarmonyPatch(typeof(NTreasureRoom), "_Ready")]
internal static class DataminerTreasureButtonPatch
{
    private const string ButtonName = "MysteriousMapDataminerButton";

    [HarmonyPostfix]
    private static void Postfix(NTreasureRoom __instance)
        => EnsureButton(__instance);

    internal static void EnsureButton(NTreasureRoom __instance)
    {
        var state = RunManager.Instance?.DebugOnlyGetState();
        if (state == null || !MysteriousDataminer.CanUseInRun(state) ||
            state.CurrentRoom is not TreasureRoom)
            return;
        var player = LocalContext.GetMe(state.Players);
        var dataminer = player?.Relics.OfType<MysteriousDataminer>().FirstOrDefault();
        if (dataminer == null)
            return;

        if (__instance.GetNodeOrNull<Button>(ButtonName) != null)
            return;
        var button = new Button
        {
            Name = ButtonName,
            Position = new Vector2(1410f, 760f),
            Size = new Vector2(340f, 72f),
            Text = dataminer.Armed ? "红镐子 · 已启动" :
                "⛏ 红镐子 · 挖掘本次遗物",
            Disabled = !dataminer.CanArm,
            ZIndex = 60
        };
        button.Pressed += () =>
        {
            if (!dataminer.TryArm())
                return;
            DataminerMultiplayerSynchronizer.PublishArmed(state, player!);
            button.Disabled = true;
            button.Text = "红镐子 · 已启动，领取遗物时变异";
        };
        __instance.AddChild(button);
    }
}

// The chest can be opened after the room node's _Ready, including when the
// starting Dataminer was granted during run startup. Recheck before rewards
// appear so the player can always arm it before taking the relic.
[HarmonyPatch(typeof(NTreasureRoom), "OpenChest")]
internal static class DataminerTreasureOpenButtonPatch
{
    [HarmonyPostfix]
    private static void Postfix(NTreasureRoom __instance) =>
        DataminerTreasureButtonPatch.EnsureButton(__instance);
}

// Arm the pickaxe before choosing a shop relic. Only a purchased relic is
// transformed; cards, potions and leaving the shop do not consume it.
[HarmonyPatch(typeof(NMerchantInventory), nameof(NMerchantInventory.Open))]
internal static class DataminerShopButtonPatch
{
    private const string ButtonName = "MysteriousMapDataminerShopButton";

    [HarmonyPostfix]
    private static void Postfix(NMerchantInventory __instance)
    {
        __instance.GetNodeOrNull<Button>(ButtonName)?.QueueFree();
        var state = RunManager.Instance?.DebugOnlyGetState();
        if (state == null || !MysteriousDataminer.CanUseInRun(state) ||
            state.CurrentRoom is not MerchantRoom)
            return;
        var player = LocalContext.GetMe(state.Players);
        var dataminer = player?.Relics.OfType<MysteriousDataminer>().FirstOrDefault();
        if (dataminer == null)
            return;

        var button = new Button
        {
            Name = ButtonName,
            Position = new Vector2(1080f, 760f),
            Size = new Vector2(350f, 72f),
            Text = dataminer.Armed ? "红镐子 · 已启动" : "⛏ 红镐子 · 下件购买的遗物",
            Disabled = !dataminer.CanArm,
            ZIndex = 60
        };
        button.Pressed += () =>
        {
            if (!dataminer.TryArm())
                return;
            DataminerMultiplayerSynchronizer.PublishArmed(state, player!);
            button.Disabled = true;
            button.Text = "红镐子 · 已启动，购买遗物时变异";
        };
        __instance.AddChild(button);
    }
}

// Elite relics are claimed from the combat rewards overlay, rather than from
// the treasure-room node. Arm before clicking the relic reward.
[HarmonyPatch(typeof(NRewardsScreen), nameof(NRewardsScreen._Ready))]
internal static class DataminerEliteRewardButtonPatch
{
    private const string ButtonName = "MysteriousMapDataminerEliteButton";

    [HarmonyPostfix]
    private static void Postfix(NRewardsScreen __instance)
    {
        var state = RunManager.Instance?.DebugOnlyGetState();
        if (state == null || !MysteriousDataminer.CanUseInRun(state) ||
            state.CurrentRoom?.RoomType != RoomType.Elite ||
            __instance.GetNodeOrNull<Button>(ButtonName) != null)
            return;

        var player = LocalContext.GetMe(state.Players);
        var dataminer = player?.Relics.OfType<MysteriousDataminer>().FirstOrDefault();
        if (dataminer == null)
            return;

        var button = new Button
        {
            Name = ButtonName,
            AnchorLeft = 1f,
            AnchorRight = 1f,
            OffsetLeft = -470f,
            OffsetRight = -35f,
            OffsetTop = 165f,
            OffsetBottom = 237f,
            Text = dataminer.Armed ? "红镐子 · 已启动" : "⛏ 红镐子 · 挖掘精英遗物",
            Disabled = !dataminer.CanArm,
            ZIndex = 80
        };
        button.Pressed += () =>
        {
            if (!dataminer.TryArm())
                return;
            DataminerMultiplayerSynchronizer.PublishArmed(state, player!);
            button.Disabled = true;
            button.Text = "红镐子 · 已启动，领取遗物时变异";
        };
        __instance.AddChild(button);
    }
}

/// <summary>
/// The treasure UI still awards its visible relic normally. Once the obtain
/// task completes, replace just that relic with a persistent glitched relic.
/// An unexpected replacement failure is logged rather than silently masked.
/// </summary>
[HarmonyPatch(typeof(RelicCmd), nameof(RelicCmd.Obtain),
    [typeof(RelicModel), typeof(Player), typeof(int)])]
internal static class DataminerTransformRewardPatch
{
    private static readonly Dictionary<Player, MysteriousGlitchedRelic> PendingAnimation = new();

    internal static bool CanTransformInRoom(AbstractRoom? room) =>
        room is TreasureRoom or MerchantRoom || room?.RoomType == RoomType.Elite;

    internal static bool TryTakeAnimationReplacement(Player player, out MysteriousGlitchedRelic replacement)
    {
        if (PendingAnimation.Remove(player, out var found))
        {
            replacement = found;
            return true;
        }
        replacement = null!;
        return false;
    }

    [HarmonyPostfix]
    private static void Postfix(Player player, ref Task<RelicModel> __result)
    {
        __result = TransformAfterObtain(__result, player);
    }

    private static async Task<RelicModel> TransformAfterObtain(
        Task<RelicModel> obtainTask, Player player)
    {
        var obtained = await obtainTask;
        if (obtained == null || player.RunState is not RunState state ||
            !CanTransformInRoom(state.CurrentRoom) ||
            !MysteriousDataminer.CanUseInRun(state))
        {
            await NotifyOwnedGlitch(player, obtained);
            return obtained!;
        }
        var dataminer = player.Relics.OfType<MysteriousDataminer>().FirstOrDefault();
        if (dataminer?.Armed != true || obtained is MysteriousDataminer or MysteriousGlitchedRelic)
        {
            var synchronized = await DataminerMultiplayerSynchronizer.ApplyPendingAfterObtain(player, obtained);
            if (!ReferenceEquals(synchronized, obtained) && synchronized is MysteriousGlitchedRelic glitch)
                PendingAnimation[player] = glitch;
            await NotifyOwnedGlitch(player, obtained);
            return synchronized;
        }

        try
        {
            var existing = player.Relics.OfType<MysteriousGlitchedRelic>().FirstOrDefault();
            if (existing != null)
            {
                await RelicCmd.Remove(obtained);
                if (existing.AddRandomEffects() == 0)
                    await CreatureCmd.GainMaxHp(player.Creature, 5m);
                dataminer.Consume();
                PendingAnimation[player] = existing;
                DataminerMultiplayerSynchronizer.PublishCombinations(state, player, obtained, existing);
                await existing.OnRelicObtained();
                Log.Info("[MysteriousMap] Dataminer added more effects to the glitched relic.");
                return existing;
            }
            var replacement = await RelicCmd.Replace(
                obtained, ModelDb.Relic<MysteriousGlitchedRelic>().ToMutable());
            dataminer.Consume();
            PendingAnimation[player] = (MysteriousGlitchedRelic)replacement;
            DataminerMultiplayerSynchronizer.PublishCombinations(
                state, player, obtained, (MysteriousGlitchedRelic)replacement);
            await ((MysteriousGlitchedRelic)replacement).OnRelicObtained();
            Log.Info("[MysteriousMap] Dataminer transformed a treasure, shop, or elite relic into a glitched relic.");
            return replacement;
        }
        catch (Exception exception)
        {
            Log.Warn($"[MysteriousMap] Dataminer could not transform relic: {exception.Message}");
            throw;
        }
    }

    private static async Task NotifyOwnedGlitch(Player player, RelicModel? obtained)
    {
        // Replace() can obtain the new glitched relic internally. The outer
        // original-relic pickup emits once after its links have been updated.
        if (obtained is null or MysteriousGlitchedRelic) return;
        if (player.Relics.OfType<MysteriousGlitchedRelic>().FirstOrDefault() is { } glitch)
            await glitch.OnRelicObtained();
    }
}

// Treasure awards animate the relic that was offered in the chest. Once the
// Dataminer replaces it, that node no longer exists in the inventory. Redirect
// just this one animation to the new, already-owned relic.
[HarmonyPatch(typeof(NRelicInventory), nameof(NRelicInventory.AnimateRelic))]
internal static class DataminerTreasureAnimationPatch
{
    private static readonly System.Reflection.FieldInfo PlayerField =
        AccessTools.Field(typeof(NRelicInventory), "_player");

    [HarmonyPrefix]
    private static void Prefix(NRelicInventory __instance, ref RelicModel relic)
    {
        if (PlayerField.GetValue(__instance) is not Player player ||
            player.Relics.Contains(relic) ||
            !DataminerTransformRewardPatch.TryTakeAnimationReplacement(player, out var replacement))
            return;
        relic = replacement;
    }
}
