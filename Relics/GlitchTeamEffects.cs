using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace MysteriousMapMod.Relics;

internal static class GlitchTeamEffects
{
    internal static bool Allows(int playerCount, GlitchTrigger trigger) =>
        playerCount > 1 || trigger is not (GlitchTrigger.TeammateHurt or
            GlitchTrigger.TeammateDamage or GlitchTrigger.TeammateBlockGained or
            GlitchTrigger.TeammateBuffGained);

    internal static bool Allows(int playerCount, GlitchOutcome outcome) =>
        playerCount > 1 || outcome is not (GlitchOutcome.TeammateDamage or
            GlitchOutcome.TeammateInjury or GlitchOutcome.TeammateCard or
            GlitchOutcome.TeammateStrength or GlitchOutcome.TeammateBlock or
            GlitchOutcome.TeammateHeal or GlitchOutcome.TeammateEndTurn or
            GlitchOutcome.TeammateRandomBuff);

    internal static bool IsLivingTeammate(Player owner, Creature creature) =>
        owner.RunState.Players.Count > 1 && owner.Creature.CombatState != null &&
        owner.RunState.Players.Any(player => player != owner &&
            player.Creature == creature && !player.Creature.IsDead &&
            player.Creature.CombatState == owner.Creature.CombatState);

    private static Player? Pick(Player owner, RunState state, bool activeTurnOnly = false)
    {
        var teammates = state.Players.Where(player => player != owner &&
                !player.Creature.IsDead &&
                player.Creature.CombatState == owner.Creature.CombatState &&
                (!activeTurnOnly || (CombatManager.Instance.IsPartOfPlayerTurn(player) &&
                                     !CombatManager.Instance.IsPlayerReadyToEndTurn(player))))
            .OrderBy(player => player.NetId).ToArray();
        return teammates.Length == 0 ? null :
            teammates[state.Rng.Niche.NextInt(teammates.Length)];
    }

    internal static async Task Apply(GlitchOutcome outcome, Player owner,
        RunState state, PlayerChoiceContext? choiceContext)
    {
        if (owner.Creature.CombatState == null || state.Players.Count < 2)
            return;
        var teammate = Pick(owner, state, outcome == GlitchOutcome.TeammateEndTurn);
        if (teammate == null) return;

        switch (outcome)
        {
            case GlitchOutcome.TeammateDamage when choiceContext != null:
                await CreatureCmd.Damage(choiceContext, teammate.Creature,
                    5m, ValueProp.Unpowered, owner.Creature);
                break;
            case GlitchOutcome.TeammateInjury:
                await GlitchCardGeneration.AddToDrawPile(ModelDb.Card<Injury>(), teammate);
                break;
            case GlitchOutcome.TeammateCard:
            {
                var cards = ModelDb.AllCards.Where(card =>
                    card.Pool == teammate.Character.CardPool &&
                    card.Rarity is CardRarity.Common or CardRarity.Uncommon &&
                    card.Type is CardType.Attack or CardType.Skill or CardType.Power).ToArray();
                if (cards.Length > 0)
                    await GlitchCardGeneration.AddToHand(
                        cards[state.Rng.Niche.NextInt(cards.Length)], teammate);
                break;
            }
            case GlitchOutcome.TeammateStrength when choiceContext != null:
                await PowerCmd.Apply<StrengthPower>(choiceContext, teammate.Creature,
                    1m, owner.Creature, null, false);
                break;
            case GlitchOutcome.TeammateBlock:
                await CreatureCmd.GainBlock(teammate.Creature, 8m,
                    ValueProp.Unpowered, null, false);
                break;
            case GlitchOutcome.TeammateHeal:
                await CreatureCmd.Heal(teammate.Creature, 5m, false);
                break;
            case GlitchOutcome.TeammateEndTurn:
                // The game's own command raises the multiplayer ready signal.
                // Never request it during an enemy turn or for an already-ready player.
                PlayerCmd.EndTurn(teammate, canBackOut: false);
                break;
            case GlitchOutcome.TeammateRandomBuff when choiceContext != null:
                await GlitchRandomBuffs.Apply(choiceContext, teammate, state);
                break;
        }
    }
}
