using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionCardState
{
    internal static bool TryRunCardState(this AbilityResolutionExecution execution, AbilityEffect effect, AbilityResolutionState cast)
    {
        if (effect is AbilityEffect.ReturnInvocation)
        {
            AbilityInvocationPayment.Return(cast);
            cast.ResolveEffect();
            return true;
        }
        if (effect is AbilityEffect.DiscardInvocation)
        {
            var deck = cast.World.Seats[cast.Player].InvocationDeck
                ?? throw new RulesNotImplementedException("no resolving player Invocation deck");
            deck.DiscardTop(cast.Source, cast.Trigger, cast.Events);
            cast.ResolveEffect();
            return true;
        }
        var result = new AbilityCardStateResult();
        bool handled = AbilityCardStateExecution.TryRun(effect, new AbilityCardStateContext(
            cast.ExpressionContext(), cast.Trigger, cast.Events,
            execution.cardPlayAbilities, execution.readinessAbilities, result));
        if (!handled) return false;
        cast.Discarded.AddRange(result.Discarded);
        foreach (var (key, value) in result.Values) cast.Results[key] = value;
        if (result.InterruptedReady is { } ready) execution.ScheduleReady(effect, ready, cast);
        return true;
    }

    private static void ScheduleReady(this AbilityResolutionExecution execution,
        AbilityEffect effect, CardReadyBatch batch, AbilityResolutionState cast)
    {
        // The receipt counts each card that actually readies; a replaced ready
        // completes its part with zero (rr:replacement-effect.1).
        var completion = new EffectCompletion(1, batch.Targets.Length);
        cast.PendingCompletion = completion;
        var capture = AbilityContinuationScheduling.ForEffectProcedure(
            execution.Capture(cast, execution.AbilityOrdinal(effect, cast)));
        var continuation = AbilityContinuations.Step(capture,
            AbilityContinuationStage.Resume, cast.World.Agenda.Current?.Round ?? 0, plan: true);
        CardReadying.Schedule(cast.World, batch, completion, continuation);
        cast.Suspend();
    }
}
