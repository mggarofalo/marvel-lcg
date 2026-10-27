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
            // Printed 08007 generates a resource "for a Preparation card".
            // Matching ability costs follow the Finesse/event-cost interpretation
            // of rulings 6800619dc41a043c and 5b44b2adcc60d4b7 (see card-dsl.md).
            ResourceRestriction.PreparationCard => target is not null
                && Traits.Has(world, target, "PREPARATION", facts),
            ResourceRestriction.Event => target is not null
                && EffectiveCards.Kind(target, facts) == CardKind.Event,
            ResourceRestriction.AttackEvent => target is not null
                && EffectiveCards.Kind(target, facts) == CardKind.Event
                && Traits.Has(world, target, "ATTACK", facts),
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
