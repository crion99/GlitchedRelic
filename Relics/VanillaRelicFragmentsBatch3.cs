using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.ValueProps;

namespace MysteriousMapMod.Relics;

internal static partial class VanillaRelicFragments
{
    private static void RegisterBatch3()
    {
        RegisterCondition("biiig_hug", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("pomander", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("new_leaf", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("yummy_cookie", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("paels_growth", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("empty_cage", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("astrolabe", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("jewelry_box", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("preserved_fog", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("precise_scissors", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("bone_flute", GlitchTrigger.AttackPlayed, true, true);
        RegisterCondition("game_piece", GlitchTrigger.PowerPlayed, true, true);
        RegisterCondition("ivory_tile", GlitchTrigger.ThreeEnergyCardPlayed, true, true);
        RegisterCondition("regalite", GlitchTrigger.CardGenerated, true, true);
        RegisterCondition("permafrost", GlitchTrigger.CardPlayed, true, true);
        RegisterCondition("tuning_fork", GlitchTrigger.CardPlayed, true, true);
        RegisterCondition("sparkling_rouge", GlitchTrigger.BlockCleared, true, false);
        RegisterCondition("mini_regent", GlitchTrigger.StarsSpent, true, false);
        RegisterCondition("helical_dart", GlitchTrigger.CardPlayed, true, true);
        RegisterCondition("chosen_cheese", GlitchTrigger.CombatVictory, false, false);
        RegisterCondition("daughter_of_the_wind", GlitchTrigger.CardPlayed, true, true);
        RegisterCondition("blood_soaked_rose", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("belt_buckle", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("blessed_antler", GlitchTrigger.FirstTurnCombat, true, true);
        RegisterCondition("funerary_mask", GlitchTrigger.FirstTurnCombat, true, true);
        RegisterCondition("lucky_fysh", GlitchTrigger.CardAddedToDeck, false, false);
        RegisterCondition("maw_bank", GlitchTrigger.RoomEntered, false, false);
        RegisterCondition("phial_holster", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("fake_venerable_tea_set", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("venerable_tea_set", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("cauldron", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("calling_bell", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("kaleidoscope", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("lost_coffer", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("neows_bones", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("orrery", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("scroll_boxes", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("sea_glass", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("paels_tooth", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("claws", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("leafy_poultice", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("bread", GlitchTrigger.TurnStart, true, true);
        RegisterCondition("booming_conch", GlitchTrigger.EliteCombatStarted, true, true);
        RegisterCondition("gambling_chip", GlitchTrigger.FirstTurnAfterDraw, true, true);
        RegisterCondition("girya", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("iron_club", GlitchTrigger.FourthCardTurn, true, true);
        RegisterCondition("rainbow_ring", GlitchTrigger.AllThreeTypesTurn, true, true);
        RegisterCondition("intimidating_helmet", GlitchTrigger.TwoEnergyCardPlayed, true, true);
        RegisterCondition("joss_paper", GlitchTrigger.FifthExhaustTurn, true, true);
        RegisterCondition("mummified_hand", GlitchTrigger.PowerPlayed, true, true);

        RegisterResult("biiig_hug", false, false, context => RemoveRandomDeckCards(context, 4));
        RegisterResult("pomander", false, false, context => UpgradeRandomDeckCards(context, null, 1));
        RegisterResult("new_leaf", false, false, context => TransformRandomDeckCards(context, 1, false));
        RegisterResult("yummy_cookie", false, false, context => UpgradeRandomDeckCards(context, null, 4));
        RegisterResult("paels_growth", false, false, context =>
            EnchantRandomCards(context, ModelDb.Enchantment<Clone>(), 4m, 1));
        RegisterResult("empty_cage", false, false, context => RemoveRandomDeckCards(context, 2));
        RegisterResult("astrolabe", false, false, context => TransformRandomDeckCards(context, 3, true));
        RegisterResult("jewelry_box", true, false, async context =>
        {
            await GlitchCardGeneration.AddExactToHand(ModelDb.Card<Apotheosis>(),
                context.Owner);
        });
        RegisterResult("preserved_fog", false, false, async context =>
        {
            await RemoveRandomDeckCards(context, 3);
            await CardPileCmd.AddCurseToDeck<Folly>(context.Owner);
        });
        RegisterResult("precise_scissors", false, false, context => RemoveRandomDeckCards(context, 1));
        RegisterResult("bone_flute", true, false, context =>
            CreatureCmd.GainBlock(context.Owner.Creature, 2m, ValueProp.Unpowered, null, false));
        RegisterResult("game_piece", true, true, context =>
            CardPileCmd.Draw(context.ChoiceContext!, 1m, context.Owner, false));
        RegisterResult("ivory_tile", true, false, context =>
            PlayerCmd.GainEnergy(ModelDb.Relic<IvoryTile>().DynamicVars.Energy.BaseValue,
                context.Owner));
        RegisterResult("regalite", true, false, context =>
            CreatureCmd.GainBlock(context.Owner.Creature, 4m,
                ValueProp.Unpowered, null, false));
        RegisterResult("permafrost", true, false, context =>
            CreatureCmd.GainBlock(context.Owner.Creature, 7m,
                ValueProp.Unpowered, null, false));
        RegisterResult("tuning_fork", true, false, context =>
            CreatureCmd.GainBlock(context.Owner.Creature,
                ModelDb.Relic<TuningFork>().DynamicVars.Block.BaseValue,
                ValueProp.Unpowered, null, false));
        RegisterResult("sparkling_rouge", true, false, async context =>
        {
            var values = ModelDb.Relic<SparklingRouge>().DynamicVars;
            var choice = new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext();
            await PowerCmd.Apply<StrengthPower>(choice, context.Owner.Creature,
                values.Strength.BaseValue, context.Owner.Creature, null, false);
            await PowerCmd.Apply<DexterityPower>(choice, context.Owner.Creature,
                values.Dexterity.BaseValue, context.Owner.Creature, null, false);
        });
        RegisterResult("mini_regent", true, false, context =>
            PowerCmd.Apply<StrengthPower>(new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(),
                context.Owner.Creature,
                1m, context.Owner.Creature, null, false));
        RegisterResult("helical_dart", true, true, context =>
            PowerCmd.Apply<HelicalDartPower>(context.ChoiceContext!, context.Owner.Creature,
                1m, context.Owner.Creature, null, false));
        RegisterResult("chosen_cheese", false, false, context =>
            GainMaxHpOncePerRoom(context, 1m));
        RegisterResult("daughter_of_the_wind", true, false, context =>
            CreatureCmd.GainBlock(context.Owner.Creature, 1m,
                ValueProp.Unpowered, null, false));
        RegisterResult("blood_soaked_rose", false, false, context =>
            CardPileCmd.AddCurseToDeck<Enthralled>(context.Owner));
        RegisterResult("belt_buckle", true, true, context =>
            PowerCmd.Apply<DexterityPower>(context.ChoiceContext!, context.Owner.Creature,
                2m, context.Owner.Creature, null, false));
        RegisterResult("blessed_antler", true, false, context =>
            CardPileCmd.AddToCombatAndPreview<Dazed>(context.Owner.Creature,
                PileType.Draw, 3, context.Owner, CardPilePosition.Random));
        RegisterResult("funerary_mask", true, false, context =>
            CardPileCmd.AddToCombatAndPreview<Soul>(context.Owner.Creature,
                PileType.Draw, 3, context.Owner, CardPilePosition.Random));
        RegisterResult("lucky_fysh", false, false, context =>
            PlayerCmd.GainGold(15m, context.Owner, false));
        RegisterResult("maw_bank", false, false, context =>
            PlayerCmd.GainGold(12m, context.Owner, false));
        RegisterResult("phial_holster", false, false, context =>
            PlayerCmd.GainMaxPotionCount(ModelDb.Relic<PhialHolster>()
                .DynamicVars["PotionSlots"].IntValue, context.Owner));
        RegisterResult("fake_venerable_tea_set", true, false, context =>
            PlayerCmd.GainEnergy(1m, context.Owner));
        RegisterResult("venerable_tea_set", true, false, context =>
            PlayerCmd.GainEnergy(2m, context.Owner));
        RegisterResult("cauldron", false, false, async context =>
        {
            var potions = PotionFactory.CreateRandomPotionsOutOfCombat(context.Owner,
                1, context.RunState.Rng.CombatPotionGeneration);
            foreach (var potion in potions)
                await PotionCmd.TryToProcure(potion.ToMutable(), context.Owner);
        });
        RegisterResult("calling_bell", false, false, context =>
            CardPileCmd.AddCurseToDeck<CurseOfTheBell>(context.Owner));
        RegisterResult("kaleidoscope", false, false, async context =>
        {
            var pools = context.Owner.UnlockState.CharacterCardPools
                .Where(pool => pool != context.Owner.Character.CardPool).ToArray();
            for (var i = 0; i < 2 && pools.Length > 0; i++)
            {
                var pool = pools[context.RunState.Rng.Niche.NextInt(pools.Length)];
                var cards = pool.GetUnlockedCards(context.Owner.UnlockState,
                    context.Owner.RunState.CardMultiplayerConstraint)
                    .Where(card => card.CanBeGeneratedByModifiers).ToArray();
                await AddRandomDeckCard(context, cards);
            }
        });
        RegisterResult("lost_coffer", false, false, async context =>
        {
            var cards = context.Owner.Character.CardPool.GetUnlockedCards(
                context.Owner.UnlockState, context.Owner.RunState.CardMultiplayerConstraint)
                .Where(card => card.CanBeGeneratedByModifiers).ToArray();
            await AddRandomDeckCard(context, cards);
            var potions = PotionFactory.CreateRandomPotionsOutOfCombat(context.Owner,
                1, context.RunState.Rng.CombatPotionGeneration);
            foreach (var potion in potions)
                await PotionCmd.TryToProcure(potion.ToMutable(), context.Owner);
        });
        RegisterResult("neows_bones", false, false, async context =>
        {
            var candidates = ModelDb.CardPool<CurseCardPool>()
                .GetUnlockedCards(context.Owner.UnlockState,
                    context.Owner.RunState.CardMultiplayerConstraint)
                .Where(card => card.CanBeGeneratedByModifiers).ToList();
            var count = ModelDb.Relic<NeowsBones>().DynamicVars["Curses"].IntValue;
            for (var i = 0; i < count && candidates.Count > 0; i++)
            {
                var index = context.RunState.Rng.Niche.NextInt(candidates.Count);
                await CardPileCmd.Add(context.RunState.CreateCard(candidates[index], context.Owner),
                    PileType.Deck);
                candidates.RemoveAt(index);
            }
        });
        RegisterResult("orrery", false, false, async context =>
        {
            var cards = context.Owner.Character.CardPool.GetUnlockedCards(
                context.Owner.UnlockState, context.Owner.RunState.CardMultiplayerConstraint)
                .Where(card => card.CanBeGeneratedByModifiers).ToArray();
            for (var i = 0; i < ModelDb.Relic<Orrery>().DynamicVars.Cards.IntValue; i++)
                await AddRandomDeckCard(context, cards);
        });
        RegisterResult("scroll_boxes", false, false, async context =>
        {
            var cards = context.Owner.Character.CardPool.GetUnlockedCards(
                context.Owner.UnlockState, context.Owner.RunState.CardMultiplayerConstraint)
                .Where(card => card.CanBeGeneratedByModifiers).ToArray();
            for (var i = 0; i < 2; i++)
                await AddRandomDeckCard(context,
                    cards.Where(card => card.Rarity == CardRarity.Common).ToArray());
            await AddRandomDeckCard(context,
                cards.Where(card => card.Rarity == CardRarity.Uncommon).ToArray());
        });
        RegisterResult("sea_glass", false, false, async context =>
        {
            var cards = context.Owner.Character.CardPool.GetUnlockedCards(
                context.Owner.UnlockState, context.Owner.RunState.CardMultiplayerConstraint)
                .Where(card => card.CanBeGeneratedByModifiers).ToArray();
            foreach (var rarity in new[] { CardRarity.Common, CardRarity.Uncommon,
                         CardRarity.Rare })
                await AddRandomDeckCard(context,
                    cards.Where(card => card.Rarity == rarity).ToArray());
        });
        RegisterResult("paels_tooth", false, false, context =>
            RemoveRandomDeckCards(context, 5));
        RegisterResult("claws", false, false, async context =>
        {
            if (context.Relic.LastDeckMutationRoomCount == context.RunState.CurrentRoomCount)
                return;
            var candidates = PileType.Deck.GetPile(context.Owner).Cards
                .Where(card => card.Type != CardType.Curse).ToList();
            for (var i = 0; i < 6 && candidates.Count > 0; i++)
            {
                var index = context.RunState.Rng.Niche.NextInt(candidates.Count);
                var card = candidates[index];
                candidates.RemoveAt(index);
                var maul = context.RunState.CreateCard<Maul>(context.Owner);
                if (card.IsUpgraded && maul.IsUpgradable) CardCmd.Upgrade(maul);
                await CardCmd.Transform(card, maul);
                context.Relic.LastDeckMutationRoomCount = context.RunState.CurrentRoomCount;
            }
        });
        RegisterResult("leafy_poultice", false, false, async context =>
        {
            if (context.Relic.LastDeckMutationRoomCount == context.RunState.CurrentRoomCount)
                return;
            await CreatureCmd.LoseMaxHp(new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(),
                context.Owner.Creature, 12m, false);
            context.Relic.LastDeckMutationRoomCount = context.RunState.CurrentRoomCount;
            var basics = PileType.Deck.GetPile(context.Owner).Cards
                .Where(card => card.Rarity == CardRarity.Basic).ToArray();
            foreach (var tag in new[] { CardTag.Strike, CardTag.Defend })
            {
                var card = basics.FirstOrDefault(item => item.Tags.Contains(tag));
                if (card != null)
                    await CardCmd.TransformToRandom(card, context.RunState.Rng.Niche);
            }
        });
        RegisterResult("bread", true, false, context =>
            PlayerCmd.LoseEnergy(2m, context.Owner));
        RegisterResult("booming_conch", true, false, context =>
            PlayerCmd.GainEnergy(1m, context.Owner));
        RegisterResult("gambling_chip", true, true, async context =>
        {
            var hand = PileType.Hand.GetPile(context.Owner).Cards.ToArray();
            if (hand.Length > 0)
                await CardCmd.DiscardAndDraw(context.ChoiceContext!, hand, hand.Length);
        });
        RegisterResult("girya", true, false, context =>
            PowerCmd.Apply<StrengthPower>(new MegaCrit.Sts2.Core.GameActions.Multiplayer.ThrowingPlayerChoiceContext(),
                context.Owner.Creature, 1m, context.Owner.Creature, null, false));
        RegisterResult("iron_club", true, true, context =>
            CardPileCmd.Draw(context.ChoiceContext!, 1m, context.Owner, false));
        RegisterResult("rainbow_ring", true, true, async context =>
        {
            var values = ModelDb.Relic<RainbowRing>().DynamicVars;
            await PowerCmd.Apply<StrengthPower>(context.ChoiceContext!, context.Owner.Creature,
                values.Strength.BaseValue, context.Owner.Creature, null, false);
            await PowerCmd.Apply<DexterityPower>(context.ChoiceContext!, context.Owner.Creature,
                values.Dexterity.BaseValue, context.Owner.Creature, null, false);
        });
        RegisterResult("intimidating_helmet", true, false, context =>
            CreatureCmd.GainBlock(context.Owner.Creature, 4m,
                ValueProp.Unpowered, null, false));
        RegisterResult("joss_paper", true, true, context =>
            CardPileCmd.Draw(context.ChoiceContext!, 1m, context.Owner, false));
        RegisterResult("mummified_hand", true, false, context =>
        {
            var hand = PileType.Hand.GetPile(context.Owner).Cards
                .Where(card => card.CostsEnergyOrStars(true)).ToArray();
            if (hand.Length > 0)
                hand[context.RunState.Rng.CombatCardSelection.NextInt(hand.Length)]
                    .SetToFreeThisTurn();
            return Task.CompletedTask;
        });
    }

    private static async Task RemoveRandomDeckCards(GlitchEffectContext context, int count)
    {
        if (context.Relic.LastDeckMutationRoomCount == context.RunState.CurrentRoomCount)
            return;
        var candidates = PileType.Deck.GetPile(context.Owner).Cards
            .Where(card => card.Type != CardType.Curse).ToList();
        for (var i = 0; i < count && candidates.Count > 0; i++)
        {
            var index = context.RunState.Rng.Niche.NextInt(candidates.Count);
            await CardPileCmd.RemoveFromDeck(candidates[index], true);
            context.Relic.LastDeckMutationRoomCount = context.RunState.CurrentRoomCount;
            candidates.RemoveAt(index);
        }
    }

    private static async Task TransformRandomDeckCards(GlitchEffectContext context,
        int count, bool upgrade)
    {
        if (context.Relic.LastDeckMutationRoomCount == context.RunState.CurrentRoomCount)
            return;
        var candidates = PileType.Deck.GetPile(context.Owner).Cards
            .Where(card => card.Type != CardType.Curse).ToList();
        for (var i = 0; i < count && candidates.Count > 0; i++)
        {
            var index = context.RunState.Rng.Niche.NextInt(candidates.Count);
            var card = candidates[index];
            candidates.RemoveAt(index);
            if (upgrade)
            {
                var replacement = MegaCrit.Sts2.Core.Factories.CardFactory
                    .CreateRandomCardForTransform(card, false, context.RunState.Rng.Niche);
                if (replacement.IsUpgradable) CardCmd.Upgrade(replacement);
                await CardCmd.Transform(card, replacement);
            }
            else
                await CardCmd.TransformToRandom(card, context.RunState.Rng.Niche);
            context.Relic.LastDeckMutationRoomCount = context.RunState.CurrentRoomCount;
        }
    }
}
