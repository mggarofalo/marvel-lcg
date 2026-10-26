using System.Collections.Immutable;
using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

internal static class AbilityDiscardIterationExecution
{
    /// <summary>Interrupts a discard effect once for every matching card.</summary>
    /// <remarks>
    /// <c>rr:alteration-effect</c> says an “each time” effect halts the
    /// preceding ability, resolves in its entirety, and only then lets that
    /// ability continue. Discarding one card per frame makes that ordering
    /// observable: its alteration finishes before the next card is discarded.
    /// The exact-card binding survives an immediate encounter-deck reset.
    /// </remarks>
    internal static void EachTime(this AbilityResolutionExecution execution, AbilityEffect node, AbilityResolutionState cast)
        => execution.ContinueEachTime((AbilityEffect.EachTime)node, cast, from: 0, count: null);

    internal static void ContinueEachTime(this AbilityResolutionExecution execution,
        AbilityEffect.EachTime repeated, AbilityResolutionState cast, long from, long? count)
    {
        bool outerContinuation = cast.HasContinuation;
        var transition = AbilityStructuralFlowExecution.EachTimeStart(
            execution.StructuralContext(cast), repeated, from, count);
        ImmutableArray<int> receipt = default;
        if (repeated.AfterDiscard)
        {
            if (transition is not DiscardEachTime)
            {
                execution.ApplyStructuralCompletion(transition, cast);
                return;
            }
            int before = cast.Discarded.Count;
            if (!execution.TryRunCardState(repeated.Effect, cast))
                throw new InvalidOperationException("The card-state owner refused discardTop");
            receipt = [.. cast.Discarded.Skip(before).Select(card => card.ObjectId)];
            transition = AbilityStructuralFlowExecution.EachTimeStart(
                execution.StructuralContext(cast), repeated, from, count, receipt);
        }
        while (transition is DiscardEachTime discard)
        {
            var discarded = execution.EachTimeDiscard(cast, discard.Frame);
            if (discarded is not null)
                cast.BindAlteration(discarded);
            transition = AbilityStructuralFlowExecution.AfterEachTimeDiscard(
                execution.StructuralContext(cast), repeated, discard.Frame,
                new AbilityStructuralObservation(false, discarded));
            if (transition is not RunLeaf leaf
                || leaf.Frames[^1] is not EachTimeFrame frame)
            {
                continue;
            }

            execution.RunStructuralLeaf(leaf, cast);
            var observation = new AbilityStructuralObservation(cast.Suspended, Outcome: cast.CompletedOutcome);
            if (!cast.Suspended)
                cast.SetContinuation(outerContinuation);
            transition = AbilityStructuralFlowExecution.NextEachTime(
                execution.StructuralContext(cast), repeated, frame,
                observation);
            if (cast.Suspended)
                return;
        }
        execution.ApplyStructuralCompletion(transition, cast);
        cast.SetContinuation(outerContinuation);
    }

}
