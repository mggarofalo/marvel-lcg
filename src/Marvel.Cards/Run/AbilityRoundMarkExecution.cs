using Marvel.Cards.Dsl;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

/// <summary>Records a named fact for the rest of the round.</summary>
internal static class AbilityRoundMarkExecution
{
    internal static bool TryRun(AbilityEffect effect, AbilityResolutionState cast)
    {
        if (effect is not AbilityRoundEffect.MarkThisRound mark) return false;
        RoundMarks.Record(cast.World, mark.Mark);
        cast.ResolveEffect();
        return true;
    }
}
