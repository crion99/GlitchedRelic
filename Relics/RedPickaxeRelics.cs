using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;

namespace MysteriousMapMod.Relics;

/// <summary>
/// Player-local active treasure item. Arming resets after the treasure relic is claimed.
/// </summary>
public sealed class MysteriousDataminer : IsaacMapRelic
{
    // Kept only so older run saves can still read their former charge value.
    // It no longer limits use or appears in the UI.
    [SavedProperty]
    public int RoomsCharged { get; set; }

    [SavedProperty]
    public bool Armed { get; set; }

    protected override string IconBaseName => "dataminer";
    public override string PackedIconPath => "res://MysteriousMap/relics/dataminer.png";
    protected override string PackedIconOutlinePath => PackedIconPath;
    protected override string BigIconPath => PackedIconPath;
    public override RelicRarity Rarity => RelicRarity.Rare;

    public override bool IsAllowed(IRunState runState) =>
        runState is RunState concrete &&
        (concrete.Players.Count == 1 || base.IsAllowed(runState));

    internal static bool CanUseInRun(RunState state) =>
        state.Players.Count == 1 || MysteriousMapMod.Map.MysteriousMapRules.IsNestedMap(state.Map);

    internal bool CanArm => !Armed;

    internal bool TryArm()
    {
        if (!CanArm)
            return false;
        Armed = true;
        return true;
    }

    internal void Consume()
    {
        Armed = false;
    }

    public override bool ShowCounter => false;
}

/// <summary>
/// Combinations are serialized as trigger/outcome pairs. Additional uses of
/// the pickaxe append branches to this one glitched relic.
/// </summary>
public sealed partial class MysteriousGlitchedRelic : IsaacMapRelic
{
    protected override string IconBaseName => "glitched_relic";
    public override string PackedIconPath => "res://MysteriousMap/relics/glitched_relic.png";
    protected override string PackedIconOutlinePath => PackedIconPath;
    protected override string BigIconPath => PackedIconPath;
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override bool ShouldReceiveCombatHooks => true;
    public override bool IsAllowed(IRunState runState) => false;

    public override Task AfterObtained()
    {
        if (ComboCodes.Length == 0 && ExternalComboCodes.Length == 0)
            AddRandomCombinations();
        return Task.CompletedTask;
    }

    internal int AddRandomEffects() => AddRandomCombinations();

    protected override IEnumerable<IHoverTip> ExtraHoverTips => [];

    public override Task BeforeCombatStart()
    {
        _cascade.BeginTurn();
        _combatStartPending = true;
        _firstBlockSeenCombat = false;
        _firstAttackSeenCombat = false;
        _firstHurtSeenCombat = false;
        _firstPotionSeenCombat = false;
        _firstBlockDoubleArmed = false;
        _firstBlockDoubleUsed = false;
        _turnNumberInCombat = 0;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStartEarly(
        PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return;
        _cascade.BeginTurn();
        _cardsPlayedThisTurn = 0;
        _attacksPlayedThisTurn = 0;
        _skillsPlayedThisTurn = 0;
        _powersPlayedThisTurn = 0;
        _exhaustedThisTurn = 0;
        _allThreeTypesTriggeredThisTurn = false;
        _turnNumberInCombat++;
        FlowerTurnCounter = (FlowerTurnCounter + 1) % 3;
        FakeFlowerTurnCounter = (FakeFlowerTurnCounter + 1) % 5;
        if (_combatStartPending)
        {
            _combatStartPending = false;
            await TriggerCombination(GlitchTrigger.CombatStarted, choiceContext);
            if (Owner.RunState is RunState state)
            {
                if (state.CurrentRoom?.RoomType == RoomType.Elite)
                    await TriggerCombination(GlitchTrigger.EliteCombatStarted, choiceContext);
                else if (state.CurrentRoom?.RoomType == RoomType.Boss)
                    await TriggerCombination(GlitchTrigger.BossCombatStarted, choiceContext);
            }
        }
        if (FlowerTurnCounter == 0)
            await TriggerCombination(GlitchTrigger.EveryThirdTurn, choiceContext);
        if (FakeFlowerTurnCounter == 0)
            await TriggerCombination(GlitchTrigger.EveryFifthTurn, choiceContext);
        if (_turnNumberInCombat <= 3)
            await TriggerCombination(GlitchTrigger.FirstThreeTurnsCombat, choiceContext);
        if (_turnNumberInCombat == 1)
            await TriggerCombination(GlitchTrigger.FirstTurnCombat, choiceContext);
        if (_turnNumberInCombat == 2)
            await TriggerCombination(GlitchTrigger.SecondTurnCombat, choiceContext);
        else if (_turnNumberInCombat == 3)
            await TriggerCombination(GlitchTrigger.ThirdTurnCombat, choiceContext);
        if (Owner.Gold >= 3)
            await TriggerCombination(GlitchTrigger.RichTurnStart, choiceContext);
        await TriggerCombination(GlitchTrigger.TurnStart, choiceContext);
    }

    public override async Task AfterPlayerTurnStart(
        PlayerChoiceContext choiceContext, Player player)
    {
        if (player != Owner) return;
        await TriggerCombination(GlitchTrigger.AfterTurnDraw, choiceContext);
        if (_turnNumberInCombat == 1)
            await TriggerCombination(GlitchTrigger.FirstTurnAfterDraw, choiceContext);
    }

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result,
        ValueProp props, Creature? target, CardModel? cardSource)
    {
        if (Owner == null || target == null || result.UnblockedDamage <= 0) return;
        if (GlitchTeamEffects.IsLivingTeammate(Owner, target))
        {
            await TriggerCombination(GlitchTrigger.TeammateHurt, choiceContext, dealer);
            return;
        }
        if (target != Owner.Creature) return;
        await TriggerCombination(GlitchTrigger.Hurt, choiceContext, dealer);
        if (!_firstHurtSeenCombat)
        {
            _firstHurtSeenCombat = true;
            await TriggerCombination(GlitchTrigger.FirstHurtCombat, choiceContext, dealer);
        }
    }

    public override async Task AfterCardExhausted(
        PlayerChoiceContext choiceContext, CardModel card, bool wasPlayed)
    {
        if (Owner == null || card.Owner != Owner || Owner.Creature.CombatState == null) return;
        _exhaustedThisTurn++;
        await TriggerCombination(GlitchTrigger.Exhaust, choiceContext);
        await TriggerCombination(wasPlayed
            ? GlitchTrigger.PlayedCardExhausted
            : GlitchTrigger.UnplayedCardExhausted, choiceContext);
    }

    public override Task AfterDamageGiven(
        PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result,
        ValueProp props, Creature target, CardModel? cardSource)
    {
        if (Owner == null || dealer == null || result.UnblockedDamage <= 0 ||
            Owner.Creature.CombatState?.HittableEnemies.Contains(target) != true)
            return Task.CompletedTask;
        if (dealer == Owner.Creature)
            return TriggerCombination(GlitchTrigger.Damage, choiceContext, target);
        return GlitchTeamEffects.IsLivingTeammate(Owner, dealer)
            ? TriggerCombination(GlitchTrigger.TeammateDamage, choiceContext, target)
            : Task.CompletedTask;
    }

    private static async Task AddCreativeAi(Player player)
    {
        await GlitchCardGeneration.AddToHand(ModelDb.Card<CreativeAi>(), player);
    }
}
