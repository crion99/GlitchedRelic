using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;

namespace MysteriousMapMod.Relics;

/// <summary>
/// Keeps class-specific mechanics out of unrelated characters' Dataminer rolls.
/// Character cards and potions are separately selected from the owner's pools.
/// </summary>
internal static class GlitchCharacterPool
{
    internal static bool Allows(CharacterModel character, GlitchTrigger trigger) =>
        trigger != GlitchTrigger.PoisonApplied || character is Silent;

    internal static bool Allows(CharacterModel character, GlitchOutcome outcome) =>
        outcome is not (GlitchOutcome.Poison or GlitchOutcome.SelfPoison or
            GlitchOutcome.EnemyHeavyPoison) || character is Silent;

    internal static bool Allows(CharacterModel character, string id)
    {
        if (id.StartsWith("core.t", StringComparison.Ordinal) &&
            int.TryParse(id.AsSpan(6), out var trigger) &&
            Enum.IsDefined((GlitchTrigger)trigger))
            return Allows(character, (GlitchTrigger)trigger);
        if (id.StartsWith("core.o", StringComparison.Ordinal) &&
            int.TryParse(id.AsSpan(6), out var outcome) &&
            Enum.IsDefined((GlitchOutcome)outcome))
            return Allows(character, (GlitchOutcome)outcome);
        return id switch
        {
            "vanilla.snecko_skull.condition" or "vanilla.snecko_skull.result" or
                "vanilla.twisted_funnel.condition" or "vanilla.twisted_funnel.result"
                => character is Silent,
            "vanilla.data_disk.condition" or "vanilla.data_disk.result"
                => character is Defect,
            "vanilla.cracked_core.condition" or "vanilla.cracked_core.result"
                => character is Defect,
            "vanilla.runic_capacitor.condition" or "vanilla.runic_capacitor.result" or
                "vanilla.symbiotic_virus.condition" or "vanilla.symbiotic_virus.result"
                => character is Defect,
            "vanilla.lunar_pastry.condition" or "vanilla.lunar_pastry.result"
                => character is Regent,
            "vanilla.divine_destiny.condition" or "vanilla.divine_destiny.result" or
                "vanilla.divine_right.condition" or "vanilla.divine_right.result"
                => character is Regent,
            "vanilla.ring_of_the_snake.condition" or "vanilla.ring_of_the_snake.result" or
                "vanilla.ring_of_the_drake.condition" or "vanilla.ring_of_the_drake.result"
                => character is Silent,
            "vanilla.ninja_scroll.condition" or "vanilla.ninja_scroll.result"
                => character is Silent,
            "vanilla.radiant_pearl.condition" or "vanilla.radiant_pearl.result"
                => character is Regent,
            "vanilla.paels_claw.condition" or "vanilla.paels_claw.result" or
                "vanilla.paels_horn.condition" or "vanilla.paels_horn.result"
                => character is Necrobinder,
            "vanilla.electric_shrymp.condition" or "vanilla.electric_shrymp.result"
                => character is Defect,
            "vanilla.paels_growth.condition" or "vanilla.paels_growth.result" or
                "vanilla.paels_tooth.condition" or "vanilla.paels_tooth.result" or
                "vanilla.funerary_mask.condition" or "vanilla.funerary_mask.result"
                => character is Necrobinder,
            "vanilla.mini_regent.condition" or "vanilla.mini_regent.result"
                => character is Regent,
            _ => true
        };
    }
}
