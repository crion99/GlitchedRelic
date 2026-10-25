using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;

namespace MysteriousMapMod.Relics;

/// <summary>
/// Card-granted powers that can operate on any player character. Deliberately
/// excludes effects needing a particular orb, summon, Star pool, or card source.
/// This is separate from the character-local generated-card pool.
/// </summary>
internal static class GlitchRandomBuffs
{
    internal readonly record struct Entry(Type PowerType, decimal Amount);

    internal static readonly Entry[] Pool =
    [
        new(typeof(StrengthPower), 1m),
        new(typeof(DexterityPower), 1m),
        new(typeof(BufferPower), 1m),
        new(typeof(ArtifactPower), 1m),
        new(typeof(RegenPower), 2m),
        new(typeof(ThornsPower), 2m),
        new(typeof(VigorPower), 4m),
        new(typeof(IntangiblePower), 1m),
        new(typeof(PlatingPower), 2m),
        new(typeof(RitualPower), 1m),
        new(typeof(BlurPower), 1m),
        new(typeof(AfterimagePower), 1m),
        new(typeof(BarricadePower), 1m),
        new(typeof(BurstPower), 1m),
        new(typeof(CorruptionPower), 1m),
        new(typeof(DarkEmbracePower), 1m),
        new(typeof(DemonFormPower), 1m),
        new(typeof(EchoFormPower), 1m),
        new(typeof(EnvenomPower), 1m),
        new(typeof(FeelNoPainPower), 1m),
        new(typeof(FlameBarrierPower), 1m),
        new(typeof(HelloWorldPower), 1m),
        new(typeof(InfiniteBladesPower), 1m),
        new(typeof(JuggernautPower), 1m),
        new(typeof(MachineLearningPower), 1m),
        new(typeof(NoxiousFumesPower), 1m),
        new(typeof(PanachePower), 1m),
        new(typeof(RupturePower), 1m)
    ];

    internal static async Task Apply(PlayerChoiceContext choiceContext, Player owner, RunState state)
    {
        var available = (from entry in Pool
            join power in ModelDb.AllPowers on entry.PowerType equals power.GetType()
            where power.Type == PowerType.Buff
            select (entry.Amount, Power: power)).ToArray();
        if (available.Length == 0)
        {
            await PowerCmd.Apply<StrengthPower>(choiceContext, owner.Creature,
                1m, owner.Creature, null, false);
            return;
        }
        var selected = available[state.Rng.Niche.NextInt(available.Length)];
        await PowerCmd.Apply(choiceContext, selected.Power.ToMutable(), owner.Creature,
            selected.Amount, owner.Creature, null, false);
    }
}
