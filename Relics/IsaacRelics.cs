using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MysteriousMapMod.Map;

namespace MysteriousMapMod.Relics;

/// <summary>
/// Obtainable Isaac relics with project-owned artwork.
/// </summary>
public abstract class IsaacMapRelic : RelicModel
{
    public override string PackedIconPath => $"res://MysteriousMap/relics/{IconBaseName}.png";
    protected override string PackedIconOutlinePath => PackedIconPath;
    protected override string BigIconPath => PackedIconPath;

    public override bool IsAllowed(IRunState runState) =>
        runState is RunState concrete &&
        (MysteriousMapRunMode.IsEnabled(concrete) || MysteriousMapRules.IsNestedMap(concrete.Map));

    public override bool ShouldReceiveCombatHooks => false;
}

public sealed class MysteriousBlueMap : IsaacMapRelic
{
    protected override string IconBaseName => "blue_map";
    public override RelicRarity Rarity => RelicRarity.Uncommon;
}

public sealed class MysteriousCompass : IsaacMapRelic
{
    protected override string IconBaseName => "compass";
    public override RelicRarity Rarity => RelicRarity.Uncommon;
}

public sealed class MysteriousBogoBombs : IsaacMapRelic
{
    protected override string IconBaseName => "bogo_bombs";
    public override RelicRarity Rarity => RelicRarity.Uncommon;
}

public sealed class MysteriousPyromaniac : IsaacMapRelic
{
    protected override string IconBaseName => "pyromaniac";
    public override RelicRarity Rarity => RelicRarity.Rare;
}

public sealed class MysteriousIpecac : IsaacMapRelic
{
    private bool _resolvingSplash;
    private int _multiTargetAttackDepth;
    private bool _multiTargetAttackHit;

    protected override string IconBaseName => "ipecac";
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override bool ShouldReceiveCombatHooks => true;

    public override Task BeforeCombatStart()
    {
        _resolvingSplash = false;
        _multiTargetAttackDepth = 0;
        _multiTargetAttackHit = false;
        return Task.CompletedTask;
    }

    public override Task BeforeAttack(AttackCommand command)
    {
        if (command.Attacker == Owner?.Creature && command.IsMultiTargeted)
        {
            _multiTargetAttackDepth++;
            _multiTargetAttackHit = false;
        }
        return Task.CompletedTask;
    }

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
    {
        if (command.Attacker != Owner?.Creature || !command.IsMultiTargeted ||
            _multiTargetAttackDepth <= 0)
            return;

        _multiTargetAttackDepth--;
        if (_multiTargetAttackDepth == 0 && _multiTargetAttackHit)
        {
            _multiTargetAttackHit = false;
            await RollSelfDamage(choiceContext);
        }
    }

    public override async Task AfterDamageGiven(
        PlayerChoiceContext choiceContext,
        Creature? dealer,
        DamageResult result,
        ValueProp props,
        Creature target,
        CardModel? cardSource)
    {
        if (_resolvingSplash || Owner == null || dealer != Owner.Creature ||
            result.TotalDamage <= 0 ||
            dealer.CombatState is not { } combat ||
            !combat.HittableEnemies.Contains(target))
            return;

        if (_multiTargetAttackDepth > 0)
        {
            // A normal area attack already hits every enemy. Count it once
            // for the self-damage roll without making its hits cross-splash.
            _multiTargetAttackHit = true;
            return;
        }

        _resolvingSplash = true;
        try
        {
            var splash = result.TotalDamage / 2;
            var others = combat.HittableEnemies
                .Where(enemy => enemy != target && enemy.IsHittable)
                .ToArray();
            if (splash > 0 && others.Length > 0)
                await CreatureCmd.Damage(choiceContext, others, splash, ValueProp.Unpowered, dealer);

            await RollSelfDamage(choiceContext);
        }
        finally
        {
            _resolvingSplash = false;
        }
    }

    private async Task RollSelfDamage(PlayerChoiceContext choiceContext)
    {
        if (Owner == null || Owner.RunState.Rng.Niche.NextInt(10) != 0)
            return;
        await CreatureCmd.Damage(
            choiceContext,
            Owner.Creature,
            10m,
            ValueProp.Unblockable | ValueProp.Unpowered,
            Owner.Creature);
    }

    internal async Task ResolveExistingAreaDamage(
        PlayerChoiceContext choiceContext,
        Func<Task> dealDamage,
        int originalHits)
    {
        // Bombs already target every enemy. Keep their original damage and
        // roll the Ipecac drawback once per bomb, not once per enemy hit.
        _resolvingSplash = true;
        try
        {
            await dealDamage();
        }
        finally
        {
            _resolvingSplash = false;
        }
        for (var hit = 0; hit < originalHits; hit++)
            await RollSelfDamage(choiceContext);
    }
}

public sealed class MysteriousSacredHeart : IsaacMapRelic
{
    protected override string IconBaseName => "sacred_heart";
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterObtained()
    {
        if (Owner == null)
            return;
        await CreatureCmd.GainMaxHp(Owner.Creature, 10m);
        await CreatureCmd.SetCurrentHp(Owner.Creature, Owner.Creature.MaxHp);
    }

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay) =>
        Owner != null && dealer == Owner.Creature &&
        dealer?.CombatState?.HittableEnemies.Contains(target!) == true
            ? 1.25m
            : 1m;
}

public sealed class MysteriousHolyMantle : IsaacMapRelic
{
    private bool _grantedThisCombat;

    protected override string IconBaseName => "wafer";
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override bool ShouldReceiveCombatHooks => true;

    public override Task BeforeCombatStart()
    {
        _grantedThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (_grantedThisCombat || player != Owner)
            return;
        _grantedThisCombat = true;
        await PowerCmd.Apply<BufferPower>(choiceContext, player.Creature, 1m, player.Creature, null, false);
    }
}

public sealed class MysteriousNineVolt : IsaacMapRelic
{
    private bool _grantedThisCombat;

    protected override string IconBaseName => "holy_mantle";
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    public override bool ShouldReceiveCombatHooks => true;

    public override Task BeforeCombatStart()
    {
        _grantedThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (_grantedThisCombat || player != Owner)
            return;
        _grantedThisCombat = true;
        await PlayerCmd.GainEnergy(1m, player);
    }
}

public sealed class MysteriousHalo : IsaacMapRelic
{
    private bool _grantedThisCombat;

    protected override string IconBaseName => "nine_volt";
    public override RelicRarity Rarity => RelicRarity.Uncommon;
    public override bool ShouldReceiveCombatHooks => true;

    public override async Task AfterObtained()
    {
        if (Owner != null)
            await CreatureCmd.GainMaxHp(Owner.Creature, 7m);
    }

    public override Task BeforeCombatStart()
    {
        _grantedThisCombat = false;
        return Task.CompletedTask;
    }

    public override async Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
    {
        if (_grantedThisCombat || player != Owner)
            return;
        _grantedThisCombat = true;
        await CreatureCmd.GainBlock(player.Creature, 5m, ValueProp.Unpowered, null, false);
    }
}

public sealed class MysteriousWafer : IsaacMapRelic
{
    protected override string IconBaseName => "halo";
    public override RelicRarity Rarity => RelicRarity.Rare;
    public override bool ShouldReceiveCombatHooks => true;

    public override decimal ModifyDamageMultiplicative(
        Creature? target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay) =>
        Owner != null && target == Owner.Creature && dealer != Owner.Creature
            ? 0.5m
            : 1m;
}
