using Marvel.Cards.Dsl;
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
            DisplayLabel = AbilitySearchDescription.Action(found.Effect),
            CommitLabel = AbilitySearchDescription.Action(found.Effect),
            PlaysCard = EffectiveCards.Kind(card, world.Facts) == CardKind.Event,
            DeferredTargetSelection = StartsWithTargetSelection(found.Effect),
            CostDescription = AbilityCostDescription.Summary(world, card, ability.Player, found.Cost),
        };
    }

    // Admission has already checked the effect's targets. This marker describes
    // its choice boundary, not a future legal set: payment and intervening
    // windows can still change whether the resolution reaches that choice.
    // Do not descend into conditional, optional or power-cancellable branches.
    private static bool StartsWithTargetSelection(AbilityEffect effect) => effect switch
    {
        AbilityEffect.ChooseCard => true,
        AbilityEffect.Sequence { Effects.Length: 1 } sequence =>
            StartsWithTargetSelection(sequence.Effects[0]),
        _ => false,
    };

}
