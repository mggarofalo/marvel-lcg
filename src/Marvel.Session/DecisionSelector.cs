using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Session;

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
    /// <summary>The namespace of <see cref="AnchorId"/>, or null for a decline.</summary>
    [JsonRequired]
    public AffordanceAnchorKind? AnchorKind { get; init; }

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
            && option.AnchorKind == selected.AnchorKind
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
            occurrence)
        {
            AnchorKind = selected.AnchorKind,
        };
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

        Decision answer = Decline
            ? new Decision(-1, targets, resources, values, allocations)
            : Decision.Take(SelectAffordance(prompt).Id, targets, resources, values, allocations);
        DecisionAssessmentResult result = DecisionAssessment.Assess(prompt, answer, actor);
        if (!result.IsAccepted)
            throw new ReplayDivergenceException($"recorded decision rejected: {result.Rejection}");
        return answer;
    }

    private Affordance SelectAffordance(Prompt prompt)
    {
        List<Affordance> exact = prompt.Affordances.Where(option =>
                option.IsLegal
                && option.AnchorId == AnchorId
                && option.AnchorKind == AnchorKind
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

    internal static int ActingSeat(Prompt prompt, Affordance selected) =>
        DecisionAssessment.ActingSeat(prompt, selected);
}
