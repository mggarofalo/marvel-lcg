using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Session;

/// <summary>Canonical JSON settings for deterministic journal value records.</summary>

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
            ValidateDecline(actor, prompt, targets, resources, values, allocations);
            return Decision.Decline;
        }
        Affordance selected = SelectAffordance(prompt);
        ValidateActor(actor, prompt, selected);
        ValidateTargets(selected, targets);
        ValidateValues(selected, values);
        ValidatePayment(selected, resources, values, allocations);
        return Decision.Take(selected.Id, targets, resources, values, allocations);
    }

    private Affordance SelectAffordance(Prompt prompt)
    {
        List<Affordance> exact = prompt.Affordances.Where(option =>
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

        return exact[Occurrence];
    }

    private static void ValidateActor(int actor, Prompt prompt, Affordance selected)
    {
        int expectedActor = ActingSeat(prompt, selected);
        if (actor != expectedActor)
        {
            throw new ReplayDivergenceException(
                $"'{selected.Label}' belongs to player {expectedActor}, not recorded "
                + $"player {actor}");
        }
    }

    private static void ValidateTargets(Affordance selected, IReadOnlyList<int> targets)
    {
        bool allowed = selected.Targets is null
            ? targets.Count == 0
            : selected.Targets.Allows(targets);
        if (!allowed)
        {
            throw new ReplayDivergenceException(
                $"recorded targets are not allowed by '{selected.Label}'");
        }
    }

    private static void ValidateValues(
        Affordance selected, IReadOnlyDictionary<string, long> values)
    {
        Dictionary<string, VariableRequest> requested = selected.CostOptions
            .SelectMany(cost => cost.VariableRequests)
            .ToDictionary(variable => variable.Name, StringComparer.Ordinal);
        if (values.Count != requested.Count
            || values.Any(entry => !requested.TryGetValue(entry.Key, out var variable)
                || !variable.Allows(entry.Value)))
        {
            throw new ReplayDivergenceException(
                $"recorded variables are not allowed by '{selected.Label}'");
        }
    }

    private static void ValidatePayment(
        Affordance selected,
        IReadOnlyList<int> resources,
        IReadOnlyDictionary<string, long> values,
        IReadOnlyList<ResourceAllocation> allocations)
    {
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
    }

    private static void ValidateDecline(
        int actor,
        Prompt prompt,
        IReadOnlyList<int> targets,
        IReadOnlyList<int> resources,
        IReadOnlyDictionary<string, long> values,
        IReadOnlyList<ResourceAllocation> allocations)
    {
        if (actor != prompt.Player)
        {
            throw new ReplayDivergenceException(
                $"prompt '{prompt.Label}' belongs to player {prompt.Player}, not recorded player {actor}");
        }
        if (!prompt.Cancellable)
        {
            throw new ReplayDivergenceException($"prompt '{prompt.Label}' cannot be declined");
        }
        if (targets.Count > 0 || resources.Count > 0 || values.Count > 0 || allocations.Count > 0)
        {
            throw new ReplayDivergenceException(
                $"decline at prompt '{prompt.Label}' records answer data");
        }
    }

    internal static int ActingSeat(Prompt prompt, Affordance selected) =>
        string.Equals(selected.Verb, Game.ActionVerb, StringComparison.Ordinal)
            && selected.AnchorPlayer >= 0
            ? selected.AnchorPlayer
            : prompt.Player;
}
