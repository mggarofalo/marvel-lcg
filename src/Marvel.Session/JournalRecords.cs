using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Session;

/// <summary>Canonical JSON settings for deterministic journal value records.</summary>
public static class JournalJson
{
    /// <summary>Uses the snake-case spelling already pinned by simulation schema 2.</summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false,
        };
        options.Converters.Add(new ResourceAllocationJsonConverter());
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }

    /// <summary>Captures one semantic event without changing its wire shape.</summary>
    public static JsonElement Event(GameEvent happened)
    {
        ArgumentNullException.ThrowIfNull(happened);
        return JsonSerializer.SerializeToElement<GameEvent>(happened, EventJson.Options);
    }
}

/// <summary>Strict schema JSON for one per-cost resource allocation.</summary>
public sealed class ResourceAllocationJsonConverter : JsonConverter<ResourceAllocation>
{
    /// <inheritdoc />
    public override ResourceAllocation Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("resource allocation must be an object");
        }

        int? source = null;
        int? cost = null;
        string? paidAs = null;
        foreach (JsonProperty property in root.EnumerateObject())
        {
            switch (property.Name)
            {
                case "source" when source is null:
                    source = property.Value.GetInt32();
                    break;
                case "cost" when cost is null:
                    cost = property.Value.GetInt32();
                    break;
                case "paid_as" when paidAs is null:
                    paidAs = property.Value.GetString()
                        ?? throw new JsonException("resource allocation paid_as is null");
                    break;
                default:
                    throw new JsonException(
                        $"resource allocation member '{property.Name}' is not allowed");
            }
        }

        if (source is null || cost is null || paidAs is null)
        {
            throw new JsonException("resource allocation is missing a required member");
        }

        return new ResourceAllocation(source.Value, cost.Value, paidAs);
    }

    /// <inheritdoc />
    public override void Write(
        Utf8JsonWriter writer,
        ResourceAllocation value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("source", value.Source);
        writer.WriteNumber("cost", value.Cost);
        writer.WriteString("paid_as", value.PaidAs);
        writer.WriteEndObject();
    }
}

/// <summary>A stable, affordance-handle-free snapshot of one engine prompt.</summary>
public sealed record PromptRecord(
    [property: JsonRequired] int Player,
    [property: JsonRequired] string Asking,
    [property: JsonRequired] string When,
    [property: JsonRequired] string Trigger,
    [property: JsonRequired] string Label,
    [property: JsonRequired] bool Cancellable,
    [property: JsonRequired] IReadOnlyList<AffordanceRecord> Affordances)
{
    /// <summary>Captures every ordered prompt field except ephemeral affordance ids.</summary>
    public static PromptRecord From(Prompt prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        return new(
            prompt.Player,
            prompt.Asking.ToString(),
            prompt.When.ToString(),
            prompt.Trigger,
            prompt.Label,
            prompt.Cancellable,
            [.. prompt.Affordances.Select(AffordanceRecord.From)]);
    }
}

/// <summary>A stable snapshot of an offered choice, excluding its live handle.</summary>
public sealed record AffordanceRecord(
    [property: JsonRequired] string Verb,
    [property: JsonRequired] int AnchorId,
    [property: JsonRequired] int AnchorPlayer,
    [property: JsonRequired] string Label,
    [property: JsonRequired] JsonElement? Targets,
    [property: JsonRequired] IReadOnlyList<JsonElement> Costs,
    [property: JsonRequired] string? Illegal)
{
    /// <summary>Captures an affordance in domain order.</summary>
    public static AffordanceRecord From(Affordance affordance)
    {
        ArgumentNullException.ThrowIfNull(affordance);
        return new(
            affordance.Verb,
            affordance.AnchorId,
            affordance.AnchorPlayer,
            affordance.Label,
            affordance.Targets is null
                ? null
                : JsonSerializer.SerializeToElement(
                    affordance.Targets, JournalJson.Options),
            [.. affordance.CostOptions.Select(cost =>
                JsonSerializer.SerializeToElement(cost, JournalJson.Options))],
            affordance.Illegal);
    }
}

/// <summary>
/// A durable affordance identity chosen by the engine because tabletop rules
/// define no persistent command identifier.
/// </summary>
public sealed record DecisionSelector(
    [property: JsonRequired] bool Decline,
    [property: JsonRequired] int? AnchorId,
    [property: JsonRequired] int? AnchorPlayer,
    [property: JsonRequired] string? Verb,
    [property: JsonRequired] string? Label,
    [property: JsonRequired] int Occurrence)
{
    /// <summary>Captures a decision without retaining <see cref="Affordance.Id"/>.</summary>
    public static DecisionSelector From(Prompt prompt, Decision decision)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(decision);
        if (decision.IsDecline)
        {
            return new DecisionSelector(true, null, null, null, null, 0);
        }

        var selected = prompt.Affordances.Single(option => option.Id == decision.Affordance);
        var exact = prompt.Affordances.Where(option =>
            option.IsLegal
            && option.AnchorId == selected.AnchorId
            && option.AnchorPlayer == selected.AnchorPlayer
            && string.Equals(option.Verb, selected.Verb, StringComparison.Ordinal)
            && string.Equals(option.Label, selected.Label, StringComparison.Ordinal));
        int occurrence = exact.TakeWhile(option => option.Id != selected.Id).Count();
        return new DecisionSelector(
            false,
            selected.AnchorId,
            selected.AnchorPlayer,
            selected.Verb,
            selected.Label,
            occurrence);
    }

    /// <summary>Resolves and validates a recorded answer against a fresh prompt.</summary>
    public Decision Resolve(
        int actor,
        Prompt prompt,
        IReadOnlyList<int> targets,
        IReadOnlyList<int> resources,
        IReadOnlyDictionary<string, long> values,
        IReadOnlyList<ResourceAllocation> allocations)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(targets);
        ArgumentNullException.ThrowIfNull(resources);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(allocations);

        if (Decline)
        {
            if (actor != prompt.Player)
            {
                throw new ReplayDivergenceException(
                    $"prompt '{prompt.Label}' belongs to player {prompt.Player}, not recorded "
                    + $"player {actor}");
            }

            if (!prompt.Cancellable)
            {
                throw new ReplayDivergenceException(
                    $"prompt '{prompt.Label}' cannot be declined");
            }

            if (targets.Count > 0 || resources.Count > 0 || values.Count > 0
                || allocations.Count > 0)
            {
                throw new ReplayDivergenceException(
                    $"decline at prompt '{prompt.Label}' records answer data");
            }

            return Decision.Decline;
        }

        var exact = prompt.Affordances.Where(option =>
                option.IsLegal
                && option.AnchorId == AnchorId
                && option.AnchorPlayer == AnchorPlayer
                && string.Equals(option.Verb, Verb, StringComparison.Ordinal)
                && string.Equals(option.Label, Label, StringComparison.Ordinal))
            .ToList();
        if (Occurrence < 0 || Occurrence >= exact.Count)
        {
            throw new ReplayDivergenceException(
                $"prompt '{prompt.Label}' has {exact.Count} matching affordance(s), "
                + $"not recorded occurrence {Occurrence}");
        }

        var selected = exact[Occurrence];
        int expectedActor = ActingSeat(prompt, selected);
        if (actor != expectedActor)
        {
            throw new ReplayDivergenceException(
                $"'{selected.Label}' belongs to player {expectedActor}, not recorded "
                + $"player {actor}");
        }

        if (selected.Targets is null ? targets.Count > 0 : !selected.Targets.Allows(targets))
        {
            throw new ReplayDivergenceException(
                $"recorded targets are not allowed by '{selected.Label}'");
        }

        var requested = selected.CostOptions
            .SelectMany(cost => cost.VariableRequests)
            .ToDictionary(variable => variable.Name, StringComparer.Ordinal);
        if (values.Count != requested.Count
            || values.Any(entry => !requested.TryGetValue(entry.Key, out var variable)
                || !variable.Allows(entry.Value)))
        {
            throw new ReplayDivergenceException(
                $"recorded variables are not allowed by '{selected.Label}'");
        }

        bool paymentAllowed = selected.CostOptions.Count == 0
            ? resources.Count == 0 && allocations.Count == 0
            : selected.CostOptions.Any(cost =>
                ResourcePayment.Allows(cost, resources, values, allocations));
        if (!paymentAllowed)
        {
            throw new ReplayDivergenceException(
                $"recorded resources or allocations do not pay an offered cost for "
                + $"'{selected.Label}'");
        }

        return Decision.Take(selected.Id, targets, resources, values, allocations);
    }

    internal static int ActingSeat(Prompt prompt, Affordance selected) =>
        string.Equals(selected.Verb, Game.ActionVerb, StringComparison.Ordinal)
            && selected.AnchorPlayer >= 0
            ? selected.AnchorPlayer
            : prompt.Player;
}

/// <summary>One complete, ordered answer that can be resolved against a fresh prompt.</summary>
public sealed record DurableDecision(
    [property: JsonRequired] int Actor,
    [property: JsonRequired] DecisionSelector Selector,
    [property: JsonRequired] IReadOnlyList<int> Targets,
    [property: JsonRequired] IReadOnlyList<int> Resources,
    [property: JsonRequired] IReadOnlyDictionary<string, long> Values,
    [property: JsonRequired] IReadOnlyList<ResourceAllocation> Allocations)
{
    /// <summary>Captures the prompt-authorized actor and every ordered answer field.</summary>
    public static DurableDecision From(int actor, Prompt prompt, Decision decision)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(decision);
        return new(
            actor,
            DecisionSelector.From(prompt, decision),
            [.. decision.Targets],
            [.. decision.Spent],
            new Dictionary<string, long>(decision.DefinedValues, StringComparer.Ordinal),
            [.. decision.Allocated]);
    }

    /// <summary>Validates actor and answer before returning an engine decision.</summary>
    public Decision Resolve(Prompt prompt)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        return Selector.Resolve(Actor, prompt, Targets, Resources, Values, Allocations);
    }

    /// <summary>
    /// Derives the acting seat for a trusted simulation decision. A server
    /// instead records its authenticated capability seat explicitly.
    /// </summary>
    public static int SimulationActor(Prompt prompt, Decision decision)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(decision);
        if (decision.IsDecline)
        {
            return prompt.Player;
        }

        var selector = DecisionSelector.From(prompt, decision);
        return SimulationActor(prompt, selector);
    }

    /// <summary>Derives the acting seat from a trusted simulation selector.</summary>
    public static int SimulationActor(Prompt prompt, DecisionSelector selector)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(selector);
        if (selector.Decline)
        {
            return prompt.Player;
        }

        var exact = prompt.Affordances.Where(option =>
                option.IsLegal
                && option.AnchorId == selector.AnchorId
                && option.AnchorPlayer == selector.AnchorPlayer
                && string.Equals(option.Verb, selector.Verb, StringComparison.Ordinal)
                && string.Equals(option.Label, selector.Label, StringComparison.Ordinal))
            .ToList();
        if (selector.Occurrence < 0 || selector.Occurrence >= exact.Count)
        {
            throw new ReplayDivergenceException(
                $"prompt '{prompt.Label}' cannot derive the recorded acting seat");
        }

        return DecisionSelector.ActingSeat(prompt, exact[selector.Occurrence]);
    }
}

/// <summary>One durable answer together with the derived facts replay verifies.</summary>
public sealed record JournalStep(
    [property: JsonRequired] PromptRecord Prompt,
    [property: JsonRequired] DurableDecision Decision,
    [property: JsonRequired] IReadOnlyList<JsonElement> Events,
    [property: JsonRequired] long RngWords,
    [property: JsonRequired] string StateFingerprint,
    [property: JsonRequired] EngineResultRecord? Result = null)
{
    /// <summary>Captures engine output in event order after a resolved answer.</summary>
    public static JournalStep From(
        int actor,
        Prompt prompt,
        Decision decision,
        IReadOnlyList<GameEvent> events,
        long rngWords,
        string stateFingerprint,
        EngineResultRecord? result = null)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentException.ThrowIfNullOrEmpty(stateFingerprint);
        return new(
            PromptRecord.From(prompt),
            DurableDecision.From(actor, prompt, decision),
            [.. events.Select(JournalJson.Event)],
            rngWords,
            stateFingerprint,
            result);
    }
}

/// <summary>The terminal meaning that a state digest alone cannot express.</summary>
public sealed record EngineResultRecord(
    [property: JsonRequired] string Outcome,
    [property: JsonRequired] int Round);

/// <summary>Deterministic comparisons used by every journal replay consumer.</summary>
public static class JournalReplay
{
    /// <summary>Requires a freshly produced prompt to match its stable record.</summary>
    public static void RequirePrompt(PromptRecord expected, Prompt actual, string context)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);
        RequireEqual(
            JsonSerializer.Serialize(expected, JournalJson.Options),
            JsonSerializer.Serialize(PromptRecord.From(actual), JournalJson.Options),
            context);
    }

    /// <summary>Requires semantic events to retain their exact count, order and shape.</summary>
    public static void RequireEvents(
        IReadOnlyList<JsonElement> expected,
        IReadOnlyList<GameEvent> actual,
        string context)
    {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);
        var recorded = actual.Select(JournalJson.Event).ToList();
        if (expected.Count != recorded.Count)
        {
            throw new ReplayDivergenceException(
                $"{context} diverged: expected {expected.Count}, got {recorded.Count}");
        }

        for (int index = 0; index < expected.Count; index++)
        {
            RequireEqual(
                expected[index].GetRawText(),
                recorded[index].GetRawText(),
                $"{context}[{index}]");
        }
    }

    /// <summary>Requires the exact hidden-state fingerprint produced after a decision.</summary>
    public static void RequireFingerprint(string expected, string actual, string context) =>
        RequireEqual(expected, actual, context);

    /// <summary>Requires cumulative gameplay RNG consumption to remain exact.</summary>
    public static void RequireRng(long expected, long actual, string context) =>
        RequireEqual(expected, actual, context);

    private static void RequireEqual<T>(T expected, T actual, string context)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new ReplayDivergenceException(
                $"{context} diverged: expected '{expected}', got '{actual}'");
        }
    }
}

/// <summary>A persisted decision or derived record no longer matches engine truth.</summary>
public sealed class ReplayDivergenceException(string message) : Exception(message);
