using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;

namespace MysteriousMapMod.Relics;

/// <summary>
/// Card-applied debuffs with well-defined player/enemy targets. Enemy-only
/// internal mechanics and debuffs needing a particular card source are omitted.
/// </summary>
internal static class GlitchRandomDebuffs
{
    internal readonly record struct Entry(Type PowerType, decimal Amount);

    internal static readonly Entry[] EnemyPool =
    [
        new(typeof(WeakPower), 2m),
        new(typeof(VulnerablePower), 2m),
        new(typeof(FrailPower), 2m),
        new(typeof(PoisonPower), 4m),
        new(typeof(DoomPower), 2m),
        new(typeof(ConstrictPower), 2m),
        new(typeof(SlowPower), 1m),
        new(typeof(StranglePower), 2m)
    ];

    internal static readonly Entry[] SelfPool =
    [
        new(typeof(WeakPower), 1m),
        new(typeof(VulnerablePower), 1m),
        new(typeof(FrailPower), 1m),
        new(typeof(PoisonPower), 2m),
        new(typeof(ConfusedPower), 1m),
        new(typeof(HexPower), 1m),
        new(typeof(NoBlockPower), 1m),
        new(typeof(NoDrawPower), 1m),
        new(typeof(NoEnergyGainPower), 1m),
        new(typeof(WraithFormPower), 1m),
        new(typeof(SlowPower), 1m),
        new(typeof(ConstrictPower), 2m),
        new(typeof(MindRotPower), 1m),
        new(typeof(DoomPower), 2m)
    ];

    internal static Task ApplyToEnemy(PlayerChoiceContext choiceContext, Player owner,
        RunState state, Creature target) => Apply(EnemyPool, choiceContext, owner, state, target);

    internal static Task ApplyToSelf(PlayerChoiceContext choiceContext, Player owner,
        RunState state) => Apply(SelfPool, choiceContext, owner, state, owner.Creature);

    private static async Task Apply(Entry[] pool, PlayerChoiceContext choiceContext,
        Player owner, RunState state, Creature target)
    {
        var available = (from entry in pool
            join power in ModelDb.AllPowers on entry.PowerType equals power.GetType()
            where power.Type == PowerType.Debuff
            select (entry.Amount, Power: power)).ToArray();
        if (available.Length == 0)
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, target,
                1m, owner.Creature, null, false);
            return;
        }
        var selected = available[state.Rng.Niche.NextInt(available.Length)];
        await PowerCmd.Apply(choiceContext, selected.Power.ToMutable(), target,
            selected.Amount, owner.Creature, null, false);
    }
}
