using System.Collections.Immutable;
using System.Globalization;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityContinuations;
using static Marvel.Cards.Run.AbilityContinuationFrames;

namespace Marvel.Cards.Run;

/// <summary>Captures and restores game-owned interpreter execution state.</summary>
internal static class AbilityContinuations
{
    internal static AbilityEffect Choice(
        AbilityProgram program, Card source, AbilityType? tier, int stoppedAt,
        PhaseStep? persisted, AbilityAdmissionContext context)
    {
        if (persisted is not { Operation: AbilityContinuationAgendaOperation { Continuation: AbilityContinuationState } } step)
            throw new InvalidOperationException("a choice requires its suspended ability frame");
        var decoded = Decode(program, source, step, tier);
        return AbilityChoiceAnalysis.ActiveChoices(decoded.Node, context).SingleOrDefault()
            ?? throw new RulesNotImplementedException($"'{source.FaceId}' has no choice at its typed ability frame");
    }

    internal static DecodedPowerContinuation DecodePower(
        AbilityProgram program, Card source, AbilityPowerContinuation continuation, string power)
    {
        var ability = AbilityAt(program, source, null, continuation.Capture.Invocation.Address.Ordinal, continuation.Capture.Invocation.Address.Face);
        var wrappers = AbilityAdmission.PowerEffects(ability.Effect, power).ToList();
        var wrapper = wrappers.ElementAtOrDefault(continuation.PowerOrdinal)
            ?? throw new RulesNotImplementedException(
                $"'{source.FaceId}' ability {continuation.Capture.Invocation.Address.Ordinal} has no "
                + $"{power.ToLowerInvariant()} wrapper {continuation.PowerOrdinal}");
        var sameTier = AbilitiesOn(program, source, continuation.Capture.Invocation.Address.Face)
            .Where(candidate => candidate.Trigger.Timing == ability.Trigger.Timing).ToList();
        int ordinal = sameTier.IndexOf(ability);
        if (ordinal < 0) throw new RulesNotImplementedException(
            $"'{source.FaceId}' cannot identify its saved {power.ToLowerInvariant()} ability");
        var frames = continuation.Capture.Cursor.Frames;
        return new(ability, EffectBody(wrapper), frames, ordinal);
    }

    internal static DecodedEachPlayerContinuation DecodeEachPlayer(
        AbilityProgram program, Card source, PhaseStep? step, int stoppedAt,
        AbilityType? tier, int player, bool finalPlayer)
    {
        if (step is not { Operation: AbilityContinuationAgendaOperation { Continuation: AbilityContinuationState } } current)
            throw new InvalidOperationException("an each-player operation requires its ability frame");
        var decoded = Decode(program, source, current, tier);
        if (decoded.Node is not AbilityEffect.EachPlayer each)
            throw new InvalidOperationException("the suspended node is not an each-player effect");
        var frames = decoded.State.Cursor.Frames.Add(new EachPlayerFrame(player, finalPlayer));
        return new(decoded.Ability, each.Effect, frames, decoded.State.Invocation.Address.Ordinal,
            finalPlayer && decoded.State.Cursor.HasContinuation);
    }

    internal static ImmutableHashSet<int> CrisisIgnoringThwartOrdinals(
        CompiledCardAbility ability, IReadOnlySet<AbilityEffect> validated,
        IReadOnlySet<int> restored)
    {
        var nodes = AbilityAdmission.PowerEffects(
            ability.Effect, BasicPowers.ThwartVerb).ToList();
        return nodes.Select((node, ordinal) => (node, ordinal))
            .Where(candidate => validated.Contains(candidate.node)
                || restored.Contains(candidate.ordinal))
            .Select(candidate => candidate.ordinal)
            .ToImmutableHashSet();
    }

    internal static PhaseStep Step(
        AbilityContinuationState capture, AbilityContinuationStage stage, int round, bool plan = false,
        int? index = null, IReadOnlyList<int>? activationIds = null) => new(
            new AbilityContinuationAgendaOperation(stage)
            {
                Continuation = capture,
                Origin = capture.Invocation.Occurrence,
                SurgeGained = capture.Memory.SurgeGained,
                AwaitedActivations = activationIds ?? [],
            }, round, 2, Index: index ?? capture.Cursor.Position + 1,
            Subject: capture.Invocation.Source.ObjectId, Seat: capture.Cursor.Player, Plan: plan);

    internal static AbilityContinuationTransition BeginResume(
        AbilityProgram program, Card source, PhaseStep step) =>
        AbilityContinuationResumption.Begin(program, source, step);

    internal static AbilityContinuationTransition? AfterPower(
        DecodedPowerContinuation decoded, AbilityContinuationState state, bool suspended) =>
        suspended || decoded.Frames.IsEmpty ? null
            : new ContinueAfterResumedNode(decoded.Ability, state, EffectApplied: false);

    internal static AbilityContinuationTransition? AfterEachPlayer(
        DecodedEachPlayerContinuation decoded, AbilityContinuationState state, bool suspended) =>
        suspended || !state.Cursor.FinalPlayer ? null
            : new ContinueAfterResumedNode(decoded.Ability, state, EffectApplied: false);

    /// <summary>
    /// Advances from one completed structural child. The caller executes only
    /// the returned command, refreshes its concrete context, and reports the
    /// resulting observation here again.
    /// </summary>
    internal static AbilityContinuationTransition Advance(
        AbilityStructuralContext context, CompiledCardAbility ability,
        AbilityContinuationState state, AbilityStructuralObservation observation) =>
        AbilityContinuationAdvancement.Advance(context, ability, state, observation);

    internal static AbilityContinuationTransition AfterEachTimeDiscard(
        AbilityStructuralContext context, DiscardForResumedEachTime command,
        Card? discarded)
    {
        var transition = AbilityStructuralFlowExecution.AfterEachTimeDiscard(
            context with { Frames = command.State.Cursor.Frames }, command.Effect,
            command.Frame, new AbilityStructuralObservation(false, discarded));
        return transition switch
        {
            RunLeaf leaf => new RunResumedNode(command.Ability, leaf.Effect, command.State with
            { Cursor = command.State.Cursor with { Frames = leaf.Frames, Position = leaf.Position, HasContinuation = leaf.HasContinuation } }),
            DiscardEachTime next => new DiscardForResumedEachTime(
                command.Ability, command.Effect, next.Frame, command.State),
            Complete => Advance(
                context, command.Ability,
                command.State,
                new AbilityStructuralObservation(false)),
            Rejected rejected => new ResumeRejected(rejected.Reason),
            Unsupported unsupported => new ResumeRejected(unsupported.Reason),
            _ => new ResumeRejected(
                $"each-time continuation returned {transition.GetType().Name}"),
        };
    }

    internal static ActivationWaitResult RecordActivationResult(
        PhaseStep step, EnemyActivation result)
    {
        var state = StateOf(step)
            ?? throw new InvalidOperationException("activation wait has no interpreter frame");
        var values = state.Memory.Results
            .SetItem("activationMade", state.Memory.Results.GetValueOrDefault("activationMade") + (result.Made ? 1 : 0))
            .SetItem("activationDamage", state.Memory.Results.GetValueOrDefault("activationDamage") + result.DamageDealt)
            .SetItem("activationThreat", state.Memory.Results.GetValueOrDefault("activationThreat") + result.ThreatPlaced);
        values = AbilityActivationReceipts.Record(values, result);
        var remaining = step.Require<AbilityContinuationAgendaOperation>().AwaitedActivations.Where(id => id != result.Id).ToImmutableArray();
        return new(step with { Operation = step.Require<AbilityContinuationAgendaOperation>() with
        { AwaitedActivations = remaining, Continuation = state with { Memory = state.Memory with { Results = values } } } }, remaining.IsEmpty);
    }

    internal static void RecordImmediateActivationResult(
        IDictionary<string, long> results, EnemyActivation activation)
    {
        results["activationDamage"] = activation.DamageDealt;
        results["activationThreat"] = activation.ThreatPlaced;
        results["activationMade"] = activation.Made ? 1 : 0;
    }

    internal static void CompleteDynamicActivation(IDictionary<string, long> results)
    {
        results["activationMade"] = results.TryGetValue(
            "dynamicActivationMade", out long made) ? made : 0;
    }

    internal static PhaseStep WithResumedResults(
        PhaseStep step, AbilityContinuationState state) =>
        step with { Operation = step.Require<AbilityContinuationAgendaOperation>() with { Continuation = state } };

}
