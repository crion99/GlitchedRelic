using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;

namespace MysteriousMapMod.Relics;

internal static partial class VanillaRelicFragments
{
    private static void RegisterBatch2()
    {
        RegisterCondition("beautiful_bracelet", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("gnarled_hammer", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("electric_shrymp", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("punch_dagger", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("fragrant_mushroom", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("distinguished_cape", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("toasty_mittens", GlitchTrigger.AfterTurnDraw, true, true);
        RegisterCondition("tri_boomerang", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("power_cell", GlitchTrigger.FirstTurnCombat, true, true);
        RegisterCondition("vexing_puzzlebox", GlitchTrigger.FirstTurnAfterDraw, true, true);
        RegisterCondition("war_hammer", GlitchTrigger.EliteCombatVictory, false, false);
        RegisterCondition("sere_talon", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("paels_claw", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("paels_horn", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("kifuda", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("ember_tea", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("big_mushroom", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("lead_paperweight", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("hefty_tablet", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("arcane_scroll", GlitchTrigger.RelicObtained, false, false);

        RegisterResult("beautiful_bracelet", false, false, context =>
            EnchantRandomCards(context, ModelDb.Enchantment<Swift>(), 2m, 4));
        RegisterResult("gnarled_hammer", false, false, context =>
            EnchantRandomCards(context, ModelDb.Enchantment<Sharp>(), 3m, 3));
        RegisterResult("electric_shrymp", false, false, context =>
            EnchantRandomCards(context, ModelDb.Enchantment<Imbued>(), 1m, 1));
        RegisterResult("punch_dagger", false, false, context =>
            EnchantRandomCards(context, ModelDb.Enchantment<Momentum>(), 5m, 1));
        RegisterResult("fragrant_mushroom", false, false, async context =>
        {
            await CreatureCmd.Damage(new ThrowingPlayerChoiceContext(),
                context.Owner.Creature,
                ModelDb.Relic<FragrantMushroom>().DynamicVars.HpLoss.BaseValue,
                ValueProp.Unblockable | ValueProp.Unpowered, null, null);
            await UpgradeRandomDeckCards(context, null,
                ModelDb.Relic<FragrantMushroom>().DynamicVars.Cards.IntValue);
        });
        RegisterResult("distinguished_cape", false, false, async context =>
        {
            var values = ModelDb.Relic<DistinguishedCape>().DynamicVars;
            var curses = ModelDb.CardPool<CurseCardPool>()
                .GetUnlockedCards(context.Owner.UnlockState,
                    context.Owner.RunState.CardMultiplayerConstraint)
                .Where(card => card.CanBeGeneratedByModifiers)
                .OrderBy(card => card.Id).ToList();
            for (var i = 0; i < values["Curses"].IntValue && curses.Count > 0; i++)
            {
                var index = context.RunState.Rng.Niche.NextInt(curses.Count);
                await CardPileCmd.Add(context.RunState.CreateCard(curses[index],
                    context.Owner), PileType.Deck);
                curses.RemoveAt(index);
            }
            for (var i = 0; i < values.Cards.IntValue; i++)
                await CardPileCmd.Add(context.RunState.CreateCard<Apparition>(context.Owner),
                    PileType.Deck);
        });
        RegisterResult("toasty_mittens", true, true, async context =>
        {
            var hand = PileType.Hand.GetPile(context.Owner).Cards.ToArray();
            if (hand.Length > 0)
                await CardCmd.Exhaust(context.ChoiceContext!,
                    hand[context.RunState.Rng.Niche.NextInt(hand.Length)]);
            await PowerCmd.Apply<StrengthPower>(context.ChoiceContext!,
                context.Owner.Creature, 1m, context.Owner.Creature, null, false);
        });
        RegisterResult("tri_boomerang", false, false, context =>
            EnchantRandomCards(context, ModelDb.Enchantment<Instinct>(), 1m, 3));
        RegisterResult("power_cell", true, false, async context =>
        {
            var candidates = PileType.Draw.GetPile(context.Owner).Cards
                .Where(card => !card.EnergyCost.CostsX &&
                    card.EnergyCost.GetWithModifiers(CostModifiers.Local) == 0).ToList();
            var count = ModelDb.Relic<PowerCell>().DynamicVars.Cards.IntValue;
            for (var i = 0; i < count && candidates.Count > 0; i++)
            {
                var index = context.RunState.Rng.CombatCardSelection.NextInt(candidates.Count);
                await CardPileCmd.Add(candidates[index], PileType.Hand);
                candidates.RemoveAt(index);
            }
        });
        RegisterResult("vexing_puzzlebox", true, false, async context =>
        {
            var pool = context.Owner.Character.CardPool.GetUnlockedCards(
                context.Owner.UnlockState,
                context.Owner.RunState.CardMultiplayerConstraint);
            var card = CardFactory.GetDistinctForCombat(context.Owner, pool, 1,
                context.RunState.Rng.CombatCardGeneration).FirstOrDefault();
            if (card == null) return;
            card.SetToFreeThisTurn();
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand,
                context.Owner, GlitchCardGeneration.HandPosition);
        });
        RegisterResult("war_hammer", false, false, context =>
            UpgradeRandomDeckCards(context, null,
                ModelDb.Relic<WarHammer>().DynamicVars.Cards.IntValue));
        RegisterResult("sere_talon", true, false, async context =>
        {
            var values = ModelDb.Relic<SereTalon>().DynamicVars;
            await CreatureCmd.LoseMaxHp(new ThrowingPlayerChoiceContext(),
                context.Owner.Creature, values.HpLoss.BaseValue, false);
            for (var i = 0; i < values["Wishes"].IntValue; i++)
                await GlitchCardGeneration.AddExactToHand(ModelDb.Card<Wish>(),
                    context.Owner);
        });
        RegisterResult("paels_claw", false, false, context =>
            EnchantRandomCards(context, ModelDb.Enchantment<Goopy>(), 1m,
                int.MaxValue));
        RegisterResult("paels_horn", false, false, async context =>
        {
            for (var i = 0; i < 2; i++)
                await CardPileCmd.Add(context.RunState.CreateCard<Relax>(context.Owner),
                    PileType.Deck);
        });
        RegisterResult("kifuda", false, false, context =>
            EnchantRandomCards(context, ModelDb.Enchantment<Adroit>(), 3m, 3));
        RegisterResult("ember_tea", true, true, context =>
            PowerCmd.Apply<StrengthPower>(context.ChoiceContext!,
                context.Owner.Creature,
                ModelDb.Relic<EmberTea>().DynamicVars.Strength.BaseValue,
                context.Owner.Creature, null, false));
        RegisterResult("big_mushroom", false, false, context =>
            CreatureCmd.GainMaxHp(context.Owner.Creature,
                ModelDb.Relic<BigMushroom>().DynamicVars.MaxHp.BaseValue));
        RegisterResult("lead_paperweight", false, false, async context =>
        {
            var pool = ModelDb.CardPool<ColorlessCardPool>()
                .GetUnlockedCards(context.Owner.UnlockState,
                    context.Owner.RunState.CardMultiplayerConstraint)
                .Where(card => card.CanBeGeneratedByModifiers).ToArray();
            await AddRandomDeckCard(context, pool);
        });
        RegisterResult("hefty_tablet", false, false, async context =>
        {
            await AddRandomDeckCard(context, CurrentCharacterRareCards(context));
            await CardPileCmd.Add(context.RunState.CreateCard<Injury>(context.Owner),
                PileType.Deck);
        });
        RegisterResult("arcane_scroll", false, false, context =>
            AddRandomDeckCard(context, CurrentCharacterRareCards(context)));
    }

    private static Task EnchantRandomCards(GlitchEffectContext context,
        EnchantmentModel enchantment, decimal amount, int count)
    {
        var eligible = PileType.Deck.GetPile(context.Owner).Cards
            .Where(enchantment.CanEnchant).ToList();
        for (var i = 0; i < count && eligible.Count > 0; i++)
        {
            var index = context.RunState.Rng.Niche.NextInt(eligible.Count);
            CardCmd.Enchant(enchantment.ToMutable(), eligible[index], amount);
            eligible.RemoveAt(index);
        }
        return Task.CompletedTask;
    }

    private static CardModel[] CurrentCharacterRareCards(GlitchEffectContext context) =>
        context.Owner.Character.CardPool.GetUnlockedCards(context.Owner.UnlockState,
                context.Owner.RunState.CardMultiplayerConstraint)
            .Where(card => card.Rarity == CardRarity.Rare &&
                card.CanBeGeneratedByModifiers).ToArray();

    private static async Task AddRandomDeckCard(GlitchEffectContext context,
        IReadOnlyList<CardModel> candidates)
    {
        if (candidates.Count == 0) return;
        var canonical = candidates[context.RunState.Rng.Niche.NextInt(candidates.Count)];
        await CardPileCmd.Add(context.RunState.CreateCard(canonical, context.Owner),
            PileType.Deck);
    }
}
