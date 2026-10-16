using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using static Marvel.Cards.Run.AbilityContinuations;
using static Marvel.Cards.Run.AbilityContinuationFrames;

namespace Marvel.Cards.Run;

internal static class AbilityContinuationAdvancement
{
    internal static AbilityContinuationTransition Advance(
        AbilityStructuralContext context, CompiledCardAbility ability,
        AbilityContinuationState state, AbilityStructuralObservation observation)
    {
        if (observation.Suspended) return new ResumeComplete(state);
        var frames = state.Cursor.Frames;
        while (!frames.IsEmpty)
        {
            var frame = frames[^1];
            var prefix = frames.RemoveAt(frames.Length - 1);
            if (frame is AfterActivationFrame)
                return new ResumeComplete(state with { Cursor = state.Cursor with { Frames = [], HasContinuation = false } });
            if (PausesForNextPlayer(frame, state))
            {
                return new ResumeComplete(state with
                { Cursor = state.Cursor with { Frames = prefix, HasContinuation = true } });
            }
            var parent = NodeAtTypedPath(ability.Effect, prefix);
            var structural = context with
            {
                Position = state.Cursor.Position,
                HasContinuation = HasRemaining(prefix),
                Frames = prefix,
            };
            var transition = NextTransition(
                frame, parent, prefix, structural, observation);
            switch (transition)
            {
                case RunLeaf leaf:
                    return ResumedLeaf(ability, state, leaf);
                case DiscardEachTime discard when parent is AbilityEffect.EachTime repeated:
                    return new DiscardForResumedEachTime(
                        ability, repeated, discard.Frame,
                        state with
                        { Cursor = state.Cursor with { Frames = prefix, HasContinuation = structural.HasContinuation } });
                case Complete completed:
                    frames = prefix;
                    state = AfterCompletedFrame(state, frame, prefix);
                    observation = new AbilityStructuralObservation(false, Outcome: completed.Outcome ?? observation.Outcome);
                    continue;
                default:
                    return new ResumeRejected(Rejection(transition));
            }
        }
        return new ResumeComplete(state with { Cursor = state.Cursor with { Frames = [], HasContinuation = false } });
    }

    private static string Rejection(AbilityStructuralTransition transition) => transition switch
    {
        Rejected rejected => rejected.Reason,
        Unsupported unsupported => unsupported.Reason,
        _ => $"structural continuation returned {transition.GetType().Name}",
    };

    private static bool PausesForNextPlayer(
        AbilityStructuralFrame frame, AbilityContinuationState state) =>
        frame is EachPlayerFrame && state.Cursor.EachPlayerFrame && !state.Cursor.FinalPlayer;

    private static AbilityStructuralTransition NextTransition(
        AbilityStructuralFrame frame, AbilityEffect parent,
        ImmutableArray<AbilityStructuralFrame> prefix,
        AbilityStructuralContext structural, AbilityStructuralObservation observation) =>
        frame switch
        {
            SequenceFrame sequence when parent is AbilityEffect.Sequence node =>
                AbilityStructuralFlowExecution.NextSequence(
                    structural, node, sequence, observation),
            SimultaneousFrame simultaneous when parent is AbilityEffect.Simultaneous node =>
                AbilityStructuralFlowExecution.NextSimultaneous(
                    structural, node, simultaneous, observation),
            DependentFrame dependent when parent is AbilityEffect.Dependent node =>
                AbilityStructuralFlowExecution.AfterDependentLeaf(
                    structural, node, dependent, observation),
            ForEachFrame repeated when parent is AbilityEffect.ForEach node =>
                AbilityStructuralFlowExecution.NextForEach(
                    structural, node, repeated, observation),
            EachTimeFrame repeated when parent is AbilityEffect.EachTime node =>
                AbilityStructuralFlowExecution.NextEachTime(
                    structural, node, repeated, observation),
            ConditionalFrame or ChoiceFrame or ChoiceOtherwiseFrame
                or DefenseFrame or EachPlayerFrame => new Complete(prefix, Outcome: observation.Outcome),
            _ => new Rejected(
                $"ability continuation frame '{frame.GetType().Name}' does not match its authored parent"),
        };

    private static RunResumedNode ResumedLeaf(
        CompiledCardAbility ability, AbilityContinuationState state, RunLeaf leaf) =>
        new(ability, leaf.Effect, state with
        { Cursor = state.Cursor with { Frames = leaf.Frames, Position = leaf.Position, HasContinuation = leaf.HasContinuation } });

    private static AbilityContinuationState AfterCompletedFrame(
        AbilityContinuationState state, AbilityStructuralFrame frame,
        ImmutableArray<AbilityStructuralFrame> prefix) =>
        state with
        { Cursor = state.Cursor with { Frames = prefix, HasContinuation = HasRemaining(prefix), Player = frame is EachPlayerFrame && state.Invocation.AbilityPlayer >= 0
                ? state.Invocation.AbilityPlayer
                : state.Cursor.Player } };
}
