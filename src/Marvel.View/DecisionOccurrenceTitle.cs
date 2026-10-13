using Marvel.Rules.Play;

namespace Marvel.View;

/// <summary>Names a committed basic action without repeating its acting seat and source.</summary>
internal static class DecisionOccurrenceTitle
{
    internal static string? From(AffordancePresentation offer, IReadOnlyList<int> targets, WorldDescriptor world)
    {
        if (targets.Count != 1) return null;
        string? verb = offer.Verb switch
        {
            BasicPowers.AttackVerb => "attacked",
            BasicPowers.ThwartVerb => "thwarted",
            _ => null,
        };
        return verb is null ? null
            : $"{offer.SourceName ?? offer.Anchor} {verb} {PromptPresentation.Describe(targets[0], world)}";
    }
}
