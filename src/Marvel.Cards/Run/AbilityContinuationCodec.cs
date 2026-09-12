using System.Collections.Immutable;
using System.Globalization;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityContinuationCodec;
using static Marvel.Cards.Run.AbilityContinuationWireCodec;

namespace Marvel.Cards.Run;

// Continuation wire data is intentionally decoded against the compiled program.
// Paths are engine-chosen save data, not an alternate executable syntax.

/// <summary>Owns the legacy continuation wire spelling and its authored-tree validation.</summary>
internal static class AbilityContinuationCodec
{
    internal static AbilityEffect Choice(
        AbilityProgram program, Card source, AbilityType? tier, int stoppedAt,
        PhaseStep? persisted, AbilityAdmissionContext context)
    {
        if (persisted is { AbilityOrdinal: >= 0, AbilityPath: { } } step)
            return PersistedChoice(program, source, step, tier, context);

        return LegacyChoice(program, source, tier, stoppedAt, persisted, context);
    }

    private static AbilityEffect PersistedChoice(
        AbilityProgram program, Card source, PhaseStep step, AbilityType? tier,
        AbilityAdmissionContext context)
    {
        var decoded = Decode(program, source, step, tier);
        return AbilityChoiceAnalysis.ActiveChoices(decoded.Node, context).SingleOrDefault()
            ?? throw new RulesNotImplementedException(
                $"'{source.FaceId}' has no choice at its persisted ability path");
    }

    private static AbilityEffect LegacyChoice(
        AbilityProgram program, Card source, AbilityType? tier, int stoppedAt,
        PhaseStep? persisted, AbilityAdmissionContext context)
    {

        var written = AbilitiesOn(program, source, persisted?.AbilityFace)
            .Where(ability => tier is null || ability.Trigger.Timing == tier)
            .ToList();
        if (written.Count > 1
            && written.Count(ability => AbilityChoiceAnalysis.ActiveChoices(
                ability.Effect, context).Any()) > 1)
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' has a choice in more than one '{tier}' ability, and a "
                + "suspended ability is found again from its card and its tier");

        var effect = written.Select(ability => ability.Effect)
            .FirstOrDefault(tree => AbilityChoiceAnalysis.ActiveChoices(tree, context).Any())
            ?? throw new RulesNotImplementedException(
                $"'{source.FaceId}' has no choice waiting on an answer");
        if (effect.OperationName() != "seq")
            return AbilityChoiceAnalysis.ActiveChoices(effect, context).Single();

        var steps = OrderedEffects(effect);
        if (stoppedAt >= 1 && stoppedAt <= steps.Length)
        {
            var nested = AbilityChoiceAnalysis.ActiveChoices(
                steps[stoppedAt - 1], context).ToList();
            if (nested.Count == 1) return nested[0];
        }
        throw new RulesNotImplementedException(
            $"'{source.FaceId}' has no single choice at step {stoppedAt - 1} of its sequence");
    }

    internal static RestoredContinuationState RestoreState(
        IReadOnlyList<Card> cards, IReadOnlyList<int>? discarded,
        IReadOnlyDictionary<string, long>? values, int actor, string sourceFace)
    {
        Card At(int id, string name) => id >= 0 && id < cards.Count ? cards[id]
            : throw new RulesNotImplementedException($"'{sourceFace}' has invalid persisted {name} metadata");
        var raw = values ?? ImmutableDictionary<string, long>.Empty;
        var chosen = ChosenBinding(cards, raw, sourceFace);
        var crisis = AbilityContinuationWireCodec.CrisisIgnoringThwartOrdinals(raw, sourceFace);
        var results = raw.Where(pair => pair.Key is not PersistedChosen
            and not PersistedChosenArea and not PersistedChosenIncarnation
            and not PersistedSourceIncarnation
            && !pair.Key.StartsWith(CrisisIgnoringThwartPrefix, StringComparison.Ordinal))
            .ToImmutableDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        return new((discarded ?? []).Select(id => At(id, "discarded-card")).ToImmutableArray(),
            results,
            raw.TryGetValue(PersistedSourceIncarnation, out long sourceIncarnation)
                ? checked((int)sourceIncarnation) : -1,
            chosen, actor >= 0 ? At(actor, "ability-actor") : null, crisis);
    }

    internal static AbilityContinuationCardBinding? ChosenBinding(
        IReadOnlyList<Card> cards, IReadOnlyDictionary<string, long>? values, string sourceFace)
    {
        if (values?.TryGetValue(PersistedChosen, out long selected) != true) return null;
        if (selected < 0 || selected >= cards.Count)
            throw new RulesNotImplementedException($"'{sourceFace}' has invalid persisted chosen-card metadata");
        if (!values.TryGetValue(PersistedChosenArea, out long area)
            || !values.TryGetValue(PersistedChosenIncarnation, out long incarnation))
            throw new RulesNotImplementedException(
                $"'{sourceFace}' has persisted chosen-card metadata without target provenance");
        return new(cards[(int)selected].ObjectId, checked((int)area), checked((int)incarnation));
    }
    internal static DecodedPowerContinuation DecodePower(
        AbilityProgram program, Card source, int abilityIndex, int powerOrdinal,
        string power, int resumeFrom, IReadOnlyList<string>? path, string savedFace,
        bool eachPlayerFrame, bool finalPlayer)
    {
        var ability = AbilityAt(program, source, null, abilityIndex, savedFace);
        var wrappers = AbilityAdmission.PowerEffects(ability.Effect, power).ToList();
        var wrapper = wrappers.ElementAtOrDefault(powerOrdinal)
            ?? throw new RulesNotImplementedException(
                $"'{source.FaceId}' ability {abilityIndex} has no {power.ToLowerInvariant()} wrapper {powerOrdinal}");
        var sameTier = AbilitiesOn(program, source, savedFace)
            .Where(candidate => candidate.Trigger.Timing == ability.Trigger.Timing).ToList();
        int ordinal = sameTier.IndexOf(ability);
        if (ordinal < 0) throw new RulesNotImplementedException(
            $"'{source.FaceId}' cannot identify its saved {power.ToLowerInvariant()} ability");
        ImmutableArray<AbilityStructuralFrame> frames;
        if (path is not null)
        {
            frames = FramesAtPath(ability.Effect, path, eachPlayerFrame, finalPlayer);
        }
        else if (resumeFrom >= 0)
        {
            if (ability.Effect is not AbilityEffect.Sequence sequence)
                throw new RulesNotImplementedException(
                    $"'{source.FaceId}' resumes a {power.ToLowerInvariant()} outside a sequence");
            frames = [new SequenceFrame(resumeFrom, sequence.Effects.Length)];
        }
        else
        {
            frames = [];
        }
        return new(ability, EffectBody(wrapper), frames, ordinal);
    }

    internal static DecodedEachPlayerContinuation DecodeEachPlayer(
        AbilityProgram program, Card source, PhaseStep? step, int stoppedAt,
        AbilityType? tier, int player, bool finalPlayer)
    {
        var written = AbilitiesOn(program, source, step?.AbilityFace)
            .Where(ability => tier is null || ability.Trigger.Timing == tier).ToList();
        int ordinal = EachPlayerOrdinal(step, written);
        var ability = written.ElementAtOrDefault(ordinal) ?? throw new RulesNotImplementedException(
            $"'{source.FaceId}' has no reconstructable each-player ability");
        var outer = ability.Effect;
        var path = EachPlayerPath(step, outer, stoppedAt);
        var each = NodeAtPath(outer, path);
        if (each is not AbilityEffect.EachPlayer eachPlayer)
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' has no each-player frame at step {stoppedAt - 1}");
        var frames = FramesAtPath(outer, path, eachPlayerFrame: true, finalPlayer)
            .Add(new EachPlayerFrame(player, finalPlayer));
        bool hasContinuation = HasEachPlayerContinuation(
            step, outer, stoppedAt, finalPlayer);
        return new(ability, eachPlayer.Effect, frames, ordinal, hasContinuation);
    }

    private static int EachPlayerOrdinal(
        PhaseStep? step, List<CompiledCardAbility> written) =>
        step is { What: Steps.ResolveEachPlayer, AbilityOrdinal: >= 0 }
            ? step.Value.AbilityOrdinal
            : written.FindIndex(ability =>
                AbilityPowerStateProjection.EachPlayers(ability.Effect).Any());

    private static IReadOnlyList<string> EachPlayerPath(
        PhaseStep? step, AbilityEffect outer, int stoppedAt) =>
        step is { What: Steps.ResolveEachPlayer, AbilityPath: { } saved }
            ? saved
            : outer.OperationName() == "seq" ? [$"seq:{stoppedAt - 1}"] : [];

    private static bool HasEachPlayerContinuation(
        PhaseStep? step, AbilityEffect outer, int stoppedAt, bool finalPlayer) =>
        finalPlayer && (step?.AbilityHasContinuation
            ?? (outer.OperationName() == "seq" && stoppedAt < OrderedEffects(outer).Length));
    internal const string PersistedChosen = "__continuation.chosen";
    internal const string PersistedChosenArea = "__continuation.chosen_area";
    internal const string PersistedChosenIncarnation = "__continuation.chosen_incarnation";
    internal const string PersistedSourceIncarnation = "__continuation.source_incarnation";
    internal const string CrisisIgnoringThwartPrefix =
        "__preflight.crisisIgnoringThwart.";

    internal static AbilityContinuationCapture Capture(
        int source, int sourceIncarnation, AbilityContinuationAddress address,
        IEnumerable<AbilityStructuralFrame> frames, int position, int player, int abilityPlayer,
        int abilityActor, bool finalStep, bool finalPlayer, bool eachPlayerFrame,
        bool hasContinuation, string trigger, bool surgeGained, Occurrence occurrence,
        IEnumerable<int> discarded, IReadOnlyDictionary<string, long> results,
        AbilityContinuationCardBinding? chosen,
        IEnumerable<int> crisisIgnoringThwarts) => new(
            source, sourceIncarnation, address, [.. frames], position, player, abilityPlayer,
            abilityActor, finalStep, finalPlayer, eachPlayerFrame, hasContinuation, trigger,
            surgeGained, occurrence, [.. discarded], PersistResults(
                results, sourceIncarnation, chosen, crisisIgnoringThwarts), chosen);

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
        AbilityContinuationCapture capture, string what, int round, bool plan = false,
        int? index = null, IReadOnlyList<int>? activationIds = null) => new(
            what, round, 2, Index: index ?? capture.Position + 1,
            Subject: capture.Source, Seat: capture.Player, Plan: plan,
            Tier: capture.Address.Tier, FinalStep: capture.FinalStep,
            FinalPlayer: capture.FinalPlayer, EachPlayerFrame: capture.EachPlayerFrame,
            Trigger: capture.Trigger, SurgeGained: capture.SurgeGained,
            Discarded: capture.Discarded, AbilityOrdinal: capture.Address.Ordinal,
            AbilityPath: [.. capture.Frames.Select(EncodeFrame)],
            AbilityActivationIds: activationIds,
            AbilityResults: capture.Results, AbilityOccurrence: capture.Occurrence,
            AbilityFace: capture.Address.Face, AbilityPlayer: capture.AbilityPlayer,
            AbilityActor: capture.AbilityActor, AbilityHasContinuation: capture.HasContinuation);

    internal static AbilityContinuationCapture ForCostProcedure(
        AbilityContinuationCapture capture) => capture with
        {
            Frames = [],
            Results = capture.Results.SetItem("costProcedurePending", 1),
        };

    internal static AbilityContinuationCapture ForEffectProcedure(
        AbilityContinuationCapture capture) => capture with
        { Results = capture.Results.SetItem("procedureApplied", 1) };

    internal static AbilityContinuationCapture ForActivations(
        AbilityContinuationCapture capture, bool dynamic)
    {
        var results = capture.Results
            .Remove("activationMade")
            .Remove("activationDamage")
            .Remove("activationThreat");
        if (dynamic)
            results = results.SetItem("repeatDynamicActivation", 1);
        return capture with { Results = results };
    }

    internal static CardPowerContinuation Power(
        AbilityContinuationCapture capture, int powerOrdinal, bool hasContinuation) => new(
            capture.Address.Ordinal, powerOrdinal,
            hasContinuation ? capture.Position + 1 : -1, capture.FinalStep,
            [], capture.SurgeGained, [.. capture.Frames.Select(EncodeFrame)], capture.Address.Face,
            capture.Results, capture.Occurrence, capture.Discarded,
            capture.EachPlayerFrame, capture.FinalPlayer, capture.AbilityPlayer,
            hasContinuation);

    /// <summary>
    /// Consumes one-shot wire markers before the executor can execute a node that
    /// might suspend again. The returned state is the only state a later capture
    /// may persist.
    /// </summary>
    internal static AbilityContinuationTransition BeginResume(
        AbilityProgram program, Card source, PhaseStep step)
    {
        var decoded = Decode(program, source, step, step.Tier);
        var state = decoded.State;
        if (state.Results.TryGetValue("costProcedurePending", out _))
            return new RestartAfterPaidCost(decoded.Ability, state with
            { Results = state.Results.Remove("costProcedurePending") });
        if (state.Results.TryGetValue("repeatDynamicActivation", out _))
        {
            var results = state.Results.Remove("repeatDynamicActivation");
            if (results.GetValueOrDefault("activationMade") > 0)
                results = results.SetItem("dynamicActivationMade", 1);
            return new RunResumedNode(
                decoded.Ability, decoded.Node, state with { Results = results },
                EffectApplied: results.GetValueOrDefault("activationMade") > 0);
        }
        bool effectApplied = state.Results.GetValueOrDefault("activationMade") > 0
            || state.Results.ContainsKey("procedureApplied");
        return new ContinueAfterResumedNode(
            decoded.Ability,
            state with { Results = state.Results.Remove("procedureApplied") },
            effectApplied);
    }

    internal static AbilityContinuationTransition BeginLegacyChoiceResume(
        AbilityProgram program, Card source, PhaseStep? step, AbilityType? tier,
        int from, bool eachPlayerFrame, bool finalPlayer,
        AbilityStructuralContext context) =>
        LegacyChoiceContinuation.Begin(
            program, source, step, tier, from, eachPlayerFrame, finalPlayer, context);

    internal static AbilityContinuationTransition? AfterPower(
        AbilityProgram program, Card source, DecodedPowerContinuation decoded,
        AbilityContinuationCapture capture, int round, bool suspended)
    {
        if (suspended || decoded.Frames.IsEmpty) return null;
        var state = Decode(
            program, source, Step(capture, Steps.ResumeAbility, round),
            decoded.Ability.Trigger.Timing).State;
        return new ContinueAfterResumedNode(decoded.Ability, state, EffectApplied: false);
    }

    internal static AbilityContinuationTransition? AfterEachPlayer(
        AbilityProgram program, Card source, DecodedEachPlayerContinuation decoded,
        AbilityContinuationCapture capture, int round, AbilityType? tier,
        bool finalPlayer, bool suspended)
    {
        if (suspended || !finalPlayer) return null;
        var state = Decode(
            program, source, Step(capture, Steps.ResumeAbility, round), tier).State;
        return new ContinueAfterResumedNode(decoded.Ability, state, EffectApplied: false);
    }

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
            context with { Frames = command.State.Frames }, command.Effect,
            command.Frame, new AbilityStructuralObservation(false, discarded));
        return transition switch
        {
            RunLeaf leaf => new RunResumedNode(command.Ability, leaf.Effect, command.State with
            {
                Frames = leaf.Frames,
                Position = leaf.Position,
                HasContinuation = leaf.HasContinuation,
            }),
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
        var values = new Dictionary<string, long>(step.AbilityResults
            ?? new Dictionary<string, long>(StringComparer.Ordinal), StringComparer.Ordinal)
        {
            ["activationMade"] = (step.AbilityResults?.GetValueOrDefault("activationMade") ?? 0) + (result.Made ? 1 : 0),
            ["activationDamage"] = (step.AbilityResults?.GetValueOrDefault("activationDamage") ?? 0) + result.DamageDealt,
            ["activationThreat"] = (step.AbilityResults?.GetValueOrDefault("activationThreat") ?? 0) + result.ThreatPlaced,
        };
        var remaining = (step.AbilityActivationIds ?? []).Where(id => id != result.Id).ToImmutableArray();
        return new(step with { AbilityResults = values, AbilityActivationIds = remaining }, remaining.IsEmpty);
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
        step with { AbilityResults = state.Results };

}
