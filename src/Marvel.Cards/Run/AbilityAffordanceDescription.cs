using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Describes offered abilities and their engine-owned card-play intent.</summary>
internal static class AbilityAffordanceDescription
{
    /// <inheritdoc/>
    internal static Affordance Describe(this AbilityResolutionExecution execution, World world, PendingAbility ability)
    {
        ArgumentNullException.ThrowIfNull(world);

        var card = world.Cards[ability.Card];
        var found = execution.Pending(card, ability);

        // The ability's own name is the verb: an affordance for Foresight is
        // offered as `Foresight`, so a client has something to render without
        // knowing what the ability does. One string does for both fields
        // because the engine carries one -- see the remarks on `Affordance.Id`.
        var price = AbilityPaymentPricing.CombinedPrice(
            world, card, ability.Player, found, execution.resourceAbilities);
        return new Affordance(
            Id: ability.Card,
            Verb: found.Name,
            AnchorId: ability.Card,
            AnchorPlayer: ability.Player,
            Label: found.Name,
            Targets: AbilityCostSelection.Ask(world, ability.Player, found.Cost),
            Costs: price is null ? null : [price], Description: AbilityEffectDescription.Summary(found.Effect))
        {
            PlaysCard = world.Facts.Kind(card.FaceId) == CardKind.Event,
        };
    }

}
