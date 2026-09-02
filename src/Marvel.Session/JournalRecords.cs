using System.Text.Json;
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

/// <summary>A stable, affordance-handle-free snapshot of one engine prompt.</summary>
public sealed record PromptRecord(
    int Player,
    string Asking,
    string When,
    string Trigger,
    string Label,
    bool Cancellable,
    IReadOnlyList<AffordanceRecord> Affordances)
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
    string Verb,
    int AnchorId,
    int AnchorPlayer,
    string Label,
    JsonElement? Targets,
    IReadOnlyList<JsonElement> Costs,
    string? Illegal)
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
    bool Decline,
    int? AnchorId,
    int? AnchorPlayer,
    string? Verb,
    string? Label,
    int Occurrence)
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
}

/// <summary>One complete, ordered answer that can be resolved against a fresh prompt.</summary>
public sealed record DurableDecision(
    int Actor,
    DecisionSelector Selector,
    IReadOnlyList<int> Targets,
    IReadOnlyList<int> Resources,
    IReadOnlyDictionary<string, long> Values,
    IReadOnlyList<ResourceAllocation> Allocations)
{
    /// <summary>Captures the prompt-authorized actor and every ordered answer field.</summary>
    public static DurableDecision From(Prompt prompt, Decision decision)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(decision);
        return new(
            prompt.Player,
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
        if (prompt.Player != Actor)
        {
            throw new ReplayDivergenceException(
                $"prompt '{prompt.Label}' belongs to player {prompt.Player}, not recorded "
                + $"player {Actor}");
        }

        return Selector.Resolve(prompt, Targets, Resources, Values, Allocations);
    }
}

/// <summary>One durable answer together with the derived facts replay verifies.</summary>
public sealed record JournalStep(
    PromptRecord Prompt,
    DurableDecision Decision,
    IReadOnlyList<JsonElement> Events,
    string StateFingerprint)
{
    /// <summary>Captures engine output in event order after a resolved answer.</summary>
    public static JournalStep From(
        Prompt prompt,
        Decision decision,
        IReadOnlyList<GameEvent> events,
        string stateFingerprint)
    {
        ArgumentNullException.ThrowIfNull(events);
        ArgumentException.ThrowIfNullOrEmpty(stateFingerprint);
        return new(
            PromptRecord.From(prompt),
            DurableDecision.From(prompt, decision),
            [.. events.Select(JournalJson.Event)],
            stateFingerprint);
    }
}

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
