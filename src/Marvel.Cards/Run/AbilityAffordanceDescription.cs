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
        TargetRequest? recipients = AbilityRecipientCommitment.Targets(execution, world, card, found, ability.Player, ability.TriggeringDefeat);
        string? contextual = AbilityReplacementDescription.Caption(execution, world, card, found, ability.Player);
        string? caption = Caption(world, found, ability, contextual);
        return new Affordance(
            Id: ability.OfferId ?? ability.Card,
            Verb: found.Name,
            AnchorId: ability.Card,
            AnchorPlayer: ability.Player,
            Label: found.Name,
            Targets: recipients ?? AbilityCostSelection.Ask(world, ability.Player, found.Cost),
            Costs: price is null ? null : [price], Description: Description(found.Effect, contextual))
        {
            Timing = found.Trigger.Timing,
            WindowChoices = AbilityWindowChoiceDescription.Describe(execution, world, card, found, ability.Player, ability.TriggeringDefeat),
            DisplayLabel = caption,
            CommitLabel = caption,
            PlaysCard = EffectiveCards.Kind(card, world.Facts) == CardKind.Event,
            DeferredTargetSelection = recipients is null && StartsWithTargetSelection(found.Effect),
            DirectRecipientCommit = CanCommitRecipient(recipients, EffectiveCards.Kind(card, world.Facts), found.Trigger.Timing),
            CostDescription = AbilityCostDescription.Summary(world, card, ability.Player, found.Cost),
        };
    }

    private static string? Caption(World world, CompiledCardAbility found,
        PendingAbility ability, string? contextual)
    {
        string? caption = contextual ?? AbilitySourceActionDescription.Caption(found.Effect)
            ?? AbilitySearchDescription.Action(found.Effect);
        return ability.TriggeringDefeat is { } defeat && world.Windows.Current?.Occurrence.Defeats.Count > 1
            ? $"{caption ?? found.Name} after {defeat.Title ?? EffectiveCards.Title(world.Cards[defeat.Card], world.Facts)} is defeated"
            : caption;
    }

    private static string? Description(AbilityEffect effect, string? contextual) => contextual is not null
        ? contextual + ". Damage prevention and interrupts can change the result."
        : AbilityEffectDescription.Summary(effect) ?? AbilitySourceActionDescription.Caption(effect);

    private static bool CanCommitRecipient(TargetRequest? recipients, CardKind kind, AbilityType timing) =>
        recipients is not null && (kind != CardKind.Event
            || timing is AbilityType.Interrupt or AbilityType.Response);

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
