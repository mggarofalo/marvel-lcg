using Marvel.Cards.Dsl;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Names complete optional-window intentions from admitted DSL instructions.</summary>
internal static class AbilityWindowChoiceDescription
{
    internal static IReadOnlyList<WindowChoice> Describe(AbilityResolutionExecution execution,
        World world, Card source, CompiledCardAbility ability, int player, Defeated? triggeringDefeat = null)
    {
        if (ability.Trigger.Timing is not (AbilityType.Interrupt or AbilityType.Response)
            || ability.Cost is not null || ability.Labels.Length != 0
            || EffectiveCards.Kind(source, world.Facts) == CardKind.Event
            || world.Windows.Current?.Occurrence is not { } occurrence) return [];
        var cast = new AbilityResolutionState(world, source, occurrence, player, [])
            { Tier = ability.Trigger.Timing, TriggeringDefeat = triggeringDefeat };
        if (AbilityRecipientCommitment.Choice(world, source, ability) is { } choice)
            return Recipients(execution, cast, choice);
        if (ability.Effect is AbilityEffect.Draw
            { Players: AbilityPlayerSelection.OnePlayer { Player: AbilityPlayer.You or AbilityPlayer.TriggerPlayer or AbilityPlayer.Controller } one } draw)
        {
            var context = execution.StructuralContext(cast);
            int recipientSeat = new AbilityExpressionEvaluation(context.Expressions,
                new AbilitySelectorEvaluation(context.Expressions.Bindings)).Seat(one.Player);
            Card recipient = world.Seats[recipientSeat].IdentityCard;
            return [DrawChoice(draw, recipient, world, null)];
        }
        return [];
    }

    private static WindowChoice[] Recipients(AbilityResolutionExecution execution,
        AbilityResolutionState cast, AbilityEffect.ChooseCard choice)
    {
        if (execution.InspectsConcealedPile(choice.From) || !CompleteEffect(choice.Effect)) return [];
        var context = execution.StructuralContext(cast);
        var legal = AbilityAdmission.LegalCardChoices(choice, context.Admission());
        if (legal.Count == 0 || legal.Any(card => !DeckTypes.IsInPlay(card.Area.Type))) return [];
        var prompt = AbilityChoicePromptDescription.DescribeChoice(
            context, choice, AbilityContinuationFacts.Empty).Prompt;
        return prompt.Affordances.Where(option => option.CommitLabel is not null
            || choice.Effect is AbilityEffect.Draw).Select(option => NamedChoice(option, choice, cast.World)).ToArray();
    }

    private static bool CompleteEffect(AbilityEffect effect) => effect switch
    {
        AbilityEffect.GiveStatus status => Chosen(status.Cards),
        AbilityEffect.CardAction action => Chosen(action.Selection),
        AbilityEffect.Draw { Players: AbilityPlayerSelection.OnePlayer
            { Player: AbilityPlayer.ChosenPlayer } } => true,
        AbilityEffect.Damage damage => Chosen(damage.Cards),
        AbilityEffect.RemoveThreat threat => Chosen(threat.Schemes),
        _ => false,
    };

    private static bool Chosen(AbilityCardSelection selection) => selection is
        AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen };

    private static WindowChoice NamedChoice(Affordance option, AbilityEffect.ChooseCard choice, World world)
    {
        if (choice.Effect is AbilityEffect.Draw
            { Players: AbilityPlayerSelection.OnePlayer { Player: AbilityPlayer.ChosenPlayer } } draw)
            return DrawChoice(draw, world.Cards[option.Id], world, option.Id);
        return new WindowChoice(option.Id, option.CommitLabel ?? option.DisplayLabel ?? option.Label,
            option.Description) { RecipientIds = [option.Id] };
    }

    private static WindowChoice DrawChoice(AbilityEffect.Draw draw, Card recipient, World world, int? target) =>
        new(target, $"{EffectiveCards.Title(recipient, world.Facts)} draws {draw.Count} card{(draw.Count == 1 ? "" : "s")}",
            AbilityEffectDescription.Summary(draw)) { RecipientIds = [recipient.ObjectId] };
}
