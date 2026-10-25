using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;

namespace MysteriousMapMod.Relics;

internal static class GlitchCardGeneration
{
    // The game's generated-card command rejects None even for the hand.
    // Infernal Blade uses Bottom for this exact destination.
    internal const CardPilePosition HandPosition = CardPilePosition.Bottom;

    internal static async Task AddToHand(CardModel canonical, Player owner)
    {
        // CardFactory registers the generated instance with CombatState.
        // A plain ToMutable() card reaches the hand but crashes FlushPlayerHand.
        var card = CardFactory.GetDistinctForCombat(owner, [canonical], 1,
            owner.RunState.Rng.CombatCardGeneration).FirstOrDefault();
        if (card == null) return;
        await CardPileCmd.AddGeneratedCardToCombat(
            card, PileType.Hand, owner, HandPosition);
    }

    internal static async Task AddExactToHand(CardModel canonical, Player owner)
    {
        // Named outcomes must not reroll from the owner's character pool.
        // CombatState.CreateCard also registers off-character cards so they
        // survive hand flushing and multiplayer card serialization.
        var combat = owner.Creature.CombatState;
        if (combat == null) return;
        var card = combat.CreateCard(canonical, owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand,
            owner, HandPosition);
    }

    internal static async Task AddToDrawPile(CardModel canonical, Player owner)
    {
        // The random-card factory filters out Curse/Status cards via
        // CanBeGeneratedInCombat, so it can silently return nothing for Injury.
        // Explicitly create the requested card within CombatState instead.
        var combat = owner.Creature.CombatState;
        if (combat == null) return;
        var card = combat.CreateCard(canonical, owner);
        await CardPileCmd.AddGeneratedCardToCombat(
            card, PileType.Draw, owner, CardPilePosition.Top);
    }
}
