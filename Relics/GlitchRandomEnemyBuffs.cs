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
/// Generic buffs safe on any monster. Flutter is intentionally excluded:
/// its damage hook casts its owner to ThievingHopper.
/// </summary>
internal static class GlitchRandomEnemyBuffs
{
    internal readonly record struct Entry(Type PowerType, decimal Amount);

    internal static readonly Entry[] Pool =
    [
        new(typeof(StrengthPower), 2m),
        new(typeof(DexterityPower), 2m),
        new(typeof(RitualPower), 1m),
        new(typeof(ThornsPower), 2m),
        new(typeof(PlatingPower), 3m),
        new(typeof(ArtifactPower), 1m),
        new(typeof(BufferPower), 1m),
        new(typeof(RegenPower), 3m),
        new(typeof(SoarPower), 1m),
        new(typeof(IntangiblePower), 1m)
    ];

    internal static async Task Apply(PlayerChoiceContext choiceContext, Player owner,
        RunState state, Creature target)
    {
        var available = (from entry in Pool
            join power in ModelDb.AllPowers on entry.PowerType equals power.GetType()
            where power.Type == PowerType.Buff
            select (entry.Amount, Power: power)).ToArray();
        if (available.Length == 0) return;
        var selected = available[state.Rng.Niche.NextInt(available.Length)];
        await PowerCmd.Apply(choiceContext, selected.Power.ToMutable(), target,
            selected.Amount, owner.Creature, null, false);
    }
}
