using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    private static bool TryRunCardState(AbilityEffect effect, Cast cast)
    {
        var result = new AbilityCardStateResult();
        bool handled = AbilityCardStateExecution.TryRun(effect, new AbilityCardStateContext(
            cast.ExpressionContext(), cast.Trigger, cast.Events, cast.Abilities, result));
        if (!handled) return false;
        cast.Discarded.AddRange(result.Discarded);
        foreach (var (key, value) in result.Values) cast.Results[key] = value;
        return true;
    }
}
