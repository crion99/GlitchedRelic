using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MysteriousMapMod.Relics;

internal static partial class VanillaRelicFragments
{
    private static void RegisterBatch4()
    {
        RegisterCondition("book_of_five_rings", GlitchTrigger.CardAddedToDeck, false, false);
        RegisterCondition("planisphere", GlitchTrigger.RestSiteEntered, false, false);
        RegisterCondition("delicate_frond", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("petrified_toad", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("paels_blood", GlitchTrigger.AfterTurnDraw, true, true);
        RegisterCondition("dollys_mirror", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("precarious_shears", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("pandoras_box", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("massive_scroll", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("philosophers_stone", GlitchTrigger.CombatStarted, true, true);

        RegisterResult("book_of_five_rings", false, false, async context =>
        {
            var threshold = ModelDb.Relic<BookOfFiveRings>().DynamicVars.Cards.IntValue;
            if (threshold <= 0) return;
            context.Relic.FiveRingsProgress++;
            if (context.Relic.FiveRingsProgress < threshold) return;
            context.Relic.FiveRingsProgress = 0;
            await CreatureCmd.Heal(context.Owner.Creature,
                ModelDb.Relic<BookOfFiveRings>().DynamicVars.Heal.BaseValue, false);
        });
        RegisterResult("planisphere", false, false, async context =>
        {
            if (context.Relic.LastPlanisphereRoomCount == context.RunState.CurrentRoomCount)
                return;
            context.Relic.LastPlanisphereRoomCount = context.RunState.CurrentRoomCount;
            await CreatureCmd.Heal(context.Owner.Creature,
                ModelDb.Relic<Planisphere>().DynamicVars.Heal.BaseValue, false);
        });
        RegisterResult("delicate_frond", true, false, async context =>
        {
            if (!context.Owner.HasOpenPotionSlots) return;
            var potions = PotionFactory.CreateRandomPotionsOutOfCombat(context.Owner,
                1, context.RunState.Rng.CombatPotionGeneration);
            foreach (var potion in potions)
                await PotionCmd.TryToProcure(potion.ToMutable(), context.Owner);
        });
        RegisterResult("petrified_toad", true, false, context =>
            PotionCmd.TryToProcure<PotionShapedRock>(context.Owner));
        RegisterResult("paels_blood", true, true, context =>
            CardPileCmd.Draw(context.ChoiceContext!,
                ModelDb.Relic<PaelsBlood>().DynamicVars.Cards.BaseValue,
                context.Owner, false));
        RegisterResult("dollys_mirror", false, false, async context =>
        {
            if (context.Relic.LastDollyMirrorRoomCount == context.RunState.CurrentRoomCount)
                return;
            var candidates = PileType.Deck.GetPile(context.Owner).Cards
                .Where(card => card.Type is not (CardType.Curse or CardType.Status))
                .ToArray();
            if (candidates.Length == 0) return;
            var original = candidates[context.RunState.Rng.Niche.NextInt(candidates.Length)];
            var clone = context.RunState.CloneCard(original);
            context.Relic.LastDollyMirrorRoomCount = context.RunState.CurrentRoomCount;
            await CardPileCmd.Add(clone, PileType.Deck);
        });
        RegisterResult("precarious_shears", false, false, async context =>
        {
            if (context.Relic.LastPrecariousShearsRoomCount == context.RunState.CurrentRoomCount)
                return;
            context.Relic.LastPrecariousShearsRoomCount = context.RunState.CurrentRoomCount;
            var values = ModelDb.Relic<PrecariousShears>().DynamicVars;
            var candidates = PileType.Deck.GetPile(context.Owner).Cards
                .Where(card => card.Type != CardType.Curse).ToList();
            for (var i = 0; i < values.Cards.IntValue && candidates.Count > 0; i++)
            {
                var index = context.RunState.Rng.Niche.NextInt(candidates.Count);
                var card = candidates[index];
                candidates.RemoveAt(index);
                await CardPileCmd.RemoveFromDeck(card, true);
            }
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(),
                context.Owner.Creature, values.Damage.BaseValue,
                ValueProp.Unpowered, null, null);
        });
        RegisterResult("pandoras_box", false, false, async context =>
        {
            if (context.Relic.LastPandorasBoxRoomCount == context.RunState.CurrentRoomCount)
                return;
            var basics = PileType.Deck.GetPile(context.Owner).Cards
                .Where(card => card.Tags.Contains(CardTag.Strike) ||
                    card.Tags.Contains(CardTag.Defend)).ToArray();
            if (basics.Length == 0) return;
            context.Relic.LastPandorasBoxRoomCount = context.RunState.CurrentRoomCount;
            foreach (var card in basics)
                await CardCmd.TransformToRandom(card, context.RunState.Rng.Niche);
        });
        RegisterResult("massive_scroll", false, false, async context =>
        {
            if (context.Relic.LastMassiveScrollRoomCount == context.RunState.CurrentRoomCount)
                return;
            var constraint = context.Owner.RunState.CardMultiplayerConstraint;
            var cards = context.Owner.Character.CardPool
                .GetUnlockedCards(context.Owner.UnlockState, constraint)
                .Concat(ModelDb.CardPool<ColorlessCardPool>()
                    .GetUnlockedCards(context.Owner.UnlockState, constraint))
                .Where(card => card.CanBeGeneratedByModifiers)
                .DistinctBy(card => card.Id).ToArray();
            if (cards.Length == 0) return;
            var selected = cards[context.RunState.Rng.Niche.NextInt(cards.Length)];
            context.Relic.LastMassiveScrollRoomCount = context.RunState.CurrentRoomCount;
            await CardPileCmd.Add(context.RunState.CreateCard(selected, context.Owner),
                PileType.Deck);
        });
        RegisterResult("philosophers_stone", true, true, context =>
            ApplyToAllEnemies<StrengthPower>(context, ModelDb.Relic<PhilosophersStone>()
                .DynamicVars["StrengthPower"].BaseValue));
    }
}
