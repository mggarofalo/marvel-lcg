using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Matches a resource ability's printed restriction to the actual cost's card.</summary>
public static class ResourceSourceEligibility
{
    /// <summary>Unrestricted sources can pay any cost; restricted sources require its card context.</summary>
    public static bool Allows(World world, ICardFacts facts, ResourceSource source, Card? target) =>
        source.Restriction switch
        {
            ResourceRestriction.None => true,
            ResourceRestriction.ArrowEvent => target is not null
                && EffectiveCards.Kind(target, facts) == CardKind.Event
                && Traits.Of(world, target, facts).Contains("ARROW", StringComparer.Ordinal),
            // ruling:6800619dc41a043c includes a cost within an aspect card's ability.
            ResourceRestriction.AspectCard => target is not null
                && facts.Attributes(target.FaceId).GetValueOrDefault("Class") is
                    "Aggression" or "Justice" or "Leadership" or "Protection" or "'Pool",
            _ => throw new RulesNotImplementedException("unknown resource spending restriction"),
        };
}
