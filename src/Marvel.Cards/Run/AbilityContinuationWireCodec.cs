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
internal static class AbilityContinuationWireCodec
{
    internal static ImmutableDictionary<string, long> PersistResults(
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

    internal static ImmutableHashSet<int> CrisisIgnoringThwartOrdinals(
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
            or InvalidOperationException or RulesNotImplementedException)
        { return null; }
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

    internal static (AbilityEffect Node, ImmutableArray<AbilityStructuralFrame> Frames,
        ImmutableArray<AbilityContinuationFrame> Facts) DecodePath(
        AbilityEffect root, IReadOnlyList<string> path, bool eachPlayerFrame, bool finalPlayer)
    {
        try
        {
            return new ContinuationPathDecoder(root, eachPlayerFrame, finalPlayer)
                .Decode(path);
        }
        catch (Exception error) when (error is AbilityException or ArgumentOutOfRangeException
            or IndexOutOfRangeException or InvalidOperationException or InvalidCastException
            or FormatException or RulesNotImplementedException)
        {
            throw new RulesNotImplementedException($"ability continuation path '{string.Join("/", path)}' is invalid");
        }
    }

    internal static AbilityStructuralOutcome ParseOutcome(string value, string frame) =>
        Enum.TryParse<AbilityStructuralOutcome>(value, out var outcome) ? outcome
        : throw new RulesNotImplementedException($"ability continuation frame '{frame}' has no resolution outcome");
    internal static AbilityStructuralOutcome? ParseOptionalOutcome(string value, string frame) =>
        string.Equals(value, "Pending", StringComparison.Ordinal) ? null : ParseOutcome(value, frame);
    internal static RulesNotImplementedException Invalid(string frame) => new($"ability continuation frame '{frame}' is invalid");
    internal static bool HasRemaining(ImmutableArray<AbilityStructuralFrame> frames) =>
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
                EachPlayerFrame when node is AbilityEffect.EachPlayer parent => parent.Effect,
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
    internal static List<int> ValidRemaining(AbilityEffect node, string[] parts, string frame)
    {
        var effects = OrderedEffects(node); var remaining = OrderPart(parts, 2, frame); var completed = Completed(parts, frame);
        var order = completed.Append(ParseIndex(parts, frame)).Concat(remaining).ToList();
        if (order.Count != effects.Length || order.Distinct().Count() != effects.Length || order.Any(index => index < 0 || index >= effects.Length))
            throw new RulesNotImplementedException($"ability continuation frame '{frame}' has an invalid remaining order");
        return remaining;
    }
    internal static List<int> OrderPart(string[] parts, int position, string frame)
    {
        if (parts.Length <= position || string.IsNullOrEmpty(parts[position])) return [];
        try { return parts[position].Split(',').Select(value => int.Parse(value, CultureInfo.InvariantCulture)).ToList(); }
        catch (Exception error) when (error is FormatException or OverflowException) { throw new RulesNotImplementedException($"ability continuation frame '{frame}' has an invalid remaining order"); }
    }
}
