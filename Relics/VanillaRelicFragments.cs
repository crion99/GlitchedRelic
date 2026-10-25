using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Enchantments;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.ValueProps;

namespace MysteriousMapMod.Relics;

/// <summary>
/// Individually verified vanilla relic fragments. The numeric values below
/// come from the installed 0.111.0 relic DynamicVars, not item-name guesses.
/// Each source condition and result can be recombined independently.
/// </summary>
internal static partial class VanillaRelicFragments
{
    private static bool _registered;
    private static readonly HashSet<string> DrawbackResults =
    [
        "royal_poison", "tea_of_discourtesy", "blood_soaked_rose",
        "blessed_antler", "calling_bell", "neows_bones", "bread"
    ];

    internal static void Register()
    {
        if (_registered) return;
        _registered = true;

        RegisterCondition("anchor", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("lantern", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("bag_of_preparation", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("oddly_smooth_stone", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("vajra", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("strawberry", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("mango", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("burning_blood", GlitchTrigger.CombatVictory, false, false);
        RegisterCondition("black_blood", GlitchTrigger.CombatVictory, false, false);
        RegisterCondition("blood_vial", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("pear", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("nutritious_oyster", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("looming_fruit", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("golden_pearl", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("old_coin", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("signet_ring", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("bag_of_marbles", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("bronze_scales", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("orichalcum", GlitchTrigger.NoBlockTurnEnd, true, true);
        RegisterCondition("centennial_puzzle", GlitchTrigger.FirstHurtCombat, true, true);
        RegisterCondition("art_of_war", GlitchTrigger.NoAttackTurnEnd, true, true);
        RegisterCondition("charons_ashes", GlitchTrigger.Exhaust, true, true);
        RegisterCondition("happy_flower", GlitchTrigger.EveryThirdTurn, true, true);
        RegisterCondition("nunchaku", GlitchTrigger.EveryTenthAttack, true, true);
        RegisterCondition("kunai", GlitchTrigger.ThirdAttackTurn, true, true);
        RegisterCondition("shuriken", GlitchTrigger.ThirdAttackTurn, true, true);
        RegisterCondition("ornamental_fan", GlitchTrigger.ThirdAttackTurn, true, true);
        RegisterCondition("horn_cleat", GlitchTrigger.SecondTurnCombat, true, true);
        RegisterCondition("captains_wheel", GlitchTrigger.ThirdTurnCombat, true, true);
        RegisterCondition("mercury_hourglass", GlitchTrigger.TurnStart, true, true);
        RegisterCondition("letter_opener", GlitchTrigger.ThirdSkillTurn, true, true);
        RegisterCondition("tingsha", GlitchTrigger.CardDiscarded, true, true);
        RegisterCondition("regal_pillow", GlitchTrigger.RestHealed, false, false);
        RegisterCondition("pocketwatch", GlitchTrigger.FewCardsTurnEnd, true, true);
        RegisterCondition("gremlin_horn", GlitchTrigger.EnemyDied, true, true);
        RegisterCondition("red_mask", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("stone_calendar", GlitchTrigger.SeventhTurnCombat, true, true);
        RegisterCondition("meat_on_the_bone", GlitchTrigger.LowHpCombatVictory, false, false);
        RegisterCondition("snecko_skull", GlitchTrigger.PoisonApplied, true, true);
        RegisterCondition("twisted_funnel", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("tough_bandages", GlitchTrigger.CardDiscarded, true, true);
        RegisterCondition("dragon_fruit", GlitchTrigger.GoldGained, false, false);
        RegisterCondition("forgotten_soul", GlitchTrigger.Exhaust, true, true);
        RegisterCondition("stone_humidifier", GlitchTrigger.RestHealed, false, false);
        RegisterCondition("fake_anchor", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("candelabra", GlitchTrigger.SecondTurnCombat, true, true);
        RegisterCondition("cloak_clasp", GlitchTrigger.TurnEnded, true, true);
        RegisterCondition("reptile_trinket", GlitchTrigger.PotionUsed, true, false);
        RegisterCondition("festive_popper", GlitchTrigger.FirstTurnCombat, true, true);
        RegisterCondition("hand_drill", GlitchTrigger.EnemyBlockBroken, true, true);
        RegisterCondition("meal_ticket", GlitchTrigger.ShopEntered, false, false);
        RegisterCondition("darkstone_periapt", GlitchTrigger.CurseAddedToDeck, false, false);
        RegisterCondition("the_abacus", GlitchTrigger.Shuffle, true, true);
        RegisterCondition("unceasing_top", GlitchTrigger.HandEmptied, true, true);
        RegisterCondition("self_forming_clay", GlitchTrigger.Hurt, true, true);
        RegisterCondition("very_hot_cocoa", GlitchTrigger.FirstTurnCombat, true, true);
        RegisterCondition("screaming_flagon", GlitchTrigger.EmptyHandTurnEnd, true, true);
        RegisterCondition("fake_mango", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("fake_lees_waffle", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("lees_waffle", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("cursed_pearl", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("pantograph", GlitchTrigger.BossCombatStarted, true, true);
        RegisterCondition("sling_of_courage", GlitchTrigger.EliteCombatStarted, true, true);
        RegisterCondition("sword_of_jade", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("gorget", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("data_disk", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("fake_blood_vial", GlitchTrigger.FirstTurnCombat, true, true);
        RegisterCondition("eternal_feather", GlitchTrigger.RestSiteEntered, false, false);
        RegisterCondition("seal_of_gold", GlitchTrigger.RichTurnStart, true, true);
        RegisterCondition("akabeko", GlitchTrigger.FirstTurnCombat, true, true);
        RegisterCondition("brimstone", GlitchTrigger.TurnStart, true, true);
        RegisterCondition("mr_struggles", GlitchTrigger.TurnStart, true, true);
        RegisterCondition("chandelier", GlitchTrigger.ThirdTurnCombat, true, true);
        RegisterCondition("bellows", GlitchTrigger.FirstTurnCombat, true, true);
        RegisterCondition("cracked_core", GlitchTrigger.FirstTurnCombat, true, true);
        RegisterCondition("runic_capacitor", GlitchTrigger.FirstTurnCombat, true, true);
        RegisterCondition("symbiotic_virus", GlitchTrigger.FirstTurnCombat, true, true);
        RegisterCondition("sai", GlitchTrigger.TurnStart, true, true);
        RegisterCondition("royal_poison", GlitchTrigger.FirstTurnCombat, true, true);
        RegisterCondition("lunar_pastry", GlitchTrigger.TurnEnded, true, true);
        RegisterCondition("fake_orichalcum", GlitchTrigger.NoBlockTurnEnd, true, true);
        RegisterCondition("orange_dough", GlitchTrigger.FirstTurnCombat, true, true);
        RegisterCondition("ring_of_the_snake", GlitchTrigger.FirstTurnCombat, true, true);
        RegisterCondition("ring_of_the_drake", GlitchTrigger.FirstThreeTurnsCombat, true, true);
        RegisterCondition("fake_happy_flower", GlitchTrigger.EveryFifthTurn, true, true);
        RegisterCondition("potion_belt", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("alchemical_coffer", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("whetstone", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("war_paint", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("dowsing_rod", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("nutritious_soup", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("stone_cracker", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("parrying_shield", GlitchTrigger.TurnEnded, true, true);
        RegisterCondition("tea_of_discourtesy", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("big_hat", GlitchTrigger.FirstTurnCombat, true, true);
        RegisterCondition("crossbow", GlitchTrigger.TurnStart, true, true);
        RegisterCondition("bone_tea", GlitchTrigger.FirstTurnAfterDraw, true, true);
        RegisterCondition("diamond_diadem", GlitchTrigger.FirstTurnCombat, true, true);
        RegisterCondition("divine_destiny", GlitchTrigger.FirstTurnCombat, true, true);
        RegisterCondition("divine_right", GlitchTrigger.CombatStarted, true, true);
        RegisterCondition("neows_sacrifice", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("neows_talisman", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("neows_torment", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("sand_castle", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("royal_stamp", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("storybook", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("tanxs_whistle", GlitchTrigger.RelicObtained, false, false);
        RegisterCondition("ninja_scroll", GlitchTrigger.FirstTurnCombat, true, true);
        RegisterCondition("jeweled_mask", GlitchTrigger.FirstTurnCombat, true, true);
        RegisterCondition("radiant_pearl", GlitchTrigger.FirstTurnCombat, true, true);
        RegisterBatch2();
        RegisterBatch3();

        RegisterResult("anchor", true, false, async context =>
            await CreatureCmd.GainBlock(context.Owner.Creature, 10m,
                ValueProp.Unpowered, null, false));
        RegisterResult("lantern", true, false, async context =>
            await PlayerCmd.GainEnergy(1m, context.Owner));
        RegisterResult("bag_of_preparation", true, true, async context =>
            await CardPileCmd.Draw(context.ChoiceContext!, 2m, context.Owner, false));
        RegisterResult("oddly_smooth_stone", true, true, async context =>
            await PowerCmd.Apply<DexterityPower>(context.ChoiceContext!, context.Owner.Creature,
                1m, context.Owner.Creature, null, false));
        RegisterResult("vajra", true, true, async context =>
            await PowerCmd.Apply<StrengthPower>(context.ChoiceContext!, context.Owner.Creature,
                1m, context.Owner.Creature, null, false));
        RegisterResult("strawberry", false, false, context => GainMaxHpOncePerRoom(context, 7m));
        RegisterResult("mango", false, false, context => GainMaxHpOncePerRoom(context, 14m));
        RegisterResult("burning_blood", false, false, async context =>
            await CreatureCmd.Heal(context.Owner.Creature, 6m, false));
        RegisterResult("black_blood", false, false, async context =>
            await CreatureCmd.Heal(context.Owner.Creature, 12m, false));
        RegisterResult("blood_vial", false, false, async context =>
            await CreatureCmd.Heal(context.Owner.Creature, 2m, false));
        RegisterResult("pear", false, false, context => GainMaxHpOncePerRoom(context, 10m));
        RegisterResult("nutritious_oyster", false, false, context => GainMaxHpOncePerRoom(context, 11m));
        RegisterResult("looming_fruit", false, false, context => GainMaxHpOncePerRoom(context, 31m));
        RegisterResult("golden_pearl", false, false, context =>
            PlayerCmd.GainGold(150m, context.Owner, false));
        RegisterResult("old_coin", false, false, context =>
            PlayerCmd.GainGold(300m, context.Owner, false));
        RegisterResult("signet_ring", false, false, context =>
            PlayerCmd.GainGold(888m, context.Owner, false));
        RegisterResult("bag_of_marbles", true, true, async context =>
        {
            var enemies = context.Owner.Creature.CombatState?.HittableEnemies.ToArray() ?? [];
            foreach (var enemy in enemies)
                await PowerCmd.Apply<VulnerablePower>(context.ChoiceContext!, enemy,
                    1m, context.Owner.Creature, null, false);
        });
        RegisterResult("bronze_scales", true, true, async context =>
            await PowerCmd.Apply<ThornsPower>(context.ChoiceContext!, context.Owner.Creature,
                3m, context.Owner.Creature, null, false));
        RegisterResult("orichalcum", true, false, async context =>
            await CreatureCmd.GainBlock(context.Owner.Creature, 6m,
                ValueProp.Unpowered, null, false));
        RegisterResult("centennial_puzzle", true, true, async context =>
            await CardPileCmd.Draw(context.ChoiceContext!, 3m, context.Owner, false));
        RegisterResult("art_of_war", true, true, async context =>
            await PowerCmd.Apply<EnergyNextTurnPower>(context.ChoiceContext!,
                context.Owner.Creature, 1m, context.Owner.Creature, null, false));
        RegisterResult("charons_ashes", true, true, async context =>
        {
            var enemies = context.Owner.Creature.CombatState?.HittableEnemies.ToArray() ?? [];
            if (enemies.Length > 0)
                await CreatureCmd.Damage(context.ChoiceContext!, enemies, 3m,
                    ValueProp.Unpowered, context.Owner.Creature);
        });
        RegisterResult("happy_flower", true, false, context =>
            PlayerCmd.GainEnergy(1m, context.Owner));
        RegisterResult("nunchaku", true, false, context =>
            PlayerCmd.GainEnergy(1m, context.Owner));
        RegisterResult("kunai", true, true, context =>
            PowerCmd.Apply<DexterityPower>(context.ChoiceContext!, context.Owner.Creature,
                1m, context.Owner.Creature, null, false));
        RegisterResult("shuriken", true, true, context =>
            PowerCmd.Apply<StrengthPower>(context.ChoiceContext!, context.Owner.Creature,
                1m, context.Owner.Creature, null, false));
        RegisterResult("ornamental_fan", true, false, async context =>
            await CreatureCmd.GainBlock(context.Owner.Creature, 4m,
                ValueProp.Unpowered, null, false));
        RegisterResult("horn_cleat", true, false, async context =>
            await CreatureCmd.GainBlock(context.Owner.Creature, 14m,
                ValueProp.Unpowered, null, false));
        RegisterResult("captains_wheel", true, false, async context =>
            await CreatureCmd.GainBlock(context.Owner.Creature, 18m,
                ValueProp.Unpowered, null, false));
        RegisterResult("mercury_hourglass", true, true,
            context => DamageAllEnemies(context, 3m));
        RegisterResult("letter_opener", true, true,
            context => DamageAllEnemies(context, 5m));
        RegisterResult("tingsha", true, true, async context =>
        {
            var enemies = context.Owner.Creature.CombatState?.HittableEnemies.ToArray() ?? [];
            if (enemies.Length == 0) return;
            var target = enemies[context.RunState.Rng.Niche.NextInt(enemies.Length)];
            await CreatureCmd.Damage(context.ChoiceContext!, [target], 3m,
                ValueProp.Unpowered, context.Owner.Creature);
        });
        RegisterResult("regal_pillow", false, false, context =>
            CreatureCmd.Heal(context.Owner.Creature, 15m, false));
        RegisterResult("pocketwatch", true, true, context =>
            PowerCmd.Apply<DrawCardsNextTurnPower>(context.ChoiceContext!,
                context.Owner.Creature, 3m, context.Owner.Creature, null, false));
        RegisterResult("gremlin_horn", true, true, async context =>
        {
            await PlayerCmd.GainEnergy(1m, context.Owner);
            await CardPileCmd.Draw(context.ChoiceContext!, 1m, context.Owner, false);
        });
        RegisterResult("red_mask", true, true, context =>
            ApplyToAllEnemies<WeakPower>(context, 1m));
        RegisterResult("stone_calendar", true, true,
            context => DamageAllEnemies(context, 52m));
        RegisterResult("meat_on_the_bone", false, false, context =>
            CreatureCmd.Heal(context.Owner.Creature, 12m, false));
        RegisterResult("snecko_skull", true, true, async context =>
        {
            var combat = context.Owner.Creature.CombatState;
            var target = context.EventEnemy != null && combat?.HittableEnemies.Contains(context.EventEnemy) == true
                ? context.EventEnemy : combat?.HittableEnemies.FirstOrDefault();
            if (target != null)
                await PowerCmd.Apply<PoisonPower>(context.ChoiceContext!, target,
                    1m, context.Owner.Creature, null, false);
        });
        RegisterResult("twisted_funnel", true, true, context =>
            ApplyToAllEnemies<PoisonPower>(context, 4m));
        RegisterResult("tough_bandages", true, false, context =>
            CreatureCmd.GainBlock(context.Owner.Creature, 3m,
                ValueProp.Unpowered, null, false));
        RegisterResult("dragon_fruit", false, false, context =>
            CreatureCmd.GainMaxHp(context.Owner.Creature, 1m));
        RegisterResult("forgotten_soul", true, true, async context =>
        {
            var enemies = context.Owner.Creature.CombatState?.HittableEnemies.ToArray() ?? [];
            if (enemies.Length == 0) return;
            var target = enemies[context.RunState.Rng.Niche.NextInt(enemies.Length)];
            await CreatureCmd.Damage(context.ChoiceContext!, [target], 1m,
                ValueProp.Unpowered, context.Owner.Creature);
        });
        RegisterResult("stone_humidifier", false, false, context =>
            CreatureCmd.GainMaxHp(context.Owner.Creature, 5m));
        RegisterResult("fake_anchor", true, false, context =>
            CreatureCmd.GainBlock(context.Owner.Creature, 4m,
                ValueProp.Unpowered, null, false));
        RegisterResult("candelabra", true, false, context =>
            PlayerCmd.GainEnergy(2m, context.Owner));
        RegisterResult("cloak_clasp", true, false, async context =>
        {
            var handSize = PileType.Hand.GetPile(context.Owner).Cards.Count;
            if (handSize > 0)
                await CreatureCmd.GainBlock(context.Owner.Creature, handSize,
                    ValueProp.Unpowered, null, false);
        });
        RegisterResult("reptile_trinket", true, true, async context =>
        {
            if (context.Owner.Creature.CombatState == null) return;
            await PowerCmd.Apply<ReptileTrinketPower>(context.ChoiceContext!, context.Owner.Creature,
                3m, context.Owner.Creature, null, false);
        });
        RegisterResult("festive_popper", true, true, context =>
            DamageAllEnemies(context, 9m));
        RegisterResult("hand_drill", true, true, async context =>
        {
            var enemies = context.Owner.Creature.CombatState?.HittableEnemies;
            var target = context.EventEnemy != null && enemies?.Contains(context.EventEnemy) == true
                ? context.EventEnemy : enemies?.FirstOrDefault();
            if (target != null)
                await PowerCmd.Apply<VulnerablePower>(context.ChoiceContext!, target,
                    2m, context.Owner.Creature, null, false);
        });
        RegisterResult("meal_ticket", false, false, context =>
            CreatureCmd.Heal(context.Owner.Creature, 15m, true));
        RegisterResult("darkstone_periapt", false, false, context =>
            CreatureCmd.GainMaxHp(context.Owner.Creature, 6m));
        RegisterResult("the_abacus", true, false, context =>
            CreatureCmd.GainBlock(context.Owner.Creature, 6m,
                ValueProp.Unpowered, null, false));
        RegisterResult("unceasing_top", true, true, async context =>
            await CardPileCmd.Draw(context.ChoiceContext!, 1m, context.Owner, false));
        RegisterResult("self_forming_clay", true, true, async context =>
            await PowerCmd.Apply<SelfFormingClayPower>(context.ChoiceContext!,
                context.Owner.Creature, 3m, context.Owner.Creature, null, false));
        RegisterResult("very_hot_cocoa", true, false, context =>
            PlayerCmd.GainEnergy(4m, context.Owner));
        RegisterResult("screaming_flagon", true, true, context =>
            DamageAllEnemies(context, 20m));
        RegisterResult("fake_mango", false, false, context =>
            CreatureCmd.GainMaxHp(context.Owner.Creature, 3m));
        RegisterResult("fake_lees_waffle", false, false, context =>
            CreatureCmd.Heal(context.Owner.Creature, 10m, true));
        RegisterResult("lees_waffle", false, false, async context =>
        {
            var creature = context.Owner.Creature;
            await CreatureCmd.GainMaxHp(creature, 7m);
            await CreatureCmd.Heal(creature, creature.MaxHp - creature.CurrentHp, true);
        });
        RegisterResult("cursed_pearl", false, false, async context =>
        {
            await CardPileCmd.AddCurseToDeck<Greed>(context.Owner);
            await PlayerCmd.GainGold(333m, context.Owner, false);
        });
        RegisterResult("pantograph", false, false, context =>
            CreatureCmd.Heal(context.Owner.Creature, 25m, true));
        RegisterResult("sling_of_courage", true, true, context =>
            PowerCmd.Apply<StrengthPower>(context.ChoiceContext!, context.Owner.Creature,
                2m, context.Owner.Creature, null, false));
        RegisterResult("sword_of_jade", true, true, context =>
            PowerCmd.Apply<StrengthPower>(context.ChoiceContext!, context.Owner.Creature,
                3m, context.Owner.Creature, null, false));
        RegisterResult("gorget", true, true, context =>
            PowerCmd.Apply<PlatingPower>(context.ChoiceContext!, context.Owner.Creature,
                4m, context.Owner.Creature, null, false));
        RegisterResult("data_disk", true, true, context =>
            PowerCmd.Apply<FocusPower>(context.ChoiceContext!, context.Owner.Creature,
                1m, context.Owner.Creature, null, false));
        RegisterResult("fake_blood_vial", false, false, context =>
            CreatureCmd.Heal(context.Owner.Creature, 1m, true));
        RegisterResult("eternal_feather", false, false, context =>
        {
            var deckSize = PileType.Deck.GetPile(context.Owner).Cards.Count;
            return CreatureCmd.Heal(context.Owner.Creature, 3m * (deckSize / 5), true);
        });
        RegisterResult("seal_of_gold", true, false, context =>
            PlayerCmd.GainEnergy(1m, context.Owner));
        RegisterResult("akabeko", true, true, context =>
            PowerCmd.Apply<VigorPower>(context.ChoiceContext!, context.Owner.Creature,
                ModelDb.Relic<Akabeko>().DynamicVars["VigorPower"].IntValue,
                context.Owner.Creature, null, false));
        RegisterResult("brimstone", true, true, async context =>
        {
            var values = ModelDb.Relic<Brimstone>().DynamicVars;
            await PowerCmd.Apply<StrengthPower>(context.ChoiceContext!, context.Owner.Creature,
                values["SelfStrength"].BaseValue, context.Owner.Creature, null, false);
            var enemies = context.Owner.Creature.CombatState?.HittableEnemies.ToArray() ?? [];
            foreach (var enemy in enemies)
                await PowerCmd.Apply<StrengthPower>(context.ChoiceContext!, enemy,
                    values["EnemyStrength"].BaseValue, context.Owner.Creature, null, false);
        });
        RegisterResult("mr_struggles", true, true, async context =>
        {
            var enemies = context.Owner.Creature.CombatState?.HittableEnemies.ToArray() ?? [];
            if (enemies.Length > 0)
                await CreatureCmd.Damage(context.ChoiceContext!, enemies,
                    context.Owner.PlayerCombatState?.TurnNumber ?? 1, ValueProp.Unpowered,
                    context.Owner.Creature);
        });
        RegisterResult("chandelier", true, false, context =>
            PlayerCmd.GainEnergy(ModelDb.Relic<Chandelier>().DynamicVars.Energy.BaseValue,
                context.Owner));
        RegisterResult("bellows", true, false, context =>
        {
            CardCmd.Upgrade(PileType.Draw.GetPile(context.Owner).Cards,
                CardPreviewStyle.HorizontalLayout);
            return Task.CompletedTask;
        });
        RegisterResult("cracked_core", true, true, async context =>
        {
            var count = ModelDb.Relic<CrackedCore>().DynamicVars["Lightning"].IntValue;
            for (var i = 0; i < count; i++)
                await OrbCmd.Channel<LightningOrb>(context.ChoiceContext!, context.Owner);
        });
        RegisterResult("runic_capacitor", true, false, context =>
            OrbCmd.AddSlots(context.Owner,
                ModelDb.Relic<RunicCapacitor>().DynamicVars.Repeat.IntValue));
        RegisterResult("symbiotic_virus", true, true, context =>
            OrbCmd.Channel<DarkOrb>(context.ChoiceContext!, context.Owner));
        RegisterResult("sai", true, false, context =>
            CreatureCmd.GainBlock(context.Owner.Creature,
                ModelDb.Relic<Sai>().DynamicVars.Block.BaseValue,
                ValueProp.Unpowered, null, false));
        RegisterResult("royal_poison", true, true, async context =>
            await CreatureCmd.Damage(context.ChoiceContext!, context.Owner.Creature,
                ModelDb.Relic<RoyalPoison>().DynamicVars.Damage.BaseValue,
                ValueProp.Unpowered, null!));
        RegisterResult("lunar_pastry", true, false, context =>
            PlayerCmd.GainStars(ModelDb.Relic<LunarPastry>().DynamicVars.Stars.BaseValue,
                context.Owner));
        RegisterResult("fake_orichalcum", true, false, context =>
            CreatureCmd.GainBlock(context.Owner.Creature,
                ModelDb.Relic<FakeOrichalcum>().DynamicVars.Block.BaseValue,
                ValueProp.Unpowered, null, false));
        RegisterResult("orange_dough", true, false, async context =>
        {
            var colorless = ModelDb.CardPool<ColorlessCardPool>()
                .GetUnlockedCards(context.Owner.UnlockState,
                    context.Owner.RunState.CardMultiplayerConstraint);
            var cards = CardFactory.GetDistinctForCombat(context.Owner, colorless,
                ModelDb.Relic<OrangeDough>().DynamicVars.Cards.IntValue,
                context.RunState.Rng.CombatCardGeneration).ToArray();
            if (cards.Length > 0)
                await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand,
                    context.Owner, CardPilePosition.Bottom);
        });
        RegisterResult("ring_of_the_snake", true, true, context =>
            CardPileCmd.Draw(context.ChoiceContext!,
                ModelDb.Relic<RingOfTheSnake>().DynamicVars.Cards.BaseValue,
                context.Owner, false));
        RegisterResult("ring_of_the_drake", true, true, context =>
            CardPileCmd.Draw(context.ChoiceContext!,
                ModelDb.Relic<RingOfTheDrake>().DynamicVars.Cards.BaseValue,
                context.Owner, false));
        RegisterResult("fake_happy_flower", true, false, context =>
            PlayerCmd.GainEnergy(
                ModelDb.Relic<FakeHappyFlower>().DynamicVars.Energy.BaseValue,
                context.Owner));
        RegisterResult("potion_belt", false, false, context =>
            PlayerCmd.GainMaxPotionCount(
                ModelDb.Relic<PotionBelt>().DynamicVars["PotionSlots"].IntValue,
                context.Owner));
        RegisterResult("alchemical_coffer", false, false, async context =>
        {
            var count = ModelDb.Relic<AlchemicalCoffer>().DynamicVars["PotionSlots"].IntValue;
            var originalSlots = context.Owner.MaxPotionCount;
            await PlayerCmd.GainMaxPotionCount(count, context.Owner);
            var potions = PotionFactory.CreateRandomPotionsOutOfCombat(context.Owner,
                count, context.RunState.Rng.CombatPotionGeneration);
            var index = 0;
            foreach (var potion in potions)
                await PotionCmd.TryToProcure(potion.ToMutable(), context.Owner,
                    originalSlots + index++);
        });
        RegisterResult("whetstone", false, false, context =>
            UpgradeRandomDeckCards(context, CardType.Attack,
                ModelDb.Relic<Whetstone>().DynamicVars.Cards.IntValue));
        RegisterResult("war_paint", false, false, context =>
            UpgradeRandomDeckCards(context, CardType.Skill,
                ModelDb.Relic<WarPaint>().DynamicVars.Cards.IntValue));
        RegisterResult("dowsing_rod", false, false, async context =>
        {
            var card = context.RunState.CreateCard<Dowsing>(context.Owner);
            await CardPileCmd.Add(card, PileType.Deck);
        });
        RegisterResult("nutritious_soup", false, false, context =>
        {
            var ember = ModelDb.Enchantment<TezcatarasEmber>();
            foreach (var card in PileType.Deck.GetPile(context.Owner).Cards.ToArray())
                if (card.Rarity == CardRarity.Basic &&
                    card.Tags.Contains(CardTag.Strike) && ember.CanEnchant(card))
                    CardCmd.Enchant<TezcatarasEmber>(card, 1m);
            return Task.CompletedTask;
        });
        RegisterResult("stone_cracker", true, false, context =>
        {
            var available = PileType.Draw.GetPile(context.Owner).Cards
                .Where(card => card.IsUpgradable).ToList();
            var selected = new List<CardModel>();
            var count = ModelDb.Relic<StoneCracker>().DynamicVars.Cards.IntValue;
            for (var i = 0; i < count && available.Count > 0; i++)
            {
                var index = context.RunState.Rng.CombatCardSelection.NextInt(available.Count);
                selected.Add(available[index]);
                available.RemoveAt(index);
            }
            if (selected.Count > 0)
                CardCmd.Upgrade(selected, CardPreviewStyle.HorizontalLayout);
            return Task.CompletedTask;
        });
        RegisterResult("parrying_shield", true, true, async context =>
        {
            var values = ModelDb.Relic<ParryingShield>().DynamicVars;
            if (context.Owner.Creature.Block < values.Block.BaseValue) return;
            var enemies = context.Owner.Creature.CombatState?.HittableEnemies.ToArray() ?? [];
            if (enemies.Length == 0) return;
            var target = enemies[context.RunState.Rng.CombatTargets.NextInt(enemies.Length)];
            await CreatureCmd.Damage(context.ChoiceContext!, [target],
                values.Damage.BaseValue, ValueProp.Unpowered, context.Owner.Creature);
        });
        RegisterResult("tea_of_discourtesy", true, false, context =>
            CardPileCmd.AddToCombatAndPreview<Dazed>(context.Owner.Creature,
                PileType.Draw,
                ModelDb.Relic<TeaOfDiscourtesy>().DynamicVars["DazedCount"].IntValue,
                context.Owner, CardPilePosition.Random));
        RegisterResult("big_hat", true, false, async context =>
        {
            var pool = context.Owner.Character.CardPool.GetUnlockedCards(
                context.Owner.UnlockState, context.Owner.RunState.CardMultiplayerConstraint)
                .Where(card => card.Keywords.Contains(CardKeyword.Ethereal)).ToArray();
            if (pool.Length == 0) return;
            var cards = CardFactory.GetDistinctForCombat(context.Owner, pool,
                ModelDb.Relic<BigHat>().DynamicVars.Cards.IntValue,
                context.RunState.Rng.CombatCardGeneration).ToArray();
            if (cards.Length > 0)
                await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand,
                    context.Owner);
        });
        RegisterResult("crossbow", true, false, async context =>
        {
            var pool = context.Owner.Character.CardPool.GetUnlockedCards(
                context.Owner.UnlockState, context.Owner.RunState.CardMultiplayerConstraint)
                .Where(card => card.Type == CardType.Attack).ToArray();
            if (pool.Length == 0) return;
            var cards = CardFactory.GetDistinctForCombat(context.Owner, pool, 1,
                context.RunState.Rng.CombatCardGeneration).ToArray();
            foreach (var card in cards) card.SetToFreeThisTurn();
            if (cards.Length > 0)
                await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand,
                    context.Owner);
        });
        RegisterResult("bone_tea", true, false, context =>
        {
            foreach (var card in PileType.Hand.GetPile(context.Owner).Cards.ToArray())
                if (card.IsUpgradable) CardCmd.Upgrade(card);
            return Task.CompletedTask;
        });
        RegisterResult("diamond_diadem", true, true, async context =>
        {
            await CreatureCmd.GainBlock(context.Owner.Creature,
                ModelDb.Relic<DiamondDiadem>().DynamicVars.Block.BaseValue,
                ValueProp.Unpowered, null, false);
            await PowerCmd.Apply<BlurPower>(context.ChoiceContext!,
                context.Owner.Creature, 1m, context.Owner.Creature, null, false);
        });
        RegisterResult("divine_destiny", true, false, context =>
            PlayerCmd.GainStars(ModelDb.Relic<DivineDestiny>().DynamicVars.Stars.BaseValue,
                context.Owner));
        RegisterResult("divine_right", true, false, context =>
            PlayerCmd.GainStars(ModelDb.Relic<DivineRight>().DynamicVars.Stars.BaseValue,
                context.Owner));
        RegisterResult("neows_sacrifice", false, false, async context =>
        {
            await PotionCmd.TryToProcure<Ambergris>(context.Owner);
            var card = context.RunState.CreateCard<Guilty>(context.Owner);
            await CardPileCmd.Add(card, PileType.Deck);
        });
        RegisterResult("neows_talisman", false, false, context =>
        {
            var basics = PileType.Deck.GetPile(context.Owner).Cards
                .Where(card => card.Rarity == CardRarity.Basic).ToArray();
            var strike = basics.LastOrDefault(card => card.Tags.Contains(CardTag.Strike));
            var defend = basics.LastOrDefault(card => card.Tags.Contains(CardTag.Defend));
            if (strike?.IsUpgradable == true) CardCmd.Upgrade(strike);
            if (defend?.IsUpgradable == true) CardCmd.Upgrade(defend);
            return Task.CompletedTask;
        });
        RegisterResult("neows_torment", false, false, async context =>
        {
            var card = context.RunState.CreateCard<NeowsFury>(context.Owner);
            await CardPileCmd.Add(card, PileType.Deck);
        });
        RegisterResult("sand_castle", false, false, context =>
            UpgradeRandomDeckCards(context, null,
                ModelDb.Relic<SandCastle>().DynamicVars.Cards.IntValue));
        RegisterResult("royal_stamp", false, false, context =>
        {
            var stamp = ModelDb.Enchantment<RoyallyApproved>();
            var cards = PileType.Deck.GetPile(context.Owner).Cards
                .Where(stamp.CanEnchant).ToArray();
            if (cards.Length > 0)
                CardCmd.Enchant<RoyallyApproved>(
                    cards[context.RunState.Rng.Niche.NextInt(cards.Length)], 1m);
            return Task.CompletedTask;
        });
        RegisterResult("storybook", false, false, async context =>
        {
            var card = context.RunState.CreateCard<BrightestFlame>(context.Owner);
            await CardPileCmd.Add(card, PileType.Deck);
        });
        RegisterResult("tanxs_whistle", false, false, async context =>
        {
            var card = context.RunState.CreateCard<Whistle>(context.Owner);
            await CardPileCmd.Add(card, PileType.Deck);
        });
        RegisterResult("ninja_scroll", true, false, context =>
            Shiv.CreateInHand(context.Owner,
                ModelDb.Relic<NinjaScroll>().DynamicVars["Shivs"].IntValue,
                context.Owner.Creature.CombatState!));
        RegisterResult("jeweled_mask", true, false, async context =>
        {
            var powers = PileType.Draw.GetPile(context.Owner).Cards
                .Where(card => card.Type == CardType.Power).ToArray();
            if (powers.Length == 0) return;
            var notInnate = powers.Where(card =>
                !card.Keywords.Contains(CardKeyword.Innate)).ToArray();
            var candidates = notInnate.Length > 0 ? notInnate : powers;
            var card = candidates[context.RunState.Rng.CombatCardSelection.NextInt(candidates.Length)];
            card.SetToFreeThisTurn();
            await CardPileCmd.Add(card, PileType.Hand);
        });
        RegisterResult("radiant_pearl", true, false, async context =>
        {
            var combat = context.Owner.Creature.CombatState;
            if (combat == null) return;
            var count = ModelDb.Relic<RadiantPearl>().DynamicVars.Cards.IntValue;
            var cards = Enumerable.Range(0, count)
                .Select(_ => combat.CreateCard<Luminesce>(context.Owner)).ToArray();
            if (cards.Length > 0)
                await CardPileCmd.AddGeneratedCardsToCombat(cards, PileType.Hand,
                    context.Owner);
        });
    }

    private static void RegisterCondition(string key, GlitchTrigger source,
        bool inCombat, bool hasChoiceContext) =>
        GlitchInteractionRegistry.RegisterBuiltinTriggerAlias(
            $"vanilla.{key}.condition", () => Label(key, "condition"),
            source, inCombat, hasChoiceContext);

    private static void RegisterResult(string key, bool combat, bool choice,
        Func<GlitchEffectContext, Task> apply) =>
        GlitchInteractionRegistry.RegisterOutcome(
            $"vanilla.{key}.result", () => Label(key, "result"),
            combat, choice, apply, DrawbackResults.Contains(key));

    private static string Label(string key, string kind) =>
        new LocString("relics", $"MYSTERIOUS_VANILLA_{key.ToUpperInvariant()}_{kind.ToUpperInvariant()}.label")
            .GetFormattedText();

    private static async Task GainMaxHpOncePerRoom(GlitchEffectContext context, decimal amount)
    {
        if (context.Relic.LastMaxHpRoomCount == context.RunState.CurrentRoomCount) return;
        context.Relic.LastMaxHpRoomCount = context.RunState.CurrentRoomCount;
        await CreatureCmd.GainMaxHp(context.Owner.Creature, amount);
    }

    private static Task UpgradeRandomDeckCards(GlitchEffectContext context,
        CardType? type, int count)
    {
        var available = PileType.Deck.GetPile(context.Owner).Cards
            .Where(card => (type == null || card.Type == type) && card.IsUpgradable).ToList();
        for (var i = 0; i < count && available.Count > 0; i++)
        {
            var index = context.RunState.Rng.Niche.NextInt(available.Count);
            CardCmd.Upgrade(available[index]);
            available.RemoveAt(index);
        }
        return Task.CompletedTask;
    }

    private static async Task DamageAllEnemies(GlitchEffectContext context, decimal amount)
    {
        var enemies = context.Owner.Creature.CombatState?.HittableEnemies.ToArray() ?? [];
        if (enemies.Length > 0)
            await CreatureCmd.Damage(context.ChoiceContext!, enemies, amount,
                ValueProp.Unpowered, context.Owner.Creature);
    }

    private static async Task ApplyToAllEnemies<TPower>(GlitchEffectContext context, decimal amount)
        where TPower : PowerModel, new()
    {
        var enemies = context.Owner.Creature.CombatState?.HittableEnemies.ToArray() ?? [];
        foreach (var enemy in enemies)
            await PowerCmd.Apply<TPower>(context.ChoiceContext!, enemy, amount,
                context.Owner.Creature, null, false);
    }
}
