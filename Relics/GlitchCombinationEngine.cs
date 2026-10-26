using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Merchant;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MysteriousMapMod.Map;
using MysteriousMapMod.Patches;
using MysteriousMapMod.Rooms;

namespace MysteriousMapMod.Relics;

internal enum GlitchTrigger
{
    Hurt = 1, Damage, Exhaust, CardPlayed, CardDrawn, TurnStart,
    BlockGained, PotionUsed, GoldGained, RoomEntered, CombatVictory,
    BuffGained, BombExploded, CardDiscarded, EnemyDied,
    BlockBroken, EnergySpent,
    AttackPlayed, SkillPlayed, PowerPlayed, PotionGained,
    CardRewardTaken, RelicRewardTaken, ShopPurchase, RestHealed,
    RestSmithed, Shuffle, HandEmptied, TurnEnded, CardGenerated,
    EnemyDebuffed, SelfDebuffed, EnemyBuffed, RelicObtained,
    GeneratedCardDrawn, PlayedCardExhausted, UnplayedCardExhausted,
    SelfPotionUsed, EnemyPotionUsed, CardBlockGained, OtherBlockGained,
    StrengthGained, PoisonApplied, WeakApplied, CombatStarted,
    FirstBlockCombat, FirstAttackCombat, ThirdCardTurn,
    NoAttackTurnEnd, NoBlockTurnEnd, FirstHurtCombat,
    EnergyRefilled, FirstPotionCombat,
    EveryThirdTurn, EveryTenthAttack, ThirdAttackTurn,
    SecondTurnCombat, ThirdTurnCombat, ThirdSkillTurn,
    FewCardsTurnEnd, SeventhTurnCombat, LowHpCombatVictory,
    FirstTurnCombat, EnemyBlockBroken, ShopEntered, CurseAddedToDeck,
    EmptyHandTurnEnd, EliteCombatStarted, BossCombatStarted,
    RestSiteEntered, RichTurnStart, EveryFifthTurn,
    FirstThreeTurnsCombat,
    TeammateHurt, TeammateDamage, TeammateBlockGained, TeammateBuffGained,
    FirstTurnAfterDraw, AfterTurnDraw, EliteCombatVictory,
    FourthCardTurn, StarsSpent, BlockCleared,
    TwoEnergyCardPlayed, ThreeEnergyCardPlayed, AllThreeTypesTurn,
    FifthExhaustTurn, CardAddedToDeck
}

internal enum GlitchOutcome
{
    Block = 1, Heal, Energy, Strength, Dexterity, Poison, Blast,
    CreativeAi, CharacterCard, Draw, MaxHp, Gold, Bomb, Reveal, Victory,
    SelfWeak, SelfVulnerable, SelfFrail, SelfPoison,
    EnemyWeak, EnemyVulnerable, EnemyFrail, EnemyStrength,
    Buffer, Artifact, Regen, Thorns, Potion,
    CharacterAttack, CharacterSkill, CharacterPower,
    TripleDraw, BigBlock, BigBlast, RandomPotionEffect,
    BigHeal, GoldBurst, BombBurst, EnergyBurst, MegaBlock,
    TinyBlast, DoubleCharacterCard, EnemyHeavyPoison,
    SelfStrengthLoss, RandomBlessing,
    FirstBlockDouble, Intangible, EnergyNextTurn,
    DrawNextTurn, Barricade, Afterimage, DoubleDamage,
    Vigor, Ritual, BlockNextTurn,
    RandomEnemyDebuff, RandomSelfDebuff,
    AnyCharacterCard, IroncladCard, SilentCard, DefectCard,
    RegentCard, NecrobinderCard, LoseHp, LoseGold, AddCurse, RemoveCard,
    SpawnEnemy, EnemySoar, EnemyIntangible, RandomEnemyBuff,
    TeammateDamage, TeammateInjury, TeammateCard, TeammateStrength,
    TeammateBlock, TeammateHeal, TeammateEndTurn, TeammateRandomBuff,
    Relic
}

internal readonly record struct GlitchCombination(
    GlitchTrigger Trigger, GlitchOutcome Outcome)
{
    public override string ToString() => $"{(int)Trigger}:{(int)Outcome}";
}

internal readonly record struct ExternalGlitchCombination(
    string TriggerId, string OutcomeId)
{
    public override string ToString() => $"{TriggerId}>{OutcomeId}";
}

public sealed partial class MysteriousGlitchedRelic
{
    private static readonly GlitchTrigger[] FrequentTriggers =
    [
        GlitchTrigger.Hurt, GlitchTrigger.Damage, GlitchTrigger.CardPlayed,
        GlitchTrigger.CardDrawn, GlitchTrigger.TurnStart, GlitchTrigger.TurnEnded,
        GlitchTrigger.BlockGained, GlitchTrigger.AttackPlayed,
        GlitchTrigger.SkillPlayed, GlitchTrigger.PowerPlayed,
        GlitchTrigger.CardGenerated, GlitchTrigger.EnemyDebuffed,
        GlitchTrigger.BuffGained, GlitchTrigger.EnemyDied,
        GlitchTrigger.CombatStarted, GlitchTrigger.CombatVictory,
        GlitchTrigger.TeammateHurt, GlitchTrigger.TeammateDamage
    ];
    private readonly GlitchCascadeLimiter _cascade = new();
    private bool _combatStartPending;
    private bool _firstBlockSeenCombat;
    private bool _firstAttackSeenCombat;
    private bool _firstHurtSeenCombat;
    private bool _firstPotionSeenCombat;
    private bool _firstBlockDoubleArmed;
    private bool _firstBlockDoubleUsed;
    private int _cardsPlayedThisTurn;
    private int _attacksPlayedThisTurn;
    private int _skillsPlayedThisTurn;
    private int _powersPlayedThisTurn;
    private int _exhaustedThisTurn;
    private bool _allThreeTypesTriggeredThisTurn;
    private int _turnNumberInCombat;

    [SavedProperty]
    public int FlowerTurnCounter { get; set; }

    [SavedProperty]
    public int FakeFlowerTurnCounter { get; set; }

    [SavedProperty]
    public int TenAttackCounter { get; set; }

    [SavedProperty]
    public string ComboCodes { get; set; } = string.Empty;

    [SavedProperty]
    public string ExternalComboCodes { get; set; } = string.Empty;

    [SavedProperty]
    public int LastMaxHpRoomCount { get; set; } = -1;

    [SavedProperty]
    public int LastDeckMutationRoomCount { get; set; } = -1;

    [SavedProperty]
    public int LastInjuryRoomCount { get; set; } = -1;

    [SavedProperty]
    public int LastGrantedRelicRoomCount { get; set; } = -1;

    [SavedProperty]
    public int LastDollyMirrorRoomCount { get; set; } = -1;

    [SavedProperty]
    public int LastMassiveScrollRoomCount { get; set; } = -1;

    [SavedProperty]
    public int LastPlanisphereRoomCount { get; set; } = -1;

    [SavedProperty]
    public int LastPrecariousShearsRoomCount { get; set; } = -1;

    [SavedProperty]
    public int LastPandorasBoxRoomCount { get; set; } = -1;

    [SavedProperty]
    public int FiveRingsProgress { get; set; }

    internal int AddRandomCombinations()
    {
        if (Owner?.RunState is not RunState state)
            return 0;
        var combos = ParseCombinations().ToList();
        var external = ParseExternalCombinations().ToList();
        var before = combos.Count + external.Count;
        // Every pickup adds its own 3–8 links. Existing links remain active;
        // duplicate links are allowed so repeated pickups have no hard cap.
        var target = before + RollInitialCombinationCount(state.Rng.Niche.NextInt(35));
        for (var attempt = 0; combos.Count + external.Count < target && attempt < 512; attempt++)
        {
            if (state.Rng.Niche.NextInt(4) == 0 &&
                TryRollExternal(state, Owner, out var externalNext) &&
                CanAddLink(combos, external, externalNext.TriggerId, externalNext.OutcomeId))
            {
                external.Add(externalNext);
                continue;
            }
            var next = RollCombination(state, Owner);
            if (combos.Count > 0 && state.Rng.Niche.NextInt(3) == 0)
            {
                var anchor = combos[state.Rng.Niche.NextInt(combos.Count)];
                if (state.Rng.Niche.NextInt(2) == 0 &&
                    IsCompatible(anchor.Trigger, next.Outcome))
                    next = new GlitchCombination(anchor.Trigger, next.Outcome);
                else if (IsCompatible(next.Trigger, anchor.Outcome))
                    next = new GlitchCombination(next.Trigger, anchor.Outcome);
            }
            if (CanAddLink(combos, external, $"core.t{(int)next.Trigger}", $"core.o{(int)next.Outcome}"))
                combos.Add(next);
        }
        // As the pool fills, new links can fail the 1:2 / 2:1 topology check.
        // Repeating an existing valid link still stacks its effect and ensures
        // the full rolled count is granted on every pickup.
        while (combos.Count + external.Count < target)
        {
            if (combos.Count > 0)
                combos.Add(combos[state.Rng.Niche.NextInt(combos.Count)]);
            else if (external.Count > 0)
                external.Add(external[state.Rng.Niche.NextInt(external.Count)]);
            else
                combos.Add(new GlitchCombination(GlitchTrigger.CardPlayed, GlitchOutcome.Block));
        }
        ComboCodes = string.Join('|', combos.Select(combo => combo.ToString()));
        ExternalComboCodes = string.Join('|', external.Select(combo => combo.ToString()));
        return combos.Count + external.Count - before;
    }

    internal static int RollInitialCombinationCount(int roll) => roll switch
    {
        < 14 => 3,
        < 22 => 4,
        < 28 => 5,
        < 32 => 6,
        < 34 => 7,
        _ => 8
    };

    private static bool CanAddLink(IEnumerable<GlitchCombination> core,
        IEnumerable<ExternalGlitchCombination> external, string triggerId, string outcomeId)
    {
        var links = core.Select(item => ($"core.t{(int)item.Trigger}", $"core.o{(int)item.Outcome}"))
            .Concat(external.Select(item => (item.TriggerId, item.OutcomeId))).ToArray();
        var triggers = new HashSet<string> { triggerId };
        var outcomes = new HashSet<string> { outcomeId };
        bool changed;
        do
        {
            changed = false;
            foreach (var link in links)
                if (triggers.Contains(link.Item1) || outcomes.Contains(link.Item2))
                    changed |= triggers.Add(link.Item1) | outcomes.Add(link.Item2);
        } while (changed);
        return (triggers.Count == 1 && outcomes.Count <= 2) ||
               (triggers.Count <= 2 && outcomes.Count == 1);
    }

    private static bool TryRollExternal(RunState state, Player owner, out ExternalGlitchCombination combo)
    {
        combo = default;
        var triggers = GlitchInteractionRegistry.AllTriggers
            .Where(item => GlitchCharacterPool.Allows(owner.Character, item.Id)).ToArray();
        var outcomes = GlitchInteractionRegistry.AllOutcomes
            .Where(item => GlitchCharacterPool.Allows(owner.Character, item.Id)).ToArray();
        if (triggers.Length == 0 && outcomes.Length == 0) return false;
        string triggerId;
        string outcomeId;
        if (triggers.Length > 0 && (outcomes.Length == 0 || state.Rng.Niche.NextInt(2) == 0))
        {
            var trigger = triggers[state.Rng.Niche.NextInt(triggers.Length)];
            triggerId = trigger.Id;
            var representative = !trigger.InCombat ? GlitchTrigger.RoomEntered :
                trigger.HasChoiceContext ? GlitchTrigger.CardPlayed : GlitchTrigger.CardGenerated;
            var candidates = Enum.GetValues<GlitchOutcome>()
                .Where(result => IsCompatible(representative, result) &&
                    GlitchCharacterPool.Allows(owner.Character, result) &&
                    GlitchTeamEffects.Allows(state.Players.Count, result))
                .Select(result => $"core.o{(int)result}")
                .Concat(outcomes.Where(result =>
                        (!result.RequiresCombat || trigger.InCombat) &&
                        (!result.RequiresChoiceContext || trigger.HasChoiceContext))
                    .Select(result => result.Id))
                .ToArray();
            if (candidates.Length == 0) return false;
            if (state.Rng.Niche.NextInt(10) < 5)
            {
                var drawbacks = candidates.Where(IsDrawbackOutcomeId).ToArray();
                if (drawbacks.Length > 0) candidates = drawbacks;
            }
            outcomeId = candidates[state.Rng.Niche.NextInt(candidates.Length)];
        }
        else
        {
            var coreTriggers = Enum.GetValues<GlitchTrigger>()
                .Where(item => GlitchCharacterPool.Allows(owner.Character, item) &&
                    GlitchTeamEffects.Allows(state.Players.Count, item)).ToArray();
            var trigger = coreTriggers[state.Rng.Niche.NextInt(coreTriggers.Length)];
            triggerId = $"core.t{(int)trigger}";
            var inCombat = IsCompatible(trigger, GlitchOutcome.CharacterCard);
            var hasChoiceContext = IsCompatible(trigger, GlitchOutcome.Strength);
            var candidates = outcomes.Where(result =>
                    (!result.RequiresCombat || inCombat) &&
                    (!result.RequiresChoiceContext || hasChoiceContext))
                .ToArray();
            if (candidates.Length == 0) return false;
            if (state.Rng.Niche.NextInt(10) < 5)
            {
                var drawbacks = candidates.Where(result => result.IsDrawback).ToArray();
                if (drawbacks.Length > 0) candidates = drawbacks;
            }
            outcomeId = candidates[state.Rng.Niche.NextInt(candidates.Length)].Id;
        }
        combo = new(triggerId, outcomeId);
        return true;
    }

    private static bool IsDrawbackOutcomeId(string id)
    {
        if (id.StartsWith("core.o", StringComparison.Ordinal) &&
            int.TryParse(id.AsSpan(6), out var number) &&
            Enum.IsDefined((GlitchOutcome)number))
            return IsDrawback((GlitchOutcome)number);
        return GlitchInteractionRegistry.TryGetOutcome(id, out var outcome) &&
            outcome.IsDrawback;
    }

    private static GlitchCombination RollCombination(RunState state, Player owner)
    {
        var rng = state.Rng.Niche;
        // Rare deliberately absurd result, independent of the ordinary pool.
        if (rng.NextInt(100) == 0)
            return new GlitchCombination(GlitchTrigger.Exhaust, GlitchOutcome.Victory);

        // Feedback recipes are weighted up: their trigger can be caused by
        // their own result, or by another glitched result in the same chain.
        if (rng.NextInt(100) < 18)
        {
            var recipes = new (GlitchTrigger Trigger, GlitchOutcome Outcome)[]
            {
                (GlitchTrigger.CardGenerated, GlitchOutcome.CharacterCard),
                (GlitchTrigger.CardGenerated, GlitchOutcome.DoubleCharacterCard),
                (GlitchTrigger.BlockGained, GlitchOutcome.Block),
                (GlitchTrigger.GoldGained, GlitchOutcome.Gold),
                (GlitchTrigger.PotionGained, GlitchOutcome.Potion),
                (GlitchTrigger.BuffGained, GlitchOutcome.Strength),
                (GlitchTrigger.Damage, GlitchOutcome.TinyBlast),
                (GlitchTrigger.EnemyDebuffed, GlitchOutcome.Poison)
            };
            var available = recipes.Where(item =>
                GlitchCharacterPool.Allows(owner.Character, item.Trigger) &&
                GlitchCharacterPool.Allows(owner.Character, item.Outcome) &&
                GlitchTeamEffects.Allows(state.Players.Count, item.Trigger) &&
                GlitchTeamEffects.Allows(state.Players.Count, item.Outcome)).ToArray();
            var recipe = available[rng.NextInt(available.Length)];
            return new GlitchCombination(recipe.Trigger, recipe.Outcome);
        }

        var triggerPool = (rng.NextInt(4) != 0
                ? FrequentTriggers : Enum.GetValues<GlitchTrigger>())
            .Where(item => GlitchCharacterPool.Allows(owner.Character, item) &&
                GlitchTeamEffects.Allows(state.Players.Count, item)).ToArray();
        var trigger = triggerPool[rng.NextInt(triggerPool.Length)];
        var outcomes = Enum.GetValues<GlitchOutcome>()
            .Where(outcome => IsCompatible(trigger, outcome) &&
                (MysteriousMapRules.IsNestedMap(state.Map) ||
                 outcome is not (GlitchOutcome.Bomb or GlitchOutcome.BombBurst or GlitchOutcome.Reveal)) &&
                GlitchCharacterPool.Allows(owner.Character, outcome) &&
                GlitchTeamEffects.Allows(state.Players.Count, outcome))
            .ToArray();
        if (rng.NextInt(10) < 5)
        {
            var drawbacks = outcomes.Where(IsDrawback).ToArray();
            if (drawbacks.Length > 0) outcomes = drawbacks;
        }
        var result = outcomes[rng.NextInt(outcomes.Length)];
        return new GlitchCombination(trigger, result);
    }

    private static bool IsDrawback(GlitchOutcome outcome) => outcome is
        GlitchOutcome.LoseHp or GlitchOutcome.LoseGold or
        GlitchOutcome.AddCurse or GlitchOutcome.RemoveCard or
        GlitchOutcome.SelfWeak or GlitchOutcome.SelfVulnerable or
        GlitchOutcome.SelfFrail or GlitchOutcome.SelfPoison or
        GlitchOutcome.SelfStrengthLoss or GlitchOutcome.RandomSelfDebuff or
        GlitchOutcome.EnemyStrength or GlitchOutcome.SpawnEnemy or
        GlitchOutcome.EnemySoar or GlitchOutcome.EnemyIntangible or
        GlitchOutcome.RandomEnemyBuff or GlitchOutcome.TeammateDamage or
        GlitchOutcome.TeammateInjury or GlitchOutcome.TeammateEndTurn;

    private static bool IsCompatible(GlitchTrigger trigger, GlitchOutcome outcome)
    {
        if (outcome == GlitchOutcome.Victory)
            return false; // Only the explicitly rare roll can produce it.
        var inCombat = trigger is not GlitchTrigger.PotionUsed and
            not GlitchTrigger.PotionGained and not GlitchTrigger.GoldGained and
            not GlitchTrigger.RoomEntered and not GlitchTrigger.CombatVictory and
            not GlitchTrigger.CardRewardTaken and not GlitchTrigger.RelicRewardTaken and
            not GlitchTrigger.ShopPurchase and not GlitchTrigger.ShopEntered and
            not GlitchTrigger.CurseAddedToDeck and not GlitchTrigger.RestSiteEntered and
            not GlitchTrigger.RestHealed and
            not GlitchTrigger.RestSmithed and not GlitchTrigger.RelicObtained and
            not GlitchTrigger.LowHpCombatVictory and
            not GlitchTrigger.EliteCombatVictory and not GlitchTrigger.CardAddedToDeck;
        if (trigger == GlitchTrigger.CombatStarted)
            inCombat = true;
        var hasChoiceContext = trigger is GlitchTrigger.Hurt or
            GlitchTrigger.Damage or GlitchTrigger.Exhaust or
            GlitchTrigger.CardPlayed or GlitchTrigger.CardDrawn or
            GlitchTrigger.TurnStart or GlitchTrigger.BuffGained or
            GlitchTrigger.BombExploded or GlitchTrigger.CardDiscarded or
            GlitchTrigger.EnemyDied or GlitchTrigger.BlockBroken or
            GlitchTrigger.AttackPlayed or GlitchTrigger.SkillPlayed or
            GlitchTrigger.PowerPlayed or GlitchTrigger.Shuffle or
            GlitchTrigger.HandEmptied or GlitchTrigger.TurnEnded or
            GlitchTrigger.EnemyDebuffed or GlitchTrigger.SelfDebuffed or
            GlitchTrigger.EnemyBuffed or GlitchTrigger.GeneratedCardDrawn or
            GlitchTrigger.PlayedCardExhausted or GlitchTrigger.UnplayedCardExhausted or
            GlitchTrigger.StrengthGained or GlitchTrigger.PoisonApplied or
            GlitchTrigger.WeakApplied or GlitchTrigger.CombatStarted or
            GlitchTrigger.FirstAttackCombat or GlitchTrigger.ThirdCardTurn or
            GlitchTrigger.NoAttackTurnEnd or GlitchTrigger.NoBlockTurnEnd or
            GlitchTrigger.FirstHurtCombat or GlitchTrigger.EveryThirdTurn or
            GlitchTrigger.EveryTenthAttack or GlitchTrigger.ThirdAttackTurn or
            GlitchTrigger.SecondTurnCombat or GlitchTrigger.ThirdTurnCombat or
            GlitchTrigger.ThirdSkillTurn or GlitchTrigger.FewCardsTurnEnd or
            GlitchTrigger.SeventhTurnCombat or GlitchTrigger.FirstTurnCombat or
            GlitchTrigger.EnemyBlockBroken or GlitchTrigger.EmptyHandTurnEnd or
            GlitchTrigger.EliteCombatStarted or GlitchTrigger.BossCombatStarted or
            GlitchTrigger.RichTurnStart or GlitchTrigger.EveryFifthTurn or
            GlitchTrigger.FirstThreeTurnsCombat or
            GlitchTrigger.FirstTurnAfterDraw or GlitchTrigger.AfterTurnDraw or
            GlitchTrigger.FourthCardTurn or GlitchTrigger.TwoEnergyCardPlayed or
            GlitchTrigger.ThreeEnergyCardPlayed or GlitchTrigger.AllThreeTypesTurn or
            GlitchTrigger.FifthExhaustTurn or
            GlitchTrigger.TeammateHurt or GlitchTrigger.TeammateDamage or
            GlitchTrigger.TeammateBuffGained;
        if (!inCombat && outcome is GlitchOutcome.Block or GlitchOutcome.Energy or
            GlitchOutcome.Strength or GlitchOutcome.Dexterity or
            GlitchOutcome.Poison or GlitchOutcome.Blast or
            GlitchOutcome.CreativeAi or GlitchOutcome.CharacterCard or
            GlitchOutcome.Draw or GlitchOutcome.Victory or
            GlitchOutcome.SelfWeak or GlitchOutcome.SelfVulnerable or
            GlitchOutcome.SelfFrail or GlitchOutcome.SelfPoison or
            GlitchOutcome.EnemyWeak or GlitchOutcome.EnemyVulnerable or
            GlitchOutcome.EnemyFrail or GlitchOutcome.EnemyStrength or
            GlitchOutcome.Buffer or GlitchOutcome.Artifact or
            GlitchOutcome.Regen or GlitchOutcome.Thorns or
            GlitchOutcome.CharacterAttack or GlitchOutcome.CharacterSkill or
            GlitchOutcome.CharacterPower or GlitchOutcome.TripleDraw or
            GlitchOutcome.AnyCharacterCard or GlitchOutcome.IroncladCard or
            GlitchOutcome.SilentCard or GlitchOutcome.DefectCard or
            GlitchOutcome.RegentCard or GlitchOutcome.NecrobinderCard or
            GlitchOutcome.SpawnEnemy or
            GlitchOutcome.BigBlock or GlitchOutcome.BigBlast or
            GlitchOutcome.EnergyBurst or
            GlitchOutcome.MegaBlock or GlitchOutcome.TinyBlast or
            GlitchOutcome.DoubleCharacterCard or GlitchOutcome.EnemyHeavyPoison or
            GlitchOutcome.SelfStrengthLoss or GlitchOutcome.RandomBlessing or
            GlitchOutcome.FirstBlockDouble or GlitchOutcome.Intangible or
            GlitchOutcome.EnergyNextTurn or GlitchOutcome.DrawNextTurn or
            GlitchOutcome.Barricade or GlitchOutcome.Afterimage or
            GlitchOutcome.DoubleDamage or GlitchOutcome.Vigor or
            GlitchOutcome.Ritual or GlitchOutcome.BlockNextTurn or
            GlitchOutcome.RandomEnemyDebuff or GlitchOutcome.RandomSelfDebuff or
            GlitchOutcome.EnemySoar or GlitchOutcome.EnemyIntangible or
            GlitchOutcome.RandomEnemyBuff or GlitchOutcome.TeammateDamage or
            GlitchOutcome.TeammateInjury or GlitchOutcome.TeammateCard or
            GlitchOutcome.TeammateStrength or GlitchOutcome.TeammateBlock or
            GlitchOutcome.TeammateHeal or GlitchOutcome.TeammateEndTurn or
            GlitchOutcome.TeammateRandomBuff)
            return false;
        if (!hasChoiceContext && outcome is GlitchOutcome.Strength or
            GlitchOutcome.Dexterity or GlitchOutcome.Poison or
            GlitchOutcome.Blast or GlitchOutcome.Draw or
            GlitchOutcome.SelfWeak or GlitchOutcome.SelfVulnerable or
            GlitchOutcome.SelfFrail or GlitchOutcome.SelfPoison or
            GlitchOutcome.EnemyWeak or GlitchOutcome.EnemyVulnerable or
            GlitchOutcome.EnemyFrail or GlitchOutcome.EnemyStrength or
            GlitchOutcome.Buffer or GlitchOutcome.Artifact or
            GlitchOutcome.Regen or GlitchOutcome.Thorns or
            GlitchOutcome.TripleDraw or GlitchOutcome.BigBlast or
            GlitchOutcome.TinyBlast or GlitchOutcome.EnemyHeavyPoison or
            GlitchOutcome.SelfStrengthLoss or GlitchOutcome.RandomBlessing or
            GlitchOutcome.Intangible or GlitchOutcome.EnergyNextTurn or
            GlitchOutcome.DrawNextTurn or GlitchOutcome.Barricade or
            GlitchOutcome.Afterimage or GlitchOutcome.DoubleDamage or
            GlitchOutcome.Vigor or GlitchOutcome.Ritual or
            GlitchOutcome.BlockNextTurn or GlitchOutcome.RandomEnemyDebuff or
            GlitchOutcome.RandomSelfDebuff or GlitchOutcome.EnemySoar or
            GlitchOutcome.EnemyIntangible or GlitchOutcome.RandomEnemyBuff or
            GlitchOutcome.TeammateDamage or GlitchOutcome.TeammateStrength or
            GlitchOutcome.TeammateRandomBuff)
            return false;
        return true;
    }

    private IEnumerable<GlitchCombination> ParseCombinations()
    {
        var active = new List<GlitchCombination>();
        foreach (var encoded in ComboCodes.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = encoded.Split(':');
            if (parts.Length is not (2 or 3) ||
                !int.TryParse(parts[0], out var trigger) ||
                !int.TryParse(parts[1], out var outcome) ||
                !Enum.IsDefined((GlitchTrigger)trigger) ||
                !Enum.IsDefined((GlitchOutcome)outcome) ||
                (IsMutable && Owner != null &&
                    (!GlitchCharacterPool.Allows(Owner.Character, (GlitchTrigger)trigger) ||
                     !GlitchCharacterPool.Allows(Owner.Character, (GlitchOutcome)outcome) ||
                     !GlitchTeamEffects.Allows(Owner.RunState.Players.Count, (GlitchTrigger)trigger) ||
                     !GlitchTeamEffects.Allows(Owner.RunState.Players.Count, (GlitchOutcome)outcome))))
                continue;
            // Old saves had a third chance field. It is intentionally ignored.
            var next = new GlitchCombination((GlitchTrigger)trigger, (GlitchOutcome)outcome);
            if (!CanAddLink(active, [], $"core.t{trigger}", $"core.o{outcome}")) continue;
            active.Add(next);
            yield return next;
        }
    }

    private IEnumerable<ExternalGlitchCombination> ParseExternalCombinations()
    {
        var core = ParseCombinations().ToArray();
        var active = new List<ExternalGlitchCombination>();
        foreach (var encoded in ExternalComboCodes.Split('|', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = encoded.Split('>');
            if (parts.Length is not (2 or 3) ||
                (IsMutable && Owner != null &&
                    (!GlitchCharacterPool.Allows(Owner.Character, parts[0]) ||
                     !GlitchCharacterPool.Allows(Owner.Character, parts[1]))))
                continue;
            if (!CanAddLink(core, active, parts[0], parts[1])) continue;
            var next = new ExternalGlitchCombination(parts[0], parts[1]);
            active.Add(next);
            yield return next;
        }
    }

    private IEnumerable<IHoverTip> GetCombinationHoverTips()
    {
        foreach (var combo in ParseCombinations())
        {
            var trigger = new LocString("relics",
                $"MYSTERIOUS_GLITCH_TRIGGER_{(int)combo.Trigger}.label");
            var outcome = new LocString("relics",
                $"MYSTERIOUS_GLITCH_OUTCOME_{(int)combo.Outcome}.label");
            yield return new HoverTip(
                new LocString("relics", "MYSTERIOUS_GLITCHED_RELIC.title"),
                $"{trigger.GetFormattedText()} → {outcome.GetFormattedText()}",
                null);
        }
        foreach (var combo in ParseExternalCombinations())
        {
            var triggerLabel = GetTriggerLabel(combo.TriggerId);
            var outcomeLabel = GetOutcomeLabel(combo.OutcomeId);
            if (triggerLabel == null || outcomeLabel == null)
                continue;
            yield return new HoverTip(
                new LocString("relics", "MYSTERIOUS_GLITCHED_RELIC.title"),
                $"{triggerLabel} → {outcomeLabel}", null);
        }
    }

    internal string GetVisibleCombinationDescription()
    {
        if (!MysteriousMapSettings.RevealGlitchEffects)
            return string.Empty;
        var note = new LocString("relics", "MYSTERIOUS_GLITCH_POOL_NOTE.label")
            .GetFormattedText();
        return note + "\n" + string.Join("\n", GetCombinationHoverTips()
            .OfType<HoverTip>()
            .Select(tip => tip.Description));
    }

    private static string? GetTriggerLabel(string id)
    {
        if (GlitchInteractionRegistry.TryGetTrigger(id, out var external))
            return external.Label();
        if (id.StartsWith("core.t", StringComparison.Ordinal) &&
            int.TryParse(id.AsSpan(6), out var number) &&
            Enum.IsDefined((GlitchTrigger)number))
            return new LocString("relics", $"MYSTERIOUS_GLITCH_TRIGGER_{number}.label").GetFormattedText();
        return null;
    }

    private static string? GetOutcomeLabel(string id)
    {
        if (GlitchInteractionRegistry.TryGetOutcome(id, out var external))
            return external.Label();
        if (id.StartsWith("core.o", StringComparison.Ordinal) &&
            int.TryParse(id.AsSpan(6), out var number) &&
            Enum.IsDefined((GlitchOutcome)number))
            return new LocString("relics", $"MYSTERIOUS_GLITCH_OUTCOME_{number}.label").GetFormattedText();
        return null;
    }

    internal async Task TriggerExternal(string triggerId,
        PlayerChoiceContext? choiceContext, Creature? eventEnemy)
    {
        if (Owner?.RunState is not RunState state ||
            !GlitchInteractionRegistry.TryGetTrigger(triggerId, out _) ||
            !_cascade.TryEnter(Owner.Creature.CombatState != null))
            return;
        try
        {
            foreach (var combo in ParseExternalCombinations()
                         .Where(item => item.TriggerId == triggerId))
            {
                if (combo.OutcomeId.StartsWith("core.o", StringComparison.Ordinal) &&
                    int.TryParse(combo.OutcomeId.AsSpan(6), out var number) &&
                    Enum.IsDefined((GlitchOutcome)number))
                {
                    var outcome = (GlitchOutcome)number;
                    if (_cascade.TrySpend(outcome, Owner.Creature.CombatState != null))
                        await ResolveSafely(() => ResolveCombination(
                            outcome, choiceContext, eventEnemy, state), combo.ToString());
                }
                else if (GlitchInteractionRegistry.TryGetOutcome(combo.OutcomeId, out var external) &&
                         (!external.RequiresCombat || Owner.Creature.CombatState != null) &&
                         (!external.RequiresChoiceContext || choiceContext != null) &&
                         _cascade.TrySpendExternal(Owner.Creature.CombatState != null))
                    await ResolveSafely(() => external.Apply(
                        new(this, Owner, state, choiceContext, eventEnemy)), combo.ToString());
            }
        }
        finally
        {
            _cascade.Exit();
        }
    }

    private async Task TriggerCombination(
        GlitchTrigger trigger, PlayerChoiceContext? choiceContext = null,
        Creature? eventEnemy = null)
    {
        if (Owner?.RunState is not RunState state ||
            !_cascade.TryEnter(Owner.Creature.CombatState != null))
            return;
        try
        {
            foreach (var combo in ParseCombinations().Where(item => item.Trigger == trigger))
                if (_cascade.TrySpend(combo.Outcome, Owner.Creature.CombatState != null))
                    await ResolveSafely(() => ResolveCombination(
                        combo.Outcome, choiceContext, eventEnemy, state), combo.ToString());
            foreach (var combo in ParseExternalCombinations()
                         .Where(item => item.TriggerId == $"core.t{(int)trigger}"))
            {
                if (!GlitchInteractionRegistry.TryGetOutcome(combo.OutcomeId, out var outcome) ||
                    (outcome.RequiresCombat && Owner.Creature.CombatState == null) ||
                    (outcome.RequiresChoiceContext && choiceContext == null) ||
                    !_cascade.TrySpendExternal(Owner.Creature.CombatState != null))
                    continue;
                await ResolveSafely(() => outcome.Apply(
                    new(this, Owner, state, choiceContext, eventEnemy)), combo.ToString());
            }
            foreach (var alias in GlitchInteractionRegistry.AliasesFor(trigger))
                await TriggerExternal(alias.Id, choiceContext, eventEnemy);
        }
        finally
        {
            _cascade.Exit();
        }
    }

    private static async Task ResolveSafely(Func<Task> effect, string combination)
    {
        try
        {
            await effect();
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // A malformed random effect must not tear down the game's turn loop.
            Log.Error($"[MysteriousMap] Skipped failed glitched effect {combination}: {exception}");
        }
    }

    private async Task ResolveCombination(
        GlitchOutcome outcome, PlayerChoiceContext? choiceContext,
        Creature? eventEnemy, RunState state)
    {
        if (Owner == null)
            return;
        var combat = Owner.Creature.CombatState;
        switch (outcome)
        {
            case GlitchOutcome.Block when combat != null:
                await CreatureCmd.GainBlock(Owner.Creature, 8m, ValueProp.Unpowered, null, false);
                break;
            case GlitchOutcome.Heal:
                await CreatureCmd.Heal(Owner.Creature, 5m, false);
                break;
            case GlitchOutcome.Energy when combat != null:
                await PlayerCmd.GainEnergy(1m, Owner);
                break;
            case GlitchOutcome.Strength when choiceContext != null && combat != null:
                await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature,
                    1m, Owner.Creature, null, false);
                break;
            case GlitchOutcome.Dexterity when choiceContext != null && combat != null:
                await PowerCmd.Apply<DexterityPower>(choiceContext, Owner.Creature,
                    1m, Owner.Creature, null, false);
                break;
            case GlitchOutcome.Poison when choiceContext != null && combat != null:
            {
                var target = eventEnemy != null && combat.HittableEnemies.Contains(eventEnemy)
                    ? eventEnemy
                    : combat.HittableEnemies.OrderBy(enemy => enemy.CombatId).FirstOrDefault();
                if (target != null)
                    await PowerCmd.Apply<PoisonPower>(choiceContext, target,
                        2m, Owner.Creature, null, false);
                break;
            }
            case GlitchOutcome.Blast when choiceContext != null && combat != null:
                await CreatureCmd.Damage(choiceContext, combat.HittableEnemies.ToArray(),
                    12m, ValueProp.Unpowered, Owner.Creature);
                break;
            case GlitchOutcome.CreativeAi when combat != null:
                if (ModelDb.Card<MegaCrit.Sts2.Core.Models.Cards.CreativeAi>().Pool == Owner.Character.CardPool)
                    await AddCreativeAi(Owner);
                else
                    await GenerateCharacterCard(GlitchOutcome.CharacterPower, state);
                break;
            case GlitchOutcome.CharacterCard when combat != null:
            case GlitchOutcome.CharacterAttack when combat != null:
            case GlitchOutcome.CharacterSkill when combat != null:
            case GlitchOutcome.CharacterPower when combat != null:
            case GlitchOutcome.AnyCharacterCard when combat != null:
            case GlitchOutcome.IroncladCard when combat != null:
            case GlitchOutcome.SilentCard when combat != null:
            case GlitchOutcome.DefectCard when combat != null:
            case GlitchOutcome.RegentCard when combat != null:
            case GlitchOutcome.NecrobinderCard when combat != null:
                await GenerateCharacterCard(outcome, state);
                break;
            case GlitchOutcome.LoseHp:
                await CreatureCmd.SetCurrentHp(Owner.Creature,
                    Math.Max(1, Owner.Creature.CurrentHp - 5));
                break;
            case GlitchOutcome.LoseGold:
                await PlayerCmd.LoseGold(Math.Min(15, Owner.Gold), Owner, GoldLossType.Lost);
                break;
            case GlitchOutcome.Relic when LastGrantedRelicRoomCount != state.CurrentRoomCount:
            {
                LastGrantedRelicRoomCount = state.CurrentRoomCount;
                try
                {
                    var rarity = new[] { RelicRarity.Common, RelicRarity.Uncommon,
                        RelicRarity.Rare }[state.Rng.Niche.NextInt(3)];
                    var relic = RelicFactory.PullNextRelicFromFront(Owner, rarity,
                        candidate => candidate is not MysteriousDataminer and
                            not MysteriousGlitchedRelic);
                    if (relic != null)
                        await RelicCmd.Obtain(relic.ToMutable(), Owner, -1);
                }
                catch
                {
                    LastGrantedRelicRoomCount = -1;
                    throw;
                }
                break;
            }
            case GlitchOutcome.AddCurse when LastInjuryRoomCount != state.CurrentRoomCount:
                LastInjuryRoomCount = state.CurrentRoomCount;
                try
                {
                    if (combat != null && combat.HittableEnemies.Count > 0)
                        await GlitchCardGeneration.AddToDrawPile(ModelDb.Card<Injury>(), Owner);
                    else
                        await CardPileCmd.AddCurseToDeck<Injury>(Owner);
                }
                catch
                {
                    LastInjuryRoomCount = -1;
                    throw;
                }
                break;
            case GlitchOutcome.RemoveCard when LastDeckMutationRoomCount != state.CurrentRoomCount:
            {
                var cards = Owner.Deck.Cards.Where(card => card.Type != CardType.Curse).ToArray();
                if (cards.Length > 0)
                {
                    // The room limit is intentional, but the removed card must
                    // be shown so the player can tell which effect resolved.
                    await CardPileCmd.RemoveFromDeck(
                        cards[state.Rng.Niche.NextInt(cards.Length)], true);
                    LastDeckMutationRoomCount = state.CurrentRoomCount;
                }
                break;
            }
            case GlitchOutcome.SpawnEnemy when choiceContext != null && combat != null:
            {
                // Dynamic CreatureCmd.Add leaves some encounters without a UI
                // slot or a rolled move. That can permanently kill the turn
                // loop even when the add exception is caught. Retain existing
                // saved combinations as a safe hostile effect instead.
                var target = combat.HittableEnemies.OrderBy(enemy => enemy.CombatId).FirstOrDefault();
                if (target != null)
                    await PowerCmd.Apply<StrengthPower>(choiceContext, target,
                        2m, Owner.Creature, null, false);
                break;
            }
            case GlitchOutcome.Draw when choiceContext != null && combat != null:
                await CardPileCmd.Draw(choiceContext, 1m, Owner, false);
                break;
            case GlitchOutcome.MaxHp when LastMaxHpRoomCount != state.CurrentRoomCount:
                LastMaxHpRoomCount = state.CurrentRoomCount;
                await CreatureCmd.GainMaxHp(Owner.Creature, 1m);
                break;
            case GlitchOutcome.Gold:
                await PlayerCmd.GainGold(15m, Owner, false);
                break;
            case GlitchOutcome.Bomb:
                if (!MysteriousMapRules.IsNestedMap(state.Map))
                {
                    await PlayerCmd.GainGold(15m, Owner, false);
                    break;
                }
                if (ShopInventoryMultiplayerSynchronizer.ShouldApplyOwnerSide(Owner))
                {
                    HiddenRoomState.AddGlitchBomb(state);
                    if (state.CurrentMapCoord is { } bombSource)
                        ShopInventoryMultiplayerSynchronizer.PublishBombAction(
                            state, Owner, 4, bombSource, amount: 1);
                }
                break;
            case GlitchOutcome.Reveal when state.VisitedMapCoords.Count > 0 &&
                MysteriousMapRules.IsNestedMap(state.Map):
                HiddenRoomState.RevealAdjacent(state, state.VisitedMapCoords[^1]);
                if (state.CurrentMapCoord is { } revealSource)
                    ShopInventoryMultiplayerSynchronizer.PublishBombAction(
                        state, Owner, 5, revealSource);
                break;
            case GlitchOutcome.Victory when combat != null && combat.HittableEnemies.Count > 0:
                await CreatureCmd.Kill(combat.HittableEnemies.ToArray(), false);
                break;
            case GlitchOutcome.BigBlock when combat != null:
                await CreatureCmd.GainBlock(Owner.Creature, 20m, ValueProp.Unpowered, null, false);
                break;
            case GlitchOutcome.TripleDraw when choiceContext != null && combat != null:
                await CardPileCmd.Draw(choiceContext, 3m, Owner, false);
                break;
            case GlitchOutcome.BigBlast when choiceContext != null && combat != null:
                await CreatureCmd.Damage(choiceContext, combat.HittableEnemies.ToArray(),
                    30m, ValueProp.Unpowered, Owner.Creature);
                break;
            case GlitchOutcome.Potion:
            {
                var potions = ModelDb.AllPotions
                    .Where(potion => potion.Pool == Owner.Character.PotionPool)
                    .ToArray();
                if (potions.Length > 0 && Owner.HasOpenPotionSlots)
                {
                    var result = await PotionCmd.TryToProcure(
                        potions[state.Rng.Niche.NextInt(potions.Length)].ToMutable(), Owner, -1);
                    if (!result.success)
                        await ResolveCombination(GlitchOutcome.RandomPotionEffect, choiceContext, eventEnemy, state);
                }
                else
                    await ResolveCombination(GlitchOutcome.RandomPotionEffect, choiceContext, eventEnemy, state);
                break;
            }
            case GlitchOutcome.RandomPotionEffect:
            {
                // Potion procurement also fires outside combat, without a choice
                // context. A full belt must still resolve to a usable effect.
                var pool = choiceContext != null && combat != null
                    ? new[] { GlitchOutcome.Heal, GlitchOutcome.Block,
                        GlitchOutcome.Energy, GlitchOutcome.Strength,
                        GlitchOutcome.Dexterity,
                        Owner.Character is MegaCrit.Sts2.Core.Models.Characters.Silent
                            ? GlitchOutcome.Poison : GlitchOutcome.Artifact }
                    : combat != null
                        ? new[] { GlitchOutcome.Heal, GlitchOutcome.Block, GlitchOutcome.Energy }
                        : new[] { GlitchOutcome.Heal };
                var effect = pool[state.Rng.Niche.NextInt(pool.Length)];
                await ResolveCombination(effect, choiceContext, eventEnemy, state);
                break;
            }
            case GlitchOutcome.SelfWeak when choiceContext != null && combat != null:
                await PowerCmd.Apply<WeakPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, null, false);
                break;
            case GlitchOutcome.SelfVulnerable when choiceContext != null && combat != null:
                await PowerCmd.Apply<VulnerablePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, null, false);
                break;
            case GlitchOutcome.SelfFrail when choiceContext != null && combat != null:
                await PowerCmd.Apply<FrailPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, null, false);
                break;
            case GlitchOutcome.SelfPoison when choiceContext != null && combat != null:
                await PowerCmd.Apply<PoisonPower>(choiceContext, Owner.Creature, 2m, Owner.Creature, null, false);
                break;
            case GlitchOutcome.EnemyWeak when choiceContext != null && combat != null:
            case GlitchOutcome.EnemyVulnerable when choiceContext != null && combat != null:
            case GlitchOutcome.EnemyFrail when choiceContext != null && combat != null:
            case GlitchOutcome.EnemyStrength when choiceContext != null && combat != null:
            {
                var target = eventEnemy != null && combat.HittableEnemies.Contains(eventEnemy)
                    ? eventEnemy : combat.HittableEnemies.OrderBy(enemy => enemy.CombatId).FirstOrDefault();
                if (target == null) break;
                if (outcome == GlitchOutcome.EnemyWeak)
                    await PowerCmd.Apply<WeakPower>(choiceContext, target, 1m, Owner.Creature, null, false);
                else if (outcome == GlitchOutcome.EnemyVulnerable)
                    await PowerCmd.Apply<VulnerablePower>(choiceContext, target, 1m, Owner.Creature, null, false);
                else if (outcome == GlitchOutcome.EnemyFrail)
                    await PowerCmd.Apply<FrailPower>(choiceContext, target, 1m, Owner.Creature, null, false);
                else
                    await PowerCmd.Apply<StrengthPower>(choiceContext, target, 1m, Owner.Creature, null, false);
                break;
            }
            case GlitchOutcome.EnemySoar when choiceContext != null && combat != null:
            case GlitchOutcome.EnemyIntangible when choiceContext != null && combat != null:
            case GlitchOutcome.RandomEnemyBuff when choiceContext != null && combat != null:
            {
                var target = eventEnemy != null && combat.HittableEnemies.Contains(eventEnemy)
                    ? eventEnemy : combat.HittableEnemies.OrderBy(enemy => enemy.CombatId).FirstOrDefault();
                if (target == null) break;
                if (outcome == GlitchOutcome.EnemySoar)
                    await PowerCmd.Apply<SoarPower>(choiceContext, target, 1m,
                        Owner.Creature, null, false);
                else if (outcome == GlitchOutcome.EnemyIntangible)
                    await PowerCmd.Apply<IntangiblePower>(choiceContext, target, 1m,
                        Owner.Creature, null, false);
                else
                    await GlitchRandomEnemyBuffs.Apply(choiceContext, Owner, state, target);
                break;
            }
            case GlitchOutcome.Buffer when choiceContext != null && combat != null:
                await PowerCmd.Apply<BufferPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, null, false);
                break;
            case GlitchOutcome.Artifact when choiceContext != null && combat != null:
                await PowerCmd.Apply<ArtifactPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, null, false);
                break;
            case GlitchOutcome.Regen when choiceContext != null && combat != null:
                await PowerCmd.Apply<RegenPower>(choiceContext, Owner.Creature, 2m, Owner.Creature, null, false);
                break;
            case GlitchOutcome.Thorns when choiceContext != null && combat != null:
                await PowerCmd.Apply<ThornsPower>(choiceContext, Owner.Creature, 2m, Owner.Creature, null, false);
                break;
            case GlitchOutcome.BigHeal:
                await CreatureCmd.Heal(Owner.Creature, 15m, false);
                break;
            case GlitchOutcome.GoldBurst:
                await PlayerCmd.GainGold(50m, Owner, false);
                break;
            case GlitchOutcome.BombBurst:
                if (!MysteriousMapRules.IsNestedMap(state.Map))
                {
                    await PlayerCmd.GainGold(45m, Owner, false);
                    break;
                }
                if (ShopInventoryMultiplayerSynchronizer.ShouldApplyOwnerSide(Owner))
                {
                    for (var i = 0; i < 3; i++)
                        HiddenRoomState.AddGlitchBomb(state);
                    if (state.CurrentMapCoord is { } burstSource)
                        ShopInventoryMultiplayerSynchronizer.PublishBombAction(
                            state, Owner, 4, burstSource, amount: 3);
                }
                break;
            case GlitchOutcome.EnergyBurst when combat != null:
                await PlayerCmd.GainEnergy(2m, Owner);
                break;
            case GlitchOutcome.MegaBlock when combat != null:
                await CreatureCmd.GainBlock(Owner.Creature, 40m, ValueProp.Unpowered, null, false);
                break;
            case GlitchOutcome.TinyBlast when choiceContext != null && combat != null:
                await CreatureCmd.Damage(choiceContext, combat.HittableEnemies.ToArray(),
                    5m, ValueProp.Unpowered, Owner.Creature);
                break;
            case GlitchOutcome.DoubleCharacterCard when combat != null:
                await GenerateCharacterCard(GlitchOutcome.CharacterCard, state);
                await GenerateCharacterCard(GlitchOutcome.CharacterCard, state);
                break;
            case GlitchOutcome.EnemyHeavyPoison when choiceContext != null && combat != null:
            {
                var target = eventEnemy != null && combat.HittableEnemies.Contains(eventEnemy)
                    ? eventEnemy : combat.HittableEnemies.OrderBy(enemy => enemy.CombatId).FirstOrDefault();
                if (target != null)
                    await PowerCmd.Apply<PoisonPower>(choiceContext, target, 8m, Owner.Creature, null, false);
                break;
            }
            case GlitchOutcome.SelfStrengthLoss when choiceContext != null && combat != null:
                await PowerCmd.Apply<StrengthPower>(choiceContext, Owner.Creature, -1m, Owner.Creature, null, false);
                break;
            case GlitchOutcome.RandomBlessing when choiceContext != null && combat != null:
            {
                await GlitchRandomBuffs.Apply(choiceContext, Owner, state);
                break;
            }
            case GlitchOutcome.RandomEnemyDebuff when choiceContext != null && combat != null:
            {
                var target = eventEnemy != null && combat.HittableEnemies.Contains(eventEnemy)
                    ? eventEnemy : combat.HittableEnemies.OrderBy(enemy => enemy.CombatId).FirstOrDefault();
                if (target != null)
                    await GlitchRandomDebuffs.ApplyToEnemy(choiceContext, Owner, state, target);
                break;
            }
            case GlitchOutcome.RandomSelfDebuff when choiceContext != null && combat != null:
                await GlitchRandomDebuffs.ApplyToSelf(choiceContext, Owner, state);
                break;
            case GlitchOutcome.FirstBlockDouble when combat != null && !_firstBlockDoubleUsed:
                _firstBlockDoubleArmed = true;
                break;
            case GlitchOutcome.Intangible when choiceContext != null && combat != null:
                await PowerCmd.Apply<IntangiblePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, null, false);
                break;
            case GlitchOutcome.EnergyNextTurn when choiceContext != null && combat != null:
                await PowerCmd.Apply<EnergyNextTurnPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, null, false);
                break;
            case GlitchOutcome.DrawNextTurn when choiceContext != null && combat != null:
                await PowerCmd.Apply<DrawCardsNextTurnPower>(choiceContext, Owner.Creature, 2m, Owner.Creature, null, false);
                break;
            case GlitchOutcome.Barricade when choiceContext != null && combat != null:
                await PowerCmd.Apply<BarricadePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, null, false);
                break;
            case GlitchOutcome.Afterimage when choiceContext != null && combat != null:
                await PowerCmd.Apply<AfterimagePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, null, false);
                break;
            case GlitchOutcome.DoubleDamage when choiceContext != null && combat != null:
                await PowerCmd.Apply<DoubleDamagePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, null, false);
                break;
            case GlitchOutcome.Vigor when choiceContext != null && combat != null:
                await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature, 4m, Owner.Creature, null, false);
                break;
            case GlitchOutcome.Ritual when choiceContext != null && combat != null:
                await PowerCmd.Apply<RitualPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, null, false);
                break;
            case GlitchOutcome.BlockNextTurn when choiceContext != null && combat != null:
                await PowerCmd.Apply<BlockNextTurnPower>(choiceContext, Owner.Creature, 8m, Owner.Creature, null, false);
                break;
            case GlitchOutcome.TeammateDamage or GlitchOutcome.TeammateInjury or
                GlitchOutcome.TeammateCard or GlitchOutcome.TeammateStrength or
                GlitchOutcome.TeammateBlock or GlitchOutcome.TeammateHeal or
                GlitchOutcome.TeammateEndTurn or GlitchOutcome.TeammateRandomBuff:
                await GlitchTeamEffects.Apply(outcome, Owner, state, choiceContext);
                break;
        }
    }

    private async Task GenerateCharacterCard(GlitchOutcome outcome, RunState state)
    {
        if (Owner == null) return;
        var type = outcome switch
        {
            GlitchOutcome.CharacterAttack => CardType.Attack,
            GlitchOutcome.CharacterSkill => CardType.Skill,
            GlitchOutcome.CharacterPower => CardType.Power,
            _ => CardType.None
        };
        var pools = outcome switch
        {
            GlitchOutcome.AnyCharacterCard => new[]
            {
                ModelDb.Character<Ironclad>().CardPool,
                ModelDb.Character<Silent>().CardPool,
                ModelDb.Character<Defect>().CardPool,
                ModelDb.Character<Regent>().CardPool,
                ModelDb.Character<Necrobinder>().CardPool
            },
            GlitchOutcome.IroncladCard => [ModelDb.Character<Ironclad>().CardPool],
            GlitchOutcome.SilentCard => [ModelDb.Character<Silent>().CardPool],
            GlitchOutcome.DefectCard => [ModelDb.Character<Defect>().CardPool],
            GlitchOutcome.RegentCard => [ModelDb.Character<Regent>().CardPool],
            GlitchOutcome.NecrobinderCard => [ModelDb.Character<Necrobinder>().CardPool],
            _ => [Owner.Character.CardPool]
        };
        var choices = ModelDb.AllCards
            .Where(card => pools.Contains(card.Pool) &&
                           card.Rarity is CardRarity.Common or CardRarity.Uncommon &&
                           (type == CardType.None || card.Type == type)).ToArray();
        if (choices.Length == 0) return;
        await GlitchCardGeneration.AddToHand(
            choices[state.Rng.Niche.NextInt(choices.Length)], Owner);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Player != Owner) return;
        _cardsPlayedThisTurn++;
        await TriggerCombination(GlitchTrigger.CardPlayed, choiceContext);
        if (_cardsPlayedThisTurn == 3)
            await TriggerCombination(GlitchTrigger.ThirdCardTurn, choiceContext);
        if (_cardsPlayedThisTurn == 4)
            await TriggerCombination(GlitchTrigger.FourthCardTurn, choiceContext);
        if (cardPlay.Resources.EnergyValue >= 2)
            await TriggerCombination(GlitchTrigger.TwoEnergyCardPlayed, choiceContext);
        if (cardPlay.Resources.EnergyValue >= 3)
            await TriggerCombination(GlitchTrigger.ThreeEnergyCardPlayed, choiceContext);
        var trigger = cardPlay.Card.Type switch
        {
            CardType.Attack => GlitchTrigger.AttackPlayed,
            CardType.Skill => GlitchTrigger.SkillPlayed,
            CardType.Power => GlitchTrigger.PowerPlayed,
            _ => (GlitchTrigger)0
        };
        if (trigger != 0) await TriggerCombination(trigger, choiceContext);
        if (trigger == GlitchTrigger.AttackPlayed)
        {
            _attacksPlayedThisTurn++;
            TenAttackCounter = (TenAttackCounter + 1) % 10;
            if (TenAttackCounter == 0)
                await TriggerCombination(GlitchTrigger.EveryTenthAttack, choiceContext);
            if (_attacksPlayedThisTurn % 3 == 0)
                await TriggerCombination(GlitchTrigger.ThirdAttackTurn, choiceContext);
            if (!_firstAttackSeenCombat)
            {
                _firstAttackSeenCombat = true;
                await TriggerCombination(GlitchTrigger.FirstAttackCombat, choiceContext);
            }
        }
        else if (trigger == GlitchTrigger.SkillPlayed)
        {
            _skillsPlayedThisTurn++;
            if (_skillsPlayedThisTurn % 3 == 0)
                await TriggerCombination(GlitchTrigger.ThirdSkillTurn, choiceContext);
        }
        else if (trigger == GlitchTrigger.PowerPlayed)
            _powersPlayedThisTurn++;
        if (!_allThreeTypesTriggeredThisTurn && _attacksPlayedThisTurn > 0 &&
            _skillsPlayedThisTurn > 0 && _powersPlayedThisTurn > 0)
        {
            _allThreeTypesTriggeredThisTurn = true;
            await TriggerCombination(GlitchTrigger.AllThreeTypesTurn, choiceContext);
        }
    }

    public override async Task AfterCardDrawn(
        PlayerChoiceContext choiceContext, CardModel card, bool wasGenerated)
    {
        if (card.Owner != Owner) return;
        await TriggerCombination(GlitchTrigger.CardDrawn, choiceContext);
        if (wasGenerated)
            await TriggerCombination(GlitchTrigger.GeneratedCardDrawn, choiceContext);
    }

    public override Task AfterCardDiscarded(
        PlayerChoiceContext choiceContext, CardModel card) =>
        card.Owner == Owner
            ? TriggerCombination(GlitchTrigger.CardDiscarded, choiceContext)
            : Task.CompletedTask;

    public override async Task AfterCardChangedPiles(
        CardModel card, PileType fromPile, AbstractModel? source)
    {
        if (card.Owner != Owner || card.Pile?.Type != PileType.Deck ||
            fromPile == PileType.Deck) return;
        await TriggerCombination(GlitchTrigger.CardAddedToDeck);
        if (card.Type == CardType.Curse)
            await TriggerCombination(GlitchTrigger.CurseAddedToDeck);
    }

    public override Task AfterDeath(
        PlayerChoiceContext choiceContext, Creature creature,
        bool wasRemovalPrevented, float deathAnimLength) =>
        Owner?.Creature.CombatState?.Enemies.Contains(creature) == true &&
        !wasRemovalPrevented
            ? TriggerCombination(GlitchTrigger.EnemyDied, choiceContext)
            : Task.CompletedTask;

    public override Task AfterBlockBroken(
        PlayerChoiceContext choiceContext, Creature target, Creature? breaker)
    {
        if (target == Owner?.Creature)
            return TriggerCombination(GlitchTrigger.BlockBroken, choiceContext, breaker);
        if (Owner is { } owner && breaker == owner.Creature &&
            owner.Creature.CombatState?.Enemies.Contains(target) == true)
            return TriggerCombination(GlitchTrigger.EnemyBlockBroken, choiceContext, target);
        return Task.CompletedTask;
    }

    public override Task AfterEnergySpent(CardModel card, int amount) =>
        card.Owner == Owner && amount > 0
            ? TriggerCombination(GlitchTrigger.EnergySpent)
            : Task.CompletedTask;

    public override Task AfterStarsSpent(int amount, Player spender) =>
        spender == Owner && amount > 0 && Owner.Creature.CombatState != null
            ? TriggerCombination(GlitchTrigger.StarsSpent)
            : Task.CompletedTask;

    public override Task AfterBlockCleared(Creature creature) =>
        creature == Owner?.Creature && Owner.Creature.CombatState != null
            ? TriggerCombination(GlitchTrigger.BlockCleared)
            : Task.CompletedTask;

    public override async Task AfterBlockGained(
        Creature target, decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (Owner != null && amount > 0 &&
            GlitchTeamEffects.IsLivingTeammate(Owner, target))
            await TriggerCombination(GlitchTrigger.TeammateBlockGained);
        if (target != Owner?.Creature || amount <= 0) return;
        if (!_firstBlockSeenCombat)
        {
            _firstBlockSeenCombat = true;
            await TriggerCombination(GlitchTrigger.FirstBlockCombat);
        }
        if (_firstBlockDoubleArmed && !_firstBlockDoubleUsed)
        {
            _firstBlockDoubleArmed = false;
            _firstBlockDoubleUsed = true;
            await CreatureCmd.GainBlock(Owner.Creature, amount, ValueProp.Unpowered, null, false);
        }
        await TriggerCombination(GlitchTrigger.BlockGained);
        await TriggerCombination(cardSource?.Owner == Owner
            ? GlitchTrigger.CardBlockGained : GlitchTrigger.OtherBlockGained);
    }

    public override async Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (potion.Owner != Owner) return;
        if (Owner.Creature.CombatState != null && !_firstPotionSeenCombat)
        {
            _firstPotionSeenCombat = true;
            await TriggerCombination(GlitchTrigger.FirstPotionCombat);
        }
        await TriggerCombination(GlitchTrigger.PotionUsed);
        if (target == Owner?.Creature)
            await TriggerCombination(GlitchTrigger.SelfPotionUsed);
        else if (target != null && Owner?.Creature.CombatState?.Enemies.Contains(target) == true)
            await TriggerCombination(GlitchTrigger.EnemyPotionUsed);
    }

    public override Task AfterPotionProcured(PotionModel potion) =>
        potion.Owner == Owner
            ? TriggerCombination(GlitchTrigger.PotionGained)
            : Task.CompletedTask;

    public override Task AfterItemPurchased(Player player, MerchantEntry entry, int cost) =>
        player == Owner && cost > 0
            ? TriggerCombination(GlitchTrigger.ShopPurchase)
            : Task.CompletedTask;

    public override async Task AfterRewardTaken(Player player, Reward reward)
    {
        if (player != Owner) return;
        if (reward is CardReward or SpecialCardReward)
            await TriggerCombination(GlitchTrigger.CardRewardTaken);
        else if (reward is RelicReward)
            await TriggerCombination(GlitchTrigger.RelicRewardTaken);
    }

    public override Task AfterRestSiteHeal(Player player, bool wasFullHeal) =>
        player == Owner ? TriggerCombination(GlitchTrigger.RestHealed) : Task.CompletedTask;

    public override Task AfterRestSiteSmith(Player player) =>
        player == Owner ? TriggerCombination(GlitchTrigger.RestSmithed) : Task.CompletedTask;

    public override Task AfterShuffle(PlayerChoiceContext choiceContext, Player player) =>
        player == Owner ? TriggerCombination(GlitchTrigger.Shuffle, choiceContext) : Task.CompletedTask;

    public override Task AfterHandEmptied(PlayerChoiceContext choiceContext, Player player) =>
        player == Owner ? TriggerCombination(GlitchTrigger.HandEmptied, choiceContext) : Task.CompletedTask;

    public override Task AfterCardGeneratedForCombat(CardModel card, Player? player) =>
        player == Owner ? TriggerCombination(GlitchTrigger.CardGenerated) : Task.CompletedTask;

    public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext,
        CombatSide side, IEnumerable<Creature> creatures) =>
        AfterOwnerTurnEnded(choiceContext, side, creatures);

    private async Task AfterOwnerTurnEnded(PlayerChoiceContext choiceContext,
        CombatSide side, IEnumerable<Creature> creatures)
    {
        if (side != CombatSide.Player || !creatures.Contains(Owner?.Creature)) return;
        var hadNoBlock = Owner?.Creature.Block <= 0;
        await TriggerCombination(GlitchTrigger.TurnEnded, choiceContext);
        if (Owner != null && PileType.Hand.GetPile(Owner).IsEmpty)
            await TriggerCombination(GlitchTrigger.EmptyHandTurnEnd, choiceContext);
        if (_attacksPlayedThisTurn == 0)
            await TriggerCombination(GlitchTrigger.NoAttackTurnEnd, choiceContext);
        if (hadNoBlock)
            await TriggerCombination(GlitchTrigger.NoBlockTurnEnd, choiceContext);
        if (_cardsPlayedThisTurn <= 3)
            await TriggerCombination(GlitchTrigger.FewCardsTurnEnd, choiceContext);
        if (_exhaustedThisTurn >= 5)
            await TriggerCombination(GlitchTrigger.FifthExhaustTurn, choiceContext);
        if (_turnNumberInCombat == 7)
            await TriggerCombination(GlitchTrigger.SeventhTurnCombat, choiceContext);
    }

    public override Task AfterEnergyReset(Player player) =>
        player == Owner ? TriggerCombination(GlitchTrigger.EnergyRefilled) : Task.CompletedTask;

    public override Task AfterGoldGained(Player player) =>
        player == Owner
            ? TriggerCombination(GlitchTrigger.GoldGained)
            : Task.CompletedTask;

    public override async Task AfterRoomEntered(AbstractRoom room)
    {
        if (Owner?.RunState is not RunState state) return;
        if (state.CurrentMapPoint is { } point && MysteriousMapRules.IsNestedEntry(point))
            await TriggerCombination(GlitchTrigger.RoomEntered);
        if (room is MerchantRoom &&
            !ShopRelicVisitRules.IsRepeatedMysteriousShopEntry(state, room))
            await TriggerCombination(GlitchTrigger.ShopEntered);
        if (room is RestSiteRoom)
            await TriggerCombination(GlitchTrigger.RestSiteEntered);
    }

    public override async Task AfterCombatVictory(CombatRoom room)
    {
        if (Owner == null) return;
        var wasLowHp = Owner.Creature.CurrentHp * 2m <= Owner.Creature.MaxHp;
        await TriggerCombination(GlitchTrigger.CombatVictory);
        if (room.RoomType == RoomType.Elite)
            await TriggerCombination(GlitchTrigger.EliteCombatVictory);
        if (wasLowHp)
            await TriggerCombination(GlitchTrigger.LowHpCombatVictory);
    }

    public override async Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext, PowerModel power, decimal amount,
        Creature? target, CardModel? cardSource)
    {
        if (Owner == null || amount <= 0) return;
        if (target == Owner.Creature)
        {
            if (power.Type == PowerType.Buff)
            {
                await TriggerCombination(GlitchTrigger.BuffGained, choiceContext);
                if (power is StrengthPower)
                    await TriggerCombination(GlitchTrigger.StrengthGained, choiceContext);
            }
            else if (power.Type == PowerType.Debuff)
                await TriggerCombination(GlitchTrigger.SelfDebuffed, choiceContext);
        }
        else if (target != null && power.Type == PowerType.Buff &&
                 GlitchTeamEffects.IsLivingTeammate(Owner, target))
            await TriggerCombination(GlitchTrigger.TeammateBuffGained, choiceContext);
        else if (target != null && Owner.Creature.CombatState?.Enemies.Contains(target) == true)
        {
            if (power.Type == PowerType.Debuff)
            {
                await TriggerCombination(GlitchTrigger.EnemyDebuffed, choiceContext, target);
                if (power is PoisonPower)
                    await TriggerCombination(GlitchTrigger.PoisonApplied, choiceContext, target);
                else if (power is WeakPower)
                    await TriggerCombination(GlitchTrigger.WeakApplied, choiceContext, target);
            }
            else if (power.Type == PowerType.Buff)
                await TriggerCombination(GlitchTrigger.EnemyBuffed, choiceContext, target);
        }
    }

    internal Task OnRelicObtained() => TriggerCombination(GlitchTrigger.RelicObtained);

    internal async Task AfterBombExploded(
        PlayerChoiceContext choiceContext, int explosions)
    {
        for (var i = 0; i < explosions; i++)
            await TriggerCombination(GlitchTrigger.BombExploded, choiceContext);
    }
}
