using System.Collections.Immutable;
using Marvel.Rules.Play;
using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Resolves a host's printed return instruction before ordinary departure can discard stored cards.</summary>
internal static class AbilityBeneathReleaseExecution
{
    internal static AbilityDeckAndRevealResult Run(AbilityEffect.ReleaseBeneath effect, AbilityDeckAndRevealContext context)
    {
        bool applied = effect.RemoveHost || BeneathCards.Of(context.World, context.Source).Count > 0;
        var state = new AbilityCardStateContext(context.Expressions, context.Trigger,
            context.Events, context.CardPlayAbilities, context.Readiness, new AbilityCardStateResult());
        // rr:when-defeated-abilities.2.1 resolves the printed instruction before
        // the defeated card leaves play. Return is not a discard: detach these
        // cards before host removal can perform the ordinary attachment cascade.
        AbilityCardMovementExecution.ReturnToHand(new AbilityCardSelection.Query(AbilityCardQuery.BeneathThis), state);
        if (effect.RemoveHost)
            AbilityCardStateExecution.TryRun(new AbilityEffect.CardAction(AbilityCardInstruction.RemoveFromGame,
                new AbilityCardSelection.Bound(AbilityCardBinding.This)), state);
        return new(true, applied, null, ImmutableDictionary<string, long>.Empty);
    }
}
