using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal sealed partial class AbilityResolutionExecution
{
    private bool TryRunCardState(AbilityEffect effect, AbilityResolutionState cast)
    {
        var result = new AbilityCardStateResult();
        bool handled = AbilityCardStateExecution.TryRun(effect, new AbilityCardStateContext(
            cast.ExpressionContext(), cast.Trigger, cast.Events,
            cardPlayAbilities, readinessAbilities, result));
        if (!handled) return false;
        cast.Discarded.AddRange(result.Discarded);
        foreach (var (key, value) in result.Values) cast.Results[key] = value;
        return true;
    }
}
