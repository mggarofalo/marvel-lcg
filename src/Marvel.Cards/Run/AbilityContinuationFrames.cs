using System.Collections.Immutable;
using System.Globalization;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityContinuations;

namespace Marvel.Cards.Run;

/// <summary>Locates typed execution frames in the immutable ability program.</summary>
internal static class AbilityContinuationFrames
{
    internal static PhaseStep? ContinuationStep(
        PhaseStep? current, IReadOnlyList<PhaseStep> outstanding, int source, int stoppedAt,
        AbilityType? tier)
    {
        bool Matches(PhaseStep step) => step.What == Steps.ChooseOption && step.Subject == source
            && step.Index == stoppedAt && (tier is null || StateOf(step)?.Invocation.Address.Tier == tier);
        if (current is { } active && Matches(active)) return active;
        for (int index = outstanding.Count - 1; index >= 0; index--)
            if (Matches(outstanding[index])) return outstanding[index];
        return null;
    }

    internal static ImmutableArray<CompiledCardAbility> AbilitiesOn(
        AbilityProgram program, Card source, string? savedFace)
    {
        // A saved face is needed after a legitimate identity change. When it is
        // the current face, retain the facedown-drone guard in the normal query.
        return string.IsNullOrEmpty(savedFace) || string.Equals(savedFace, source.FaceId, StringComparison.Ordinal)
            ? [.. AbilityProgramQueries.On(program, source)]
            : [.. program.On(savedFace)];
    }

    internal static CompiledCardAbility AbilityAt(
        AbilityProgram program, Card source, AbilityType? tier, int ordinal, string? face = null) =>
        AbilitiesOn(program, source, face)
            .Where(ability => tier is null || ability.Trigger.Timing == tier)
            .ElementAtOrDefault(ordinal)
        ?? throw new RulesNotImplementedException($"'{source.FaceId}' has no '{tier}' ability {ordinal}");

    internal static int OrdinalForNode(AbilityProgram program, Card source, string face,
        AbilityType? tier, IReadOnlyList<AbilityStructuralFrame> frames, AbilityEffect node)
    {
        var matches = AbilitiesOn(program, source, face)
            .Where(ability => tier is null || ability.Trigger.Timing == tier)
            .Select((ability, ordinal) => (
                Node: TryNodeAtTypedPath(ability.Effect, frames), ordinal))
            .Where(candidate => ReferenceEquals(candidate.Node, node))
            .Select(candidate => candidate.ordinal).ToList();
        return matches.Count == 1 ? matches[0] : throw new RulesNotImplementedException(
            $"'{source.FaceId}' cannot identify the exact ability that suspended");
    }

    internal static RulesNotImplementedException Invalid(string frame) => new($"ability continuation frame '{frame}' is invalid");

    internal static bool HasRemaining(ImmutableArray<AbilityStructuralFrame> frames) =>
        frames.Skip(DetachedBoundary(frames)).Any(frame => frame switch
        {
            SequenceFrame sequence => sequence.Next < sequence.Count,
            SimultaneousFrame simultaneous => !simultaneous.Remaining.IsEmpty,
            DependentFrame { Predecessor: true, Outcome: { } outcome } dependent =>
                outcome == (dependent.OnFull
                    ? AbilityStructuralOutcome.Full
                    : AbilityStructuralOutcome.None),
            ForEachFrame repeated => repeated.Next < repeated.Count,
            EachTimeFrame repeated => repeated.Next < repeated.Count,
            _ => false,
        });

    private static int DetachedBoundary(ImmutableArray<AbilityStructuralFrame> frames)
    {
        for (int index = frames.Length - 1; index >= 0; index--)
            if (frames[index] is AfterActivationFrame) return index + 1;
        return 0;
    }

    internal static AbilityEffect NodeAtTypedPath(
        AbilityEffect root, IEnumerable<AbilityStructuralFrame> frames)
    {
        var node = root;
        foreach (var frame in frames)
        {
            node = frame switch
            {
                SequenceFrame sequence when node is AbilityEffect.Sequence parent =>
                    parent.Effects[checked(sequence.Next - 1)],
                SimultaneousFrame simultaneous when node is AbilityEffect.Simultaneous parent =>
                    parent.Effects[simultaneous.Current],
                ConditionalFrame conditional when node is AbilityEffect.Conditional parent =>
                    (conditional.Then ? parent.Then : parent.Else) ?? throw Invalid(frame.ToString()!),
                DependentFrame dependent when node is AbilityEffect.Dependent parent =>
                    dependent.Predecessor ? parent.Effect : parent.Continuation,
                ForEachFrame when node is AbilityEffect.ForEach parent => parent.Effect,
                EachTimeFrame when node is AbilityEffect.EachTime parent => parent.Then,
                ChoiceFrame { Option: { } option } when node is AbilityEffect.Choose parent =>
                    parent.Options[option],
                ChoiceFrame when node is AbilityEffect.ChooseCard parent => parent.Effect,
                ChoiceOtherwiseFrame => EffectFollowing(node),
                DefenseFrame => EffectBody(node),
                PowerFrame when node is AbilityEffect.Power parent => parent.Effect,
                PowerFrame when node is AbilityEffect.ThwartGroup parent => parent.Thwart.Effect,
                EachPlayerFrame when node is AbilityEffect.EachPlayer parent => parent.Effect,
                AfterActivationFrame when node is AbilityEffect.AfterActivation parent => parent.Effect,
                _ => throw Invalid(frame.ToString()!),
            };
        }
        return node;
    }

    internal static AbilityEffect? TryNodeAtTypedPath(
        AbilityEffect root, IEnumerable<AbilityStructuralFrame> frames)
    {
        try { return NodeAtTypedPath(root, frames); }
        catch (Exception error) when (error is AbilityException
            or ArgumentOutOfRangeException or IndexOutOfRangeException
            or InvalidOperationException or RulesNotImplementedException)
        {
            return null;
        }
    }

    internal static AbilityContinuationState? StateOf(PhaseStep? step) =>
        (step?.Operation as AbilityContinuationAgendaOperation)?.Continuation as AbilityContinuationState;

    internal static DecodedAbilityContinuation Decode(
        AbilityProgram program, Card source, PhaseStep step, AbilityType? tier)
    {
        if (StateOf(step) is not { } saved)
            throw new InvalidOperationException("the agenda operation has no typed ability continuation");
        if (saved.Invocation.Source.ObjectId != source.ObjectId)
            throw new InvalidOperationException("the continuation belongs to another source");
        var state = saved with
        { Cursor = saved.Cursor with { Player = step.Seat, FinalPlayer = step.What == Steps.ResolveEachPlayer
                ? step.Require<AbilityContinuationAgendaOperation>().FinalPlayer : saved.Cursor.FinalPlayer, EachPlayerFrame = step.What == Steps.ResolveEachPlayer || saved.Cursor.EachPlayerFrame }, Memory = saved.Memory with { SurgeGained = step.Require<AbilityContinuationAgendaOperation>().SurgeGained } };
        var ability = AbilityAt(program, source, state.Invocation.Address.Tier,
            state.Invocation.Address.Ordinal, state.Invocation.Address.Face);
        var node = TryNodeAtTypedPath(ability.Effect, state.Cursor.Frames)
            ?? throw new RulesNotImplementedException("an ability continuation frame does not match its instruction");
        return new(state, ability, node, DescribeFrames(ability.Effect, state));
    }

    private static AbilityContinuationFacts DescribeFrames(
        AbilityEffect root, AbilityContinuationState state)
    {
        var facts = ImmutableArray.CreateBuilder<AbilityContinuationFrame>();
        for (int index = DetachedBoundary(state.Cursor.Frames); index < state.Cursor.Frames.Length; index++)
        {
            var parent = NodeAtTypedPath(root, state.Cursor.Frames.Take(index));
            if (DescribeFrame(parent, state.Cursor.Frames[index], state) is { } fact)
                facts.Add(fact);
        }
        return new(true, facts.ToImmutable());
    }

    private static AbilityContinuationFrame? DescribeFrame(
        AbilityEffect parent, AbilityStructuralFrame frame, AbilityContinuationState state) => frame switch
    {
        SequenceFrame sequence => new SequenceContinuationFrame((AbilityEffect.Sequence)parent, sequence.Next - 1),
        SimultaneousFrame simultaneous => new SimultaneousContinuationFrame((AbilityEffect.Simultaneous)parent, simultaneous.Remaining),
        DependentFrame dependent => new DependentContinuationFrame((AbilityEffect.Dependent)parent, dependent.Predecessor, dependent.Outcome),
        ForEachFrame repeated => new ForEachContinuationFrame((AbilityEffect.ForEach)parent, repeated.Next - 1, repeated.Count),
        EachTimeFrame repeated => new EachTimeContinuationFrame((AbilityEffect.EachTime)parent, repeated.Next - 1, repeated.Count),
        EachPlayerFrame => new EachPlayerContinuationFrame(state.Cursor.EachPlayerFrame && !state.Cursor.FinalPlayer),
        _ => null,
    };
}
