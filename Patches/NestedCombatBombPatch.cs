using System.Runtime.CompilerServices;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MysteriousMapMod.Map;
using MysteriousMapMod.Relics;
using MysteriousMapMod.Rooms;

namespace MysteriousMapMod.Patches;

[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
internal static class NestedCombatBombButtonPatch
{
    private const string ButtonName = "MysteriousMapCombatBomb";
    private const int EnergyCost = 2;

    [HarmonyPostfix]
    private static void Postfix(NCombatRoom __instance)
    {
        var state = RunManager.Instance?.DebugOnlyGetState();
        if (state == null || !MysteriousMapRules.IsNestedMap(state.Map) ||
            state.CurrentRoom is not CombatRoom room)
            return;

        var player = LocalContext.GetMe(state.Players);
        var playerCombatState = player?.PlayerCombatState;
        if (player == null || playerCombatState == null)
            return;

        var button = new Button
        {
            Name = ButtonName,
            // Keep the bomb next to the player's turn resources instead of
            // floating over the enemy side of the battlefield.
            Position = new Vector2(42f, 730f),
            Size = new Vector2(330f, 76f),
            ZIndex = 80
        };
        void Refresh()
        {
            var count = HiddenRoomState.GetBombCount(state);
            var energy = playerCombatState.Energy;
            button.Text = $"💣 使用炸弹  ⚡{EnergyCost}  ·  3 回合后 40 群体伤害（{count}）";
            button.Disabled = count <= 0 || energy < EnergyCost;
        }
        button.Pressed += async () =>
        {
            if (button.Disabled || playerCombatState.Energy < EnergyCost ||
                HiddenRoomState.GetBombCount(state) <= 0)
                return;

            // Disable immediately so a double click cannot pay or schedule twice.
            button.Disabled = true;
            await PlayerCmd.LoseEnergy(EnergyCost, player);
            if (!HiddenRoomState.TryConsumeBomb(state))
            {
                await PlayerCmd.GainEnergy(EnergyCost, player);
                Refresh();
                return;
            }
            NestedCombatBombs.Schedule(room.CombatState, player);
            if (state.CurrentMapCoord is { } source)
                ShopInventoryMultiplayerSynchronizer.PublishBombAction(
                    state, player, 3, source, roundNumber: room.CombatState.RoundNumber);
            Refresh();
        };

        void OnEnergyChanged(int _, int __) => Refresh();
        playerCombatState.EnergyChanged += OnEnergyChanged;
        button.TreeExited += () => playerCombatState.EnergyChanged -= OnEnergyChanged;

        Refresh();
        __instance.AddChild(button);
    }
}

[HarmonyPatch(typeof(Hook), nameof(Hook.AfterPlayerTurnStart))]
internal static class NestedCombatBombTriggerPatch
{
    [HarmonyPostfix]
    private static void Postfix(
        ICombatState combatState,
        PlayerChoiceContext choiceContext,
        Player player,
        ref Task __result)
    {
        if (combatState is CombatState concrete)
            __result = NestedCombatBombs.TriggerAfter(__result, concrete, choiceContext, player);
    }
}

internal static class NestedCombatBombs
{
    private sealed record Fuse(int Round, ulong OwnerId);
    private static readonly ConditionalWeakTable<CombatState, List<Fuse>> Pending = new();

    internal static void Schedule(CombatState combatState, Player owner, int? armedRound = null) =>
        Pending.GetValue(combatState, _ => []).Add(new Fuse(
            (armedRound ?? combatState.RoundNumber) + 3, owner.NetId));

    internal static async Task TriggerAfter(
        Task original,
        CombatState combatState,
        PlayerChoiceContext choiceContext,
        Player player)
    {
        await original;
        if (!Pending.TryGetValue(combatState, out var pending))
            return;
        var explosions = pending.RemoveAll(fuse =>
            fuse.Round <= combatState.RoundNumber && fuse.OwnerId == player.NetId);
        if (explosions == 0 || combatState.HittableEnemies.Count == 0)
            return;
        if (player.Relics.Any(relic => relic is MysteriousPyromaniac))
            await CreatureCmd.Heal(player.Creature, 5m * explosions, false);
        Task DealBombDamage() => CreatureCmd.Damage(
            choiceContext,
            combatState.HittableEnemies,
            40m * explosions,
            ValueProp.Unpowered,
            player.Creature);
        var ipecac = player.Relics.OfType<MysteriousIpecac>().FirstOrDefault();
        if (ipecac == null)
            await DealBombDamage();
        else
            await ipecac.ResolveExistingAreaDamage(choiceContext, DealBombDamage, explosions);
        var glitch = player.Relics.OfType<MysteriousGlitchedRelic>().FirstOrDefault();
        if (glitch != null)
            await glitch.AfterBombExploded(choiceContext, explosions);
    }
}
