using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>One complete, distinct recipient selection for ordinary simultaneous damage.</summary>
internal static class AbilityDamageRecipients
{
    internal static IReadOnlyList<Card> Candidates(AbilityEffect.Damage damage,
        AbilityAdmissionContext context)
    {
        if (AbilityEventModifiers.DamageAmount(context.World, context.Source,
            context.Evaluator().Amount(damage.Amount)) <= 0) return [];
        return [.. context.Selectors().Every(damage.Cards)
            .Where(card => DeckTypes.IsInPlay(card.Area.Type)
                && CardKinds.IsCharacter(EffectiveCards.Kind(card, context.World.Facts))
                && AbilityProgramQueries.CanTakeDamage(context.World, context.Program, card, context.Source))
            .OrderBy(card => card.ObjectId)];
    }

    private static TargetRequest Request(AbilityStructuralContext context, AbilityEffect.Damage damage)
    {
        var cards = Candidates(damage, context.Admission());
        long amount = AbilityEventModifiers.DamageAmount(context.Expressions.World, context.Expressions.Source,
            context.Admission().Evaluator().Amount(damage.Amount));
        return new([.. cards.Select(card => card.ObjectId)], 0, Math.Min(damage.Maximum!.Value, cards.Count))
        {
            Details = cards.ToDictionary(card => card.ObjectId,
                _ => $"Deal {amount} damage. Prevention and interrupts can change the result."),
        };
    }

    internal static Prompt Describe(AbilityStructuralContext context, AbilityEffect.Damage damage)
    {
        string description = $"Choose up to {damage.Maximum} different characters to receive damage together. "
            + "Confirm with none selected to deal no damage; later instructions still resolve.";
        return new(context.Player, Question.Element, TimingPriority.Untimed, Steps.TurnAction,
            $"{context.SourceFace}: choose damage recipients", false,
            [new Affordance(context.Expressions.Source.ObjectId, AbilityStructuralExecution.ChooseVerb,
                context.Expressions.Source.ObjectId, context.Player, damage.OperationName(), Request(context, damage),
                Description: description)
            { DisplayLabel = "Choose damage recipients", CommitLabel = "Deal damage to chosen characters" }])
        {
            PublicKind = PublicDecisionKind.VisibleCardSelection,
            DisplayQuestion = $"Choose up to {damage.Maximum} different damage recipients",
            Description = description,
            ContextCardIds = [context.Expressions.Source.ObjectId],
        };
    }

    internal static void Commit(AbilityResolutionExecution execution, AbilityResolutionState cast,
        AbilityEffect.Damage damage, Decision answer)
    {
        var context = execution.StructuralContext(cast);
        if (answer.IsDecline || answer.Affordance != cast.Source.ObjectId
            || answer.Spent.Count != 0 || !Request(context, damage).Allows(answer.Targets))
            throw new ArgumentException("Select a bounded group of distinct currently eligible characters", nameof(answer));
        // rr:choose-game-element.3.1: "The same target cannot be chosen multiple times this way."
        // The engine chooses physical-id order for the simultaneous damage event stream.
        var selected = answer.Targets.Order().Select(id => cast.World.Cards[id]).ToArray();
        execution.ApplyDamageAndThreat(AbilityDamageAndThreatExecution.Run(damage, damage,
            execution.DamageAndThreatContext(cast) with { SelectedDamageRecipients = selected }), damage, cast);
    }
}
