using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Admits one explicit recipient with a structurally complete activation.</summary>
internal static class AbilityRecipientCommitment
{
    // Engine-chosen continuation evidence, stored in the existing numeric results map.
    internal const string PendingRecipient = "committedSupportRecipient";

    internal static bool Resume(AbilityResolutionExecution execution,
        AbilityResolutionState cast, CompiledCardAbility ability)
    {
        if (!cast.Results.Remove(PendingRecipient, out long recipient)) return false;
        var choice = Choice(cast.World, cast.Source, ability)
            ?? throw new RulesNotImplementedException("The committed recipient has no supported effect.");
        Resolve(execution, cast, choice, checked((int)recipient));
        return true;
    }

    internal static AbilityEffect.ChooseCard? Choice(World world, Card source, CompiledCardAbility ability)
    {
        if (ability.Trigger.Timing is AbilityType.Interrupt or AbilityType.Response)
            return WindowChoice(world, source, ability);
        if (EffectiveCards.Kind(source, world.Facts) == CardKind.Event)
            return EventChoice(world, source, ability);
        if (EffectiveCards.Kind(source, world.Facts) != CardKind.Support
            || ability.Trigger.Timing != AbilityType.Action || ability.Labels.Length != 0
            || ability.Cost is not AbilityCost.Exhaust { Card: AbilityCostCard.Source }
            || ability.Effect is not AbilityEffect.ChooseCard
            { From: AbilityCardSelection.Query { Kind: AbilityCardQuery.Identities } } choice)
            return null;
        return choice.Effect switch
        {
            AbilityEffect.Draw { Players: AbilityPlayerSelection.OnePlayer
                { Player: AbilityPlayer.ChosenPlayer } } => choice,
            AbilityEffect.ReduceNextCardCost { Player: AbilityPlayer.ChosenPlayer,
                Amount: AbilityNumber.Constant } => choice,
            _ => null,
        };
    }

    private static AbilityEffect.ChooseCard? WindowChoice(World world, Card source,
        CompiledCardAbility ability) => ability.Labels.Length == 0
            && AbilityCostSelection.Ask(world, source.Owner, ability.Cost) is null
            && ability.Effect is AbilityEffect.ChooseCard choice ? choice : null;

    private static AbilityEffect.ChooseCard? EventChoice(World world, Card source,
        CompiledCardAbility ability) =>
        ability.Trigger.Timing == AbilityType.Action && ability.Labels.Length == 0
            && AbilityCostSelection.Ask(world, source.Owner, ability.Cost) is null
            && ability.Effect is AbilityEffect.ChooseCard choice ? choice : null;

    internal static TargetRequest? Targets(AbilityResolutionExecution execution, World world,
        Card source, CompiledCardAbility ability, int player, Defeated? triggeringDefeat = null)
    {
        if (Choice(world, source, ability) is not { } choice
            || execution.InspectsConcealedPile(choice.From)) return null;
        var cast = new AbilityResolutionState(world, source,
            world.Windows.Current?.Occurrence
                ?? new Occurrence(0, [Steps.TurnAction], Subject: source.ObjectId, Player: player), player, [])
            { Tier = ability.Trigger.Timing, TriggeringDefeat = triggeringDefeat };
        var context = execution.StructuralContext(cast);
        var cards = AbilityAdmission.LegalCardChoices(choice, context.Admission());
        string cost = AbilityCostDescription.Summary(world, source, player, ability.Cost) ?? "";
        // Empty selection retains the separate-choice engine route used by durable answers.
        // The explicit recipient route supplies exactly one current legal card.
        return new TargetRequest([.. cards.Select(card => card.ObjectId)], 0, 1)
        {
            Details = cards.ToDictionary(card => card.ObjectId,
                card => string.Join("; ", new[] { cost,
                    Detail(context, choice, card) }
                    .Where(value => !string.IsNullOrWhiteSpace(value)))),
        };
    }

    private static string? Detail(AbilityStructuralContext context, AbilityEffect.ChooseCard choice, Card card) =>
        EffectiveCards.Kind(context.Expressions.Source, context.Expressions.World.Facts) == CardKind.Event
            // Payment-sensitive quantities are unresolved until the actual effect.
            ? $"{EffectiveCards.Title(card, context.Expressions.World.Facts)}: {AbilityEffectDescription.Summary(choice)}"
            : AbilityPlayerChoiceDescription.Description(context, choice, card);

    internal static AbilityEffect.ChooseCard? Validate(AbilityResolutionExecution execution,
        AbilityResolutionState cast, CompiledCardAbility ability, IReadOnlyList<int> chosen)
    {
        if (chosen.Count == 0 || Choice(cast.World, cast.Source, ability) is not { } choice
            || execution.InspectsConcealedPile(choice.From)) return null;
        var legal = AbilityAdmission.LegalCardChoices(choice, execution.AdmissionContext(cast));
        if (chosen.Count != 1 || !legal.Any(card => card.ObjectId == chosen[0]))
            throw new RulesNotImplementedException("The activation requires one currently legal recipient.");
        return choice;
    }

    internal static void Resolve(AbilityResolutionExecution execution, AbilityResolutionState cast,
        AbilityEffect.ChooseCard choice, int recipient)
    {
        if (!AbilityAdmission.LegalCardChoices(choice, execution.AdmissionContext(cast))
            .Any(card => card.ObjectId == recipient))
        {
            // The intention names an exact recipient, never a replacement. Costs
            // remain paid; the current effect owns any new meaningful target choice.
            execution.Run(choice, cast);
            return;
        }
        // rr:initiating-abilities.step.5: "Pay the cost(s)." Step 7 then resolves the ability.
        // Reuse the structural answer after payment: it rechecks the recipient against the
        // actual effect state and preserves the existing choice frame and resolution ledger.
        execution.ApplyStructuralDecision(AbilityStructuralFlowExecution.AnswerGenericChoice(
            execution.StructuralContext(cast), choice, AbilityContinuationFacts.Empty,
            new Decision(recipient, [])), cast);
    }
}
