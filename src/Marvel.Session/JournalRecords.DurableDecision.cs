using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Session;

/// <summary>Canonical JSON settings for deterministic journal value records.</summary>

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
