namespace MysteriousMapMod.Relics;

/// <summary>
/// Allows feedback combinations to cascade, but bounds one synchronous game-action
/// cascade so a 100% self-loop cannot lock the game or fill the hand forever.
/// A later independent action starts a fresh cascade.
/// </summary>
internal sealed class GlitchCascadeLimiter
{
    // A feedback event may re-enter at most four times after its first firing.
    internal const int MaxDepth = 5;
    internal const int MaxEffectsPerCascade = 20;
    internal const int MaxGeneratedCardsPerCascade = 5;
    internal const int MaxEffectsPerTurn = 40;

    private int _depth;
    private int _effects;
    private int _generatedCards;
    private int _turnEffects;

    internal void BeginTurn() => _turnEffects = 0;

    internal bool TryEnter(bool inCombat)
    {
        if (_depth >= MaxDepth || (inCombat && _turnEffects >= MaxEffectsPerTurn))
            return false;
        if (_depth == 0)
        {
            _effects = 0;
            _generatedCards = 0;
        }
        _depth++;
        return true;
    }

    internal bool TrySpend(GlitchOutcome outcome, bool inCombat)
    {
        if (_effects >= MaxEffectsPerCascade ||
            (inCombat && _turnEffects >= MaxEffectsPerTurn))
            return false;
        var cards = outcome switch
        {
            GlitchOutcome.CreativeAi or GlitchOutcome.CharacterCard or
                GlitchOutcome.CharacterAttack or GlitchOutcome.CharacterSkill or
                GlitchOutcome.CharacterPower or GlitchOutcome.TeammateCard or
                GlitchOutcome.TeammateInjury => 1,
            GlitchOutcome.DoubleCharacterCard => 2,
            _ => 0
        };
        if (_generatedCards + cards > MaxGeneratedCardsPerCascade)
            return false;
        _generatedCards += cards;
        _effects++;
        if (inCombat) _turnEffects++;
        return true;
    }

    internal bool TrySpendExternal(bool inCombat)
    {
        if (_effects >= MaxEffectsPerCascade ||
            (inCombat && _turnEffects >= MaxEffectsPerTurn))
            return false;
        _effects++;
        if (inCombat) _turnEffects++;
        return true;
    }

    internal void Exit()
    {
        if (_depth > 0) _depth--;
    }
}
