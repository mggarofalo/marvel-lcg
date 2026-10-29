using Marvel.Cards.Dsl;
using Marvel.Rules.Prompts;
using Marvel.Rules.Play;
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
        TargetRequest? recipients = AbilityRecipientCommitment.Targets(execution, world, card, found, ability.Player, ability);
        string? contextual = AbilityReplacementDescription.Caption(execution, world, card, found, ability.Player);
        string? entry = AbilityEntryCounterDescription.Summary(execution, world, card, found, ability.Player);
        string? caption = AbilityEntryCounterDescription.Caption(found.Effect, entry)
            ?? Caption(world, found, ability, contextual);
        return new Affordance(
            Id: ability.OfferId ?? ability.Card,
            Verb: found.Name,
            AnchorId: ability.Card,
            AnchorPlayer: ability.Player,
            Label: found.Name,
            Targets: recipients ?? AdmittedCostTargets(execution, world, card, found, ability.Player),
            Costs: price is null ? null : [price], Description: Description(execution, world, card, ability, found.Effect, contextual, entry))
        {
            Timing = found.Trigger.Timing,
            ContextCardIds = ContextCards(world, ability),
            WindowChoices = AbilityWindowChoiceDescription.Describe(execution, world, card, found, ability.Player, ability),
            DisplayLabel = caption,
            CommitLabel = CommitCaption(found.Effect, caption, world, card),
            PlaysCard = EffectiveCards.Kind(card, world.Facts) == CardKind.Event,
            DeferredTargetSelection = recipients is null && StartsWithTargetSelection(found.Effect),
            DirectRecipientCommit = CanCommitRecipient(recipients, EffectiveCards.Kind(card, world.Facts), found.Trigger.Timing),
            CostDescription = AbilityCostDescription.Summary(world, card, ability.Player, found.Cost),
        };
    }

    private static string? CommitCaption(AbilityEffect effect, string? caption, World world, Card source) =>
        effect is AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.PlaySharedHeroTraitCard }
            ? $"Play a matching card with {EffectiveCards.Title(source, world.Facts)}" : caption;

    private static int[] ContextCards(World world, PendingAbility ability)
    {
        if (ability.TriggeringWouldDefeat is { } imminent) return [imminent];
        if (ability.TriggeringAttack is { } attack)
            return world.Cards[attack.Card].Incarnation == attack.Incarnation ? [attack.Card] : [];
        return ability.TriggeringDefeat is { } defeat ? [defeat.Card] : [];
    }

    private static TargetRequest? AdmittedCostTargets(AbilityResolutionExecution execution,
        World world, Card card, CompiledCardAbility ability, int player)
    {
        var targets = AbilityCostSelection.Ask(world, card, player, ability.Cost);
        if (ability.Cost is not AbilityCost.ChooseScheme || targets is null) return targets;
        // rr:target.2: initiation requires a valid target. Reuse the same
        // chosen-scheme admission as execution, including status cancellation.
        return targets with { Legal = [.. targets.Legal.Where(id =>
            execution.offerQueries.ActionAdmission(world, card, ability, player,
                world.Windows.Current?.Occurrence, [id]) is not null)] };
    }

    private static string? Caption(World world, CompiledCardAbility found,
        PendingAbility ability, string? contextual)
    {
        string? caption = contextual ?? (found.Effect is AbilityEffect.Fixed
            { Instruction: AbilityFixedInstruction.PlaySharedHeroTraitCard }
                ? "Play a matching card" : null)
            ?? AbilitySourceActionDescription.Caption(found.Effect)
            ?? AbilitySearchDescription.Action(found.Effect);
        if (ability.TriggeringWouldDefeat is { } imminent)
            return $"{caption ?? found.Name} before {EffectiveCards.Title(world.Cards[imminent], world.Facts)} would be defeated";
        if (ability.TriggeringThreat is { } threat && threat.Target >= 0)
            return $"{caption ?? found.Name} after threat is placed on {threat.TargetFacts?.EffectiveTitle ?? EffectiveCards.Title(world.Cards[threat.Target], world.Facts)}";
        if (ability.TriggeringAttack is { } attack)
            return $"{caption ?? found.Name} after attacking {attack.Title ?? "an enemy"}";
        return DefeatCaption(world, ability, caption, found.Name);
    }

    private static string? DefeatCaption(World world, PendingAbility ability, string? caption, string name) =>
        ability.TriggeringDefeat is { } defeat && world.Windows.Current?.Occurrence.Defeats.Count > 1
            ? $"{caption ?? name} after {defeat.Title ?? EffectiveCards.Title(world.Cards[defeat.Card], world.Facts)} is defeated"
            : caption;

    private static string? Description(AbilityResolutionExecution execution, World world, Card source,
        PendingAbility ability, AbilityEffect effect, string? contextual, string? entry) =>
        entry ??
        AbilitySourceThreatDescription.Summary(execution.StructuralContext(
            new AbilityResolutionState(world, source, new Occurrence(0, [Steps.TurnAction],
                Subject: source.ObjectId, Player: ability.Player), ability.Player, [])), effect)
        ?? AbilityAttackHealingDescription.Summary(effect, ability.TriggeringAttack)
        ?? AbilityInvocationDescription.Summary(execution, world, ability.Player, effect)
        ?? (contextual is not null
            ? contextual + ". Damage prevention and interrupts can change the result."
            : AbilityEffectDescription.Summary(effect) ?? AbilitySourceActionDescription.Caption(effect));

    private static bool CanCommitRecipient(TargetRequest? recipients, CardKind kind, AbilityType timing) =>
        recipients is not null && (kind != CardKind.Event
            || timing is AbilityType.Interrupt or AbilityType.Response);

    // Admission has already checked the effect's targets. This marker describes
    // its choice boundary, not a future legal set: payment and intervening
    // windows can still change whether the resolution reaches that choice.
    // Do not descend into conditional, optional or power-cancellable branches.
    private static bool StartsWithTargetSelection(AbilityEffect effect) => effect switch
    {
        AbilityEffect.ChooseCard or AbilityEffect.Damage { Maximum: not null } => true,
        AbilityEffect.Sequence { Effects.Length: 1 } sequence =>
            StartsWithTargetSelection(sequence.Effects[0]),
        _ => false,
    };

}
