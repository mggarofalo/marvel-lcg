using System.Collections.Immutable;
using System.Globalization;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityEffectStructure;

namespace Marvel.Cards.Run;

// Continuation wire data is intentionally decoded against the compiled program.
// Paths are engine-chosen save data, not an alternate executable syntax.
internal sealed record AbilityContinuationAddress(string Face, AbilityType? Tier, int Ordinal);
internal sealed record AbilityContinuationCardBinding(int ObjectId, int AreaId, int Incarnation);
internal enum AbilityResumeReason { Choice, CostProcedure, EffectProcedure, Activations, Power, EachPlayer }

internal sealed record AbilityContinuationState(
    AbilityContinuationAddress Address,
    ImmutableArray<AbilityStructuralFrame> Frames,
    int Position, int Player, int AbilityPlayer, int AbilityActor,
    bool FinalStep, bool FinalPlayer, bool EachPlayerFrame, bool HasContinuation,
    string Trigger, bool SurgeGained, Occurrence? Occurrence,
    ImmutableArray<int> Discarded, ImmutableDictionary<string, long> Results,
    AbilityContinuationCardBinding Source, AbilityContinuationCardBinding? Chosen,
    ImmutableHashSet<int> CrisisIgnoringThwarts, ImmutableArray<int> ActivationIds,
    AbilityResumeReason Reason);

internal sealed record AbilityContinuationWire(
    int Ordinal, ImmutableArray<string> Path, ImmutableArray<int> ActivationIds,
    ImmutableDictionary<string, long> Results, string Face, int Player, int Actor,
    Occurrence? Occurrence, bool FinalStep, bool FinalPlayer, bool EachPlayerFrame,
    bool HasContinuation, string Trigger, bool SurgeGained, ImmutableArray<int> Discarded);

internal sealed record DecodedAbilityContinuation(
    AbilityContinuationState State, CompiledCardAbility Ability, AbilityEffect Node,
    AbilityContinuationFacts Facts);
internal sealed record DecodedPowerContinuation(
    CompiledCardAbility Ability, AbilityEffect Body,
    ImmutableArray<AbilityStructuralFrame> Frames, int Ordinal);
internal sealed record DecodedEachPlayerContinuation(
    CompiledCardAbility Ability, AbilityEffect Body, ImmutableArray<AbilityStructuralFrame> Frames,
    int Ordinal, bool HasContinuation);
internal sealed record RestoredContinuationState(
    ImmutableArray<Card> Discarded, ImmutableDictionary<string, long> Results,
    int SourceIncarnation, AbilityContinuationCardBinding? Chosen, Card? Actor,
    ImmutableHashSet<int> CrisisIgnoringThwarts);

// Concrete capture facts cross the runner boundary. This deliberately contains
// values, not a Cast, callback, or runner service.
internal sealed record AbilityContinuationCapture(
    int Source, int SourceIncarnation, AbilityContinuationAddress Address,
    ImmutableArray<AbilityStructuralFrame> Frames, int Position, int Player, int AbilityPlayer,
    int AbilityActor, bool FinalStep, bool FinalPlayer, bool EachPlayerFrame,
    bool HasContinuation, string Trigger, bool SurgeGained, Occurrence Occurrence,
    ImmutableArray<int> Discarded, ImmutableDictionary<string, long> Results,
    AbilityContinuationCardBinding? Chosen);
internal sealed record ActivationWaitResult(PhaseStep Step, bool Complete);
internal abstract record AbilityContinuationTransition;
internal sealed record RunResumedNode(
    CompiledCardAbility Ability, AbilityEffect Effect, AbilityContinuationState State,
    bool EffectApplied = false)
    : AbilityContinuationTransition;
internal sealed record ContinueAfterResumedNode(
    CompiledCardAbility Ability, AbilityContinuationState State, bool EffectApplied)
    : AbilityContinuationTransition;
internal sealed record DiscardForResumedEachTime(
    CompiledCardAbility Ability, AbilityEffect.EachTime Effect,
    EachTimeFrame Frame, AbilityContinuationState State)
    : AbilityContinuationTransition;
internal sealed record RestartAfterPaidCost(CompiledCardAbility Ability, AbilityContinuationState State)
    : AbilityContinuationTransition;
internal sealed record ResumeComplete(AbilityContinuationState State) : AbilityContinuationTransition;
internal sealed record ResumeRejected(string Reason) : AbilityContinuationTransition;

/// <summary>Owns the legacy continuation wire spelling and its authored-tree validation.</summary>
internal static class AbilityContinuationCodec
{
    internal static AbilityEffect Choice(
        AbilityProgram program, Card source, AbilityType? tier, int stoppedAt,
        PhaseStep? persisted, AbilityAdmissionContext context)
    {
        if (persisted is { AbilityOrdinal: >= 0, AbilityPath: { } } step)
        {
            var decoded = Decode(program, source, step, tier);
            return AbilityInitiation.ActiveChoices(decoded.Node, context).SingleOrDefault()
                ?? throw new RulesNotImplementedException(
                    $"'{source.FaceId}' has no choice at its persisted ability path");
        }

        var written = AbilitiesOn(program, source, persisted?.AbilityFace)
            .Where(ability => tier is null || ability.Trigger.Timing == tier)
            .ToList();
        if (written.Count > 1
            && written.Count(ability => AbilityInitiation.ActiveChoices(
                ability.Effect, context).Any()) > 1)
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' has a choice in more than one '{tier}' ability, and a "
                + "suspended ability is found again from its card and its tier");

        var effect = written.Select(ability => ability.Effect)
            .FirstOrDefault(tree => AbilityInitiation.ActiveChoices(tree, context).Any())
            ?? throw new RulesNotImplementedException(
                $"'{source.FaceId}' has no choice waiting on an answer");
        if (effect.OperationName() != "seq")
            return AbilityInitiation.ActiveChoices(effect, context).Single();

        var steps = OrderedEffects(effect);
        if (stoppedAt >= 1 && stoppedAt <= steps.Length)
        {
            var nested = AbilityInitiation.ActiveChoices(
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
        var crisis = CrisisIgnoringThwartOrdinals(raw, sourceFace);
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
        var wrappers = AbilityInitiation.PowerEffects(ability.Effect, power).ToList();
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
        int ordinal = step is { What: Steps.ResolveEachPlayer, AbilityOrdinal: >= 0 }
            ? step.Value.AbilityOrdinal
            : written.FindIndex(ability => AbilityInitiation.EachPlayers(ability.Effect).Any());
        var ability = written.ElementAtOrDefault(ordinal) ?? throw new RulesNotImplementedException(
            $"'{source.FaceId}' has no reconstructable each-player ability");
        var outer = ability.Effect;
        var path = step is { What: Steps.ResolveEachPlayer, AbilityPath: { } saved }
            ? saved : outer.OperationName() == "seq" ? [$"seq:{stoppedAt - 1}"] : [];
        var each = NodeAtPath(outer, path);
        if (each is not AbilityEffect.EachPlayer eachPlayer)
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' has no each-player frame at step {stoppedAt - 1}");
        var frames = FramesAtPath(outer, path, eachPlayerFrame: true, finalPlayer)
            .Add(new EachPlayerFrame(player, finalPlayer));
        bool hasContinuation = finalPlayer && (step?.AbilityHasContinuation
            ?? (outer.OperationName() == "seq" && stoppedAt < OrderedEffects(outer).Length));
        return new(ability, eachPlayer.Effect, frames, ordinal, hasContinuation);
    }
    internal const string PersistedChosen = "__continuation.chosen";
    internal const string PersistedChosenArea = "__continuation.chosen_area";
    internal const string PersistedChosenIncarnation = "__continuation.chosen_incarnation";
    internal const string PersistedSourceIncarnation = "__continuation.source_incarnation";
    private const string CrisisIgnoringThwartPrefix =
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
        var nodes = AbilityInitiation.PowerEffects(
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
    /// Consumes one-shot wire markers before the runner can execute a node that
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
        AbilityStructuralContext context)
    {
        string face = string.IsNullOrEmpty(step?.AbilityFace) ? source.FaceId : step.Value.AbilityFace;
        var written = AbilitiesOn(program, source, face)
            .Where(ability => tier is null || ability.Trigger.Timing == tier).ToList();
        var ability = written.FirstOrDefault(candidate =>
                AbilityInitiation.Choices(candidate.Effect).Any())
            ?? throw new RulesNotImplementedException(
                $"'{source.FaceId}' has no choice waiting on an answer");
        int ordinal = written.IndexOf(ability);
        var restored = RestoreState(
            context.Expressions.World.Cards, step?.Discarded, step?.AbilityResults,
            step?.AbilityActor ?? -1, source.FaceId);
        ImmutableArray<AbilityStructuralFrame> frames = ability.Effect is AbilityEffect.Sequence sequence
            && (!eachPlayerFrame || finalPlayer)
                ? [new SequenceFrame(from, sequence.Effects.Length)]
                : [];
        var state = new AbilityContinuationState(
            new(face, tier, ordinal), frames, from,
            step?.Seat ?? context.Player, step?.AbilityPlayer ?? context.Player,
            step?.AbilityActor ?? -1, step?.FinalStep ?? false, finalPlayer,
            eachPlayerFrame, !frames.IsEmpty, step?.Trigger ?? "", step?.SurgeGained ?? false,
            step?.AbilityOccurrence ?? new Occurrence(
                0, [step?.Trigger ?? Steps.ChooseOption], Subject: source.ObjectId,
                Player: step?.Seat ?? context.Player),
            restored.Discarded.Select(card => card.ObjectId).ToImmutableArray(),
            restored.Results,
            new(source.ObjectId, source.Area.Id, source.Incarnation), restored.Chosen,
            restored.CrisisIgnoringThwarts, [], AbilityResumeReason.Choice);
        return frames.IsEmpty
            ? new ResumeComplete(state with { HasContinuation = false })
            : Advance(context, ability, state, new AbilityStructuralObservation(false));
    }

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
        AbilityContinuationState state, AbilityStructuralObservation observation)
    {
        if (observation.Suspended)
            return new ResumeComplete(state);

        var frames = state.Frames;
        while (!frames.IsEmpty)
        {
            var frame = frames[^1];
            var prefix = frames.RemoveAt(frames.Length - 1);
            if (frame is EachPlayerFrame && state.EachPlayerFrame && !state.FinalPlayer)
                return new ResumeComplete(state with
                {
                    Frames = prefix,
                    HasContinuation = true,
                });
            var parent = NodeAtTypedPath(ability.Effect, prefix);
            var structural = context with
            {
                Position = state.Position,
                HasContinuation = HasRemaining(prefix),
                Frames = prefix,
            };
            AbilityStructuralTransition transition = frame switch
            {
                SequenceFrame sequence when parent is AbilityEffect.Sequence node =>
                    AbilityStructuralExecution.NextSequence(
                        structural, node, sequence, observation),
                SimultaneousFrame simultaneous when parent is AbilityEffect.Simultaneous node =>
                    AbilityStructuralExecution.NextSimultaneous(
                        structural, node, simultaneous, observation),
                DependentFrame dependent when parent is AbilityEffect.Dependent node =>
                    AbilityStructuralExecution.AfterDependentLeaf(
                        structural, node, dependent, observation),
                ForEachFrame repeated when parent is AbilityEffect.ForEach node =>
                    AbilityStructuralExecution.NextForEach(
                        structural, node, repeated, observation),
                EachTimeFrame repeated when parent is AbilityEffect.EachTime node =>
                    AbilityStructuralExecution.NextEachTime(
                        structural, node, repeated, observation),
                ConditionalFrame or ChoiceFrame or ChoiceOtherwiseFrame
                    or DefenseFrame or EachPlayerFrame =>
                    new Complete(prefix),
                _ => new Rejected(
                    $"ability continuation frame '{frame.GetType().Name}' does not match its authored parent"),
            };

            switch (transition)
            {
                case RunLeaf leaf:
                    return new RunResumedNode(ability, leaf.Effect, state with
                    {
                        Frames = leaf.Frames,
                        Position = leaf.Position,
                        HasContinuation = leaf.HasContinuation,
                    });
                case DiscardEachTime discard when parent is AbilityEffect.EachTime repeated:
                    return new DiscardForResumedEachTime(
                        ability, repeated, discard.Frame,
                        state with { Frames = prefix, HasContinuation = structural.HasContinuation });
                case Complete:
                    frames = prefix;
                    state = state with
                    {
                        Frames = prefix,
                        HasContinuation = HasRemaining(prefix),
                        Player = frame is EachPlayerFrame && state.AbilityPlayer >= 0
                            ? state.AbilityPlayer
                            : state.Player,
                    };
                    observation = new AbilityStructuralObservation(false);
                    continue;
                case Rejected rejected:
                    return new ResumeRejected(rejected.Reason);
                case Unsupported unsupported:
                    return new ResumeRejected(unsupported.Reason);
                default:
                    return new ResumeRejected(
                        $"structural continuation returned {transition.GetType().Name}");
            }
        }
        return new ResumeComplete(state with { Frames = [], HasContinuation = false });
    }

    internal static AbilityContinuationTransition AfterEachTimeDiscard(
        AbilityStructuralContext context, DiscardForResumedEachTime command,
        Card? discarded)
    {
        var transition = AbilityStructuralExecution.AfterEachTimeDiscard(
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

    private static ImmutableDictionary<string, long> PersistResults(
        IReadOnlyDictionary<string, long> values, int sourceIncarnation,
        AbilityContinuationCardBinding? chosen, IEnumerable<int> crisisIgnoringThwarts)
    {
        var persisted = values.ToImmutableDictionary(StringComparer.Ordinal)
            .SetItem(PersistedSourceIncarnation, sourceIncarnation);
        persisted = chosen is null
            ? persisted.Remove(PersistedChosen).Remove(PersistedChosenArea)
                .Remove(PersistedChosenIncarnation)
            : persisted.SetItem(PersistedChosen, chosen.ObjectId)
                .SetItem(PersistedChosenArea, chosen.AreaId)
                .SetItem(PersistedChosenIncarnation, chosen.Incarnation);
        foreach (int ordinal in crisisIgnoringThwarts.Order())
            persisted = persisted.SetItem($"{CrisisIgnoringThwartPrefix}{ordinal}", 1);
        return persisted;
    }

    private static ImmutableHashSet<int> CrisisIgnoringThwartOrdinals(
        IReadOnlyDictionary<string, long> values, string sourceFace)
    {
        var found = ImmutableHashSet.CreateBuilder<int>();
        foreach (var (name, value) in values)
        {
            if (!name.StartsWith(CrisisIgnoringThwartPrefix, StringComparison.Ordinal))
                continue;
            string encoded = name[CrisisIgnoringThwartPrefix.Length..];
            if (value != 1 || !int.TryParse(
                    encoded, NumberStyles.None, CultureInfo.InvariantCulture,
                    out int ordinal) || ordinal < 0)
                throw new RulesNotImplementedException(
                    $"'{sourceFace}' has invalid persisted thwart target metadata");
            found.Add(ordinal);
        }
        return found.ToImmutable();
    }
    internal static string EncodeFrame(AbilityStructuralFrame frame) => frame switch
    {
        SequenceFrame sequence => $"seq:{sequence.Next - 1}",
        SimultaneousFrame simultaneous when simultaneous.Current >= 0 =>
            $"and:{simultaneous.Current}:{string.Join(',', simultaneous.Remaining)}:{string.Join(',', simultaneous.Completed)}",
        ConditionalFrame conditional => conditional.Then ? "if:then" : "if:else",
        DependentFrame dependent when dependent.Predecessor =>
            $"{(dependent.OnFull ? "then" : "otherwise")}:effect:{(dependent.Outcome?.ToString() ?? "Pending")}",
        DependentFrame dependent =>
            $"{(dependent.OnFull ? "then" : "otherwise")}:{(dependent.OnFull ? "then" : "otherwise")}",
        ForEachFrame repeated => $"forEach:{repeated.Next - 1}:{repeated.Count}",
        EachTimeFrame repeated when repeated.DiscardedCard is { } card =>
            $"eachTime:{repeated.Next - 1}:{repeated.Count}:{card}",
        ChoiceFrame { Option: null } => "choice:effect",
        ChoiceFrame { Option: { } option } => $"choice:option:{option}",
        ChoiceOtherwiseFrame => "choice:otherwise",
        DefenseFrame => "defense:effect",
        EachPlayerFrame => "eachPlayer:effect",
        _ => throw new InvalidOperationException($"No legacy continuation encoding exists for {frame.GetType().Name}"),
    };

    internal static AbilityContinuationWire Encode(PhaseStep step) => new(
        step.AbilityOrdinal, step.AbilityPath?.ToImmutableArray() ?? [],
        step.AbilityActivationIds?.ToImmutableArray() ?? [],
        (step.AbilityResults ?? ImmutableDictionary<string, long>.Empty).ToImmutableDictionary(StringComparer.Ordinal),
        step.AbilityFace, step.AbilityPlayer, step.AbilityActor, step.AbilityOccurrence,
        step.FinalStep, step.FinalPlayer, step.EachPlayerFrame, step.AbilityHasContinuation,
        step.Trigger, step.SurgeGained, step.Discarded?.ToImmutableArray() ?? []);

    internal static PhaseStep? ContinuationStep(
        PhaseStep? current, IReadOnlyList<PhaseStep> outstanding, int source, int stoppedAt,
        AbilityType? tier)
    {
        bool Matches(PhaseStep step) => step.What == Steps.ChooseOption && step.Subject == source
            && step.Index == stoppedAt && step.Tier == tier;
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

    internal static DecodedAbilityContinuation Decode(
        AbilityProgram program, Card source, PhaseStep step, AbilityType? tier)
    {
        if (step.Subject != source.ObjectId)
            throw new RulesNotImplementedException($"'{source.FaceId}' continuation has a different source card");
        if (step.AbilityOrdinal < 0 || step.AbilityPath is null)
            throw new RulesNotImplementedException($"'{source.FaceId}' continuation has no structural address");

        var face = string.IsNullOrEmpty(step.AbilityFace) ? source.FaceId : step.AbilityFace;
        var ability = AbilityAt(program, source, tier, step.AbilityOrdinal, face);
        var decoded = DecodePath(ability.Effect, step.AbilityPath, step.EachPlayerFrame, step.FinalPlayer);
        var results = (step.AbilityResults ?? ImmutableDictionary<string, long>.Empty)
            .ToImmutableDictionary(StringComparer.Ordinal);
        var crisis = CrisisIgnoringThwartOrdinals(results, source.FaceId);
        var state = new AbilityContinuationState(
            new(face, tier, step.AbilityOrdinal), decoded.Frames, step.Index, step.Seat,
            step.AbilityPlayer, step.AbilityActor, step.FinalStep, step.FinalPlayer,
            step.EachPlayerFrame, step.AbilityHasContinuation, step.Trigger, step.SurgeGained,
            step.AbilityOccurrence, step.Discarded?.ToImmutableArray() ?? [], results,
            new AbilityContinuationCardBinding(source.ObjectId, source.Area.Id, source.Incarnation), null, crisis,
            step.AbilityActivationIds?.ToImmutableArray() ?? [], AbilityResumeReason.Choice);
        return new(state, ability, decoded.Node, new AbilityContinuationFacts(true, decoded.Facts));
    }


    internal static int? EachTimeCard(IReadOnlyList<string> path)
    {
        var frame = path.LastOrDefault(value => value.StartsWith("eachTime:", StringComparison.Ordinal));
        if (frame is null) return null;
        var parts = frame.Split(':');
        return ParseEachTimeCard(parts, frame);
    }

    internal static AbilityEffect NodeAtPath(AbilityEffect root, IReadOnlyList<string> path) =>
        DecodePath(root, path, false, false).Node;

    internal static ImmutableArray<AbilityStructuralFrame> FramesAtPath(
        AbilityEffect root, IReadOnlyList<string> path,
        bool eachPlayerFrame = false, bool finalPlayer = false) =>
        DecodePath(root, path, eachPlayerFrame, finalPlayer).Frames;

    internal static AbilityEffect? TryNodeAtPath(AbilityEffect root, IReadOnlyList<string> path)
    {
        try { return NodeAtPath(root, path); }
        catch (Exception error) when (error is AbilityException or ArgumentOutOfRangeException
            or InvalidOperationException or RulesNotImplementedException) { return null; }
    }

    internal static ImmutableArray<int> ValidRemaining(AbilityEffect node, string frame) =>
        ValidRemaining(node, frame.Split(':'), frame).ToImmutableArray();
    internal static int ParseIndex(string[] parts, string frame, int position = 1) =>
        parts.Length > position && int.TryParse(parts[position], NumberStyles.None,
            CultureInfo.InvariantCulture, out int value) ? value : throw new RulesNotImplementedException(
                $"ability continuation frame '{frame}' has no valid index");
    internal static long ParseForEachCount(string[] parts, string frame) =>
        parts.Length >= 3 && long.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out long count) && count >= 0
            ? count : throw new RulesNotImplementedException($"ability continuation frame '{frame}' has no iteration count");
    internal static int ParseEachTimeCard(string[] parts, string frame) =>
        parts.Length >= 4 && int.TryParse(parts[3], NumberStyles.None, CultureInfo.InvariantCulture, out int card) && card >= 0
            ? card : throw new RulesNotImplementedException($"ability continuation frame '{frame}' has no bound card");
    internal static ImmutableArray<int> Completed(string[] parts, string frame) =>
        (parts.Length < 4
            ? throw new RulesNotImplementedException($"ability continuation frame '{frame}' has no completed order")
            : OrderPart(parts, 3, frame)).ToImmutableArray();
    internal static bool DependentContinues(string[] parts, string frame, bool onFull) =>
        ParseOutcome(parts.Length >= 3 ? parts[2] : string.Empty, frame)
        == (onFull ? AbilityStructuralOutcome.Full : AbilityStructuralOutcome.None);

    private static (AbilityEffect Node, ImmutableArray<AbilityStructuralFrame> Frames,
        ImmutableArray<AbilityContinuationFrame> Facts) DecodePath(
        AbilityEffect root, IReadOnlyList<string> path, bool eachPlayerFrame, bool finalPlayer)
    {
        try
        {
            var node = root;
            var frames = ImmutableArray.CreateBuilder<AbilityStructuralFrame>();
            var facts = ImmutableArray.CreateBuilder<AbilityContinuationFrame>();
            foreach (var encoded in path)
            {
                var parts = encoded.Split(':');
                switch (parts[0])
                {
                    case "seq":
                    {
                        var sequence = node as AbilityEffect.Sequence ?? throw Invalid(encoded);
                        int index = ParseIndex(parts, encoded);
                        if (index < 0 || index >= sequence.Effects.Length) throw Invalid(encoded);
                        frames.Add(new SequenceFrame(index + 1, sequence.Effects.Length));
                        facts.Add(new SequenceContinuationFrame(sequence, index));
                        node = sequence.Effects[index]; break;
                    }
                    case "and":
                    {
                        var simultaneous = node as AbilityEffect.Simultaneous ?? throw Invalid(encoded);
                        int current = ParseIndex(parts, encoded);
                        var remaining = ValidRemaining(node, parts, encoded);
                        var completed = Completed(parts, encoded);
                        frames.Add(new SimultaneousFrame(current, [.. remaining], [.. completed]));
                        facts.Add(new SimultaneousContinuationFrame(simultaneous, [.. remaining]));
                        node = simultaneous.Effects.ElementAt(current); break;
                    }
                    case "if":
                    {
                        bool then = parts.Length == 2 && parts[1] is "then" or "else" ? parts[1] == "then" : throw Invalid(encoded);
                        var conditional = node as AbilityEffect.Conditional ?? throw Invalid(encoded);
                        frames.Add(new ConditionalFrame(then)); node = ConditionalBranch(conditional, then ? "then" : "else") ?? throw Invalid(encoded); break;
                    }
                    case "then" or "otherwise":
                    {
                        var dependent = node as AbilityEffect.Dependent ?? throw Invalid(encoded);
                        bool onFull = parts[0] == "then";
                        if (parts.Length >= 2 && parts[1] == "effect")
                        {
                            AbilityStructuralOutcome? outcome = parts.Length == 3
                                ? ParseOptionalOutcome(parts[2], encoded) : throw Invalid(encoded);
                            frames.Add(new DependentFrame(onFull, true, outcome));
                            facts.Add(new DependentContinuationFrame(dependent, true, outcome));
                            node = dependent.Effect;
                        }
                        else if (parts.Length == 2 && parts[1] == parts[0])
                        {
                            frames.Add(new DependentFrame(onFull, false, null));
                            node = ContinuationChild(dependent, parts[0]);
                        }
                        else throw Invalid(encoded);
                        break;
                    }
                    case "defense":
                        frames.Add(new DefenseFrame()); node = EffectBody(node); break;
                    case "eachPlayer":
                    {
                        var each = node as AbilityEffect.EachPlayer ?? throw Invalid(encoded);
                        frames.Add(new EachPlayerFrame(-1, finalPlayer));
                        facts.Add(new EachPlayerContinuationFrame(eachPlayerFrame && !finalPlayer));
                        node = each.Effect; break;
                    }
                    case "forEach":
                    {
                        var repeated = node as AbilityEffect.ForEach ?? throw Invalid(encoded);
                        int current = ParseIndex(parts, encoded); long count = ParseForEachCount(parts, encoded);
                        if (current < 0 || current >= count) throw Invalid(encoded);
                        frames.Add(new ForEachFrame(current + 1, count)); facts.Add(new ForEachContinuationFrame(repeated, current, count)); node = repeated.Effect; break;
                    }
                    case "eachTime":
                    {
                        var repeated = node as AbilityEffect.EachTime ?? throw Invalid(encoded);
                        int current = ParseIndex(parts, encoded); long count = ParseForEachCount(parts, encoded); int card = ParseEachTimeCard(parts, encoded);
                        if (current < 0 || current >= count) throw Invalid(encoded);
                        frames.Add(new EachTimeFrame(current + 1, count, card)); facts.Add(new EachTimeContinuationFrame(repeated, current, count)); node = repeated.Then; break;
                    }
                    case "choice" when parts.Length == 3 && parts[1] == "option":
                    {
                        var options = node as AbilityEffect.Choose ?? throw Invalid(encoded); int option = ParseIndex(parts, encoded, 2);
                        if (option < 0 || option >= options.Options.Length) throw Invalid(encoded);
                        frames.Add(new ChoiceFrame(option, null)); node = options.Options[option]; break;
                    }
                    case "choice" when parts.Length == 2 && parts[1] == "effect":
                        if (node is not AbilityEffect.ChooseCard choice) throw Invalid(encoded);
                        frames.Add(new ChoiceFrame(null, null)); node = choice.Effect; break;
                    case "choice" when parts.Length == 2 && parts[1] == "otherwise":
                        frames.Add(new ChoiceOtherwiseFrame()); node = EffectFollowing(node); break;
                    default: throw new RulesNotImplementedException($"ability continuation frame '{encoded}' is not implemented");
                }
            }
            return (node, frames.ToImmutable(), facts.ToImmutable());
        }
        catch (Exception error) when (error is AbilityException or ArgumentOutOfRangeException
            or IndexOutOfRangeException or InvalidOperationException or InvalidCastException
            or FormatException or RulesNotImplementedException)
        {
            throw new RulesNotImplementedException($"ability continuation path '{string.Join("/", path)}' is invalid");
        }
    }

    private static AbilityStructuralOutcome ParseOutcome(string value, string frame) =>
        Enum.TryParse<AbilityStructuralOutcome>(value, out var outcome) ? outcome
        : throw new RulesNotImplementedException($"ability continuation frame '{frame}' has no resolution outcome");
    private static AbilityStructuralOutcome? ParseOptionalOutcome(string value, string frame) =>
        string.Equals(value, "Pending", StringComparison.Ordinal) ? null : ParseOutcome(value, frame);
    private static RulesNotImplementedException Invalid(string frame) => new($"ability continuation frame '{frame}' is invalid");
    private static bool HasRemaining(ImmutableArray<AbilityStructuralFrame> frames) =>
        frames.Any(frame => frame switch
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

    private static AbilityEffect NodeAtTypedPath(
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
                EachPlayerFrame when node is AbilityEffect.EachPlayer parent => parent.Effect,
                _ => throw Invalid(frame.ToString()!),
            };
        }
        return node;
    }

    private static AbilityEffect? TryNodeAtTypedPath(
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
    private static List<int> ValidRemaining(AbilityEffect node, string[] parts, string frame)
    {
        var effects = OrderedEffects(node); var remaining = OrderPart(parts, 2, frame); var completed = Completed(parts, frame);
        var order = completed.Append(ParseIndex(parts, frame)).Concat(remaining).ToList();
        if (order.Count != effects.Length || order.Distinct().Count() != effects.Length || order.Any(index => index < 0 || index >= effects.Length))
            throw new RulesNotImplementedException($"ability continuation frame '{frame}' has an invalid remaining order");
        return remaining;
    }
    private static List<int> OrderPart(string[] parts, int position, string frame)
    {
        if (parts.Length <= position || string.IsNullOrEmpty(parts[position])) return [];
        try { return parts[position].Split(',').Select(value => int.Parse(value, CultureInfo.InvariantCulture)).ToList(); }
        catch (Exception error) when (error is FormatException or OverflowException) { throw new RulesNotImplementedException($"ability continuation frame '{frame}' has an invalid remaining order"); }
    }
}
