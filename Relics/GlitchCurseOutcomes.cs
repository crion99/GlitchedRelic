using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace MysteriousMapMod.Relics;

/// <summary>
/// Direct, independently rollable pieces of the original curse effects.
/// Noncombat curse-card insertions share the existing once-per-room card
/// penalty so a feedback loop cannot silently fill the permanent deck.
/// </summary>
internal static class GlitchCurseOutcomes
{
    internal static void Register()
    {
        Register("doubt", true, context =>
            PowerCmd.Apply<WeakPower>(new ThrowingPlayerChoiceContext(),
                context.Owner.Creature, 1m, context.Owner.Creature, null, false));
        Register("shame", true, context =>
            PowerCmd.Apply<FrailPower>(new ThrowingPlayerChoiceContext(),
                context.Owner.Creature, 1m, context.Owner.Creature, null, false));
        Register("burn", true, context => DamageSelf(context, 2m,
            ValueProp.Unpowered | ValueProp.Move));
        Register("decay", true, context => DamageSelf(context, 2m,
            ValueProp.Unpowered | ValueProp.Move));
        Register("regret", true, context => DamageSelf(context,
            PileType.Hand.GetPile(context.Owner).Cards.Count,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move));
        Register("writhe", false, context => AddPenaltyCard(context,
            ModelDb.Card<Writhe>()));
        Register("clumsy", false, context => AddPenaltyCard(context,
            ModelDb.Card<Clumsy>()));
        Register("folly", false, context => AddPenaltyCard(context,
            ModelDb.Card<Folly>()));
        Register("normality", false, context => AddPenaltyCard(context,
            ModelDb.Card<Normality>()));
        Register("greed", false, context => AddPenaltyCard(context,
            ModelDb.Card<Greed>()));
        Register("injury", false, context => AddPenaltyCard(context,
            ModelDb.Card<Injury>()));
        Register("soot", false, context => AddPenaltyCard(context,
            ModelDb.Card<Soot>()));
    }

    private static void Register(string key, bool combat,
        Func<GlitchEffectContext, Task> apply) =>
        GlitchInteractionRegistry.RegisterOutcome($"curse.{key}.result",
            () => new LocString("relics",
                $"MYSTERIOUS_CURSE_{key.ToUpperInvariant()}_RESULT.label")
                .GetFormattedText(), combat, false, apply, isDrawback: true);

    private static Task DamageSelf(GlitchEffectContext context, decimal amount,
        ValueProp props) => amount <= 0 ? Task.CompletedTask :
        CreatureCmd.Damage(new ThrowingPlayerChoiceContext(),
            context.Owner.Creature, amount, props, null, null);

    private static async Task AddPenaltyCard(GlitchEffectContext context,
        CardModel canonical)
    {
        if (context.Relic.LastInjuryRoomCount == context.RunState.CurrentRoomCount)
            return;
        context.Relic.LastInjuryRoomCount = context.RunState.CurrentRoomCount;
        try
        {
            if (context.Owner.Creature.CombatState != null)
                await GlitchCardGeneration.AddToDrawPile(canonical, context.Owner);
            else
                await CardPileCmd.Add(context.RunState.CreateCard(canonical,
                    context.Owner), PileType.Deck);
        }
        catch
        {
            context.Relic.LastInjuryRoomCount = -1;
            throw;
        }
    }
}
