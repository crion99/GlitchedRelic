using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Runs;

namespace MysteriousMapMod.Relics;

/// <summary>
/// Stable, opt-in bridge for other mods. Generic game hooks already observe
/// modded cards and relics; custom events/effects can join the random pool here.
/// Registration must happen before a run starts. IDs should be namespaced,
/// e.g. "othermod.on_orb_evoked".
/// </summary>
public static class GlitchInteractionRegistry
{
    private static readonly Dictionary<string, GlitchTriggerRegistration> Triggers = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, GlitchOutcomeRegistration> Outcomes = new(StringComparer.Ordinal);

    public static void RegisterTrigger(string id, Func<string> label,
        bool inCombat, bool hasChoiceContext)
    {
        ValidateId(id);
        ArgumentNullException.ThrowIfNull(label);
        if (!Triggers.TryAdd(id, new(id, label, inCombat, hasChoiceContext, null)))
            throw new ArgumentException($"Glitch trigger already registered: {id}", nameof(id));
    }

    internal static void RegisterBuiltinTriggerAlias(string id, Func<string> label,
        GlitchTrigger source, bool inCombat, bool hasChoiceContext)
    {
        ValidateId(id);
        ArgumentNullException.ThrowIfNull(label);
        if (!Triggers.TryAdd(id, new(id, label, inCombat, hasChoiceContext, source)))
            throw new ArgumentException($"Glitch trigger already registered: {id}", nameof(id));
    }

    public static void RegisterOutcome(string id, Func<string> label,
        bool requiresCombat, bool requiresChoiceContext,
        Func<GlitchEffectContext, Task> apply) =>
        RegisterOutcome(id, label, requiresCombat, requiresChoiceContext,
            apply, isDrawback: false);

    public static void RegisterOutcome(string id, Func<string> label,
        bool requiresCombat, bool requiresChoiceContext,
        Func<GlitchEffectContext, Task> apply, bool isDrawback)
    {
        ValidateId(id);
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(apply);
        if (!Outcomes.TryAdd(id, new(id, label, requiresCombat,
                requiresChoiceContext, apply, isDrawback)))
            throw new ArgumentException($"Glitch outcome already registered: {id}", nameof(id));
    }

    /// <summary>Emit an owner-local custom event. Cascades use the same safety budget as built-in events.</summary>
    public static async Task EmitAsync(Player player, string triggerId,
        PlayerChoiceContext? choiceContext = null, Creature? enemy = null)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (!Triggers.ContainsKey(triggerId)) return;
        foreach (var relic in player.Relics.OfType<MysteriousGlitchedRelic>())
            await relic.TriggerExternal(triggerId, choiceContext, enemy);
    }

    internal static IReadOnlyList<GlitchTriggerRegistration> AllTriggers => Triggers.Values.ToArray();
    internal static IReadOnlyList<GlitchOutcomeRegistration> AllOutcomes => Outcomes.Values.ToArray();
    internal static IEnumerable<GlitchTriggerRegistration> AliasesFor(GlitchTrigger source) =>
        Triggers.Values.Where(item => item.BuiltInSource == source);
    internal static bool TryGetTrigger(string id, out GlitchTriggerRegistration registration) =>
        Triggers.TryGetValue(id, out registration!);
    internal static bool TryGetOutcome(string id, out GlitchOutcomeRegistration registration) =>
        Outcomes.TryGetValue(id, out registration!);

    private static void ValidateId(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !id.Contains('.') ||
            id.StartsWith("core.", StringComparison.Ordinal) ||
            id.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-')))
            throw new ArgumentException("Use a stable namespaced ASCII ID with letters, digits, '.', '_' or '-'.", nameof(id));
    }
}

internal sealed record GlitchTriggerRegistration(
    string Id, Func<string> Label, bool InCombat, bool HasChoiceContext,
    GlitchTrigger? BuiltInSource);

internal sealed record GlitchOutcomeRegistration(
    string Id, Func<string> Label, bool RequiresCombat,
    bool RequiresChoiceContext, Func<GlitchEffectContext, Task> Apply,
    bool IsDrawback);

public sealed record GlitchEffectContext(
    MysteriousGlitchedRelic Relic, Player Owner, RunState RunState,
    PlayerChoiceContext? ChoiceContext, Creature? EventEnemy);
