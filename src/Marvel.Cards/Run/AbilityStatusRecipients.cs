using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>One complete, distinct status-recipient selection before any status is given.</summary>
internal static class AbilityStatusRecipients
{
    internal static AbilityTargetAdmission.TargetLegality Target(
        AbilityEffect.ChooseStatusRecipients group, AbilityAdmissionContext context) =>
        Candidates(group, context).Count > 0
            ? AbilityTargetAdmission.TargetLegality.Valid : AbilityTargetAdmission.TargetLegality.Invalid;

    internal static IReadOnlyList<Card> Candidates(AbilityEffect.ChooseStatusRecipients group,
        AbilityAdmissionContext context)
    {
        var world = context.Expressions.World;
        return new AbilitySelectorEvaluation(context.Expressions.Bindings).Every(group.From)
            .Where(card => DeckTypes.IsInPlay(card.Area.Type)
                && CardKinds.IsCharacter(EffectiveCards.Kind(card, world.Facts))
                && Statuses.Count(world, card, group.Status) < Statuses.Limit(world, world.Facts, card, group.Status))
            .OrderBy(card => card.ObjectId).ToList();
    }

    private static TargetRequest Request(AbilityStructuralContext context, AbilityEffect.ChooseStatusRecipients group)
    {
        var cards = Candidates(group, context.Admission());
        return new([.. cards.Select(card => card.ObjectId)], 0, Math.Min(group.Maximum, cards.Count))
        {
            Details = cards.ToDictionary(card => card.ObjectId, _ => $"Give a {group.Status} status card"),
        };
    }

    internal static Prompt Describe(AbilityStructuralContext context, AbilityEffect.ChooseStatusRecipients group)
    {
        string description = $"Choose up to {group.Maximum} different characters. Give each a {group.Status} status card. Confirm with none selected to give no status cards; later instructions still resolve.";
        return new(context.Player, Question.Element, TimingPriority.Untimed, Steps.TurnAction,
            $"{context.SourceFace}: choose status recipients", false,
            [new Affordance(context.Expressions.Source.ObjectId, AbilityStructuralExecution.ChooseVerb,
                context.Expressions.Source.ObjectId, context.Player, group.OperationName(), Request(context, group),
                Description: description)
            { DisplayLabel = "Choose status recipients", CommitLabel = "Give selected characters status cards" }])
        {
            PublicKind = PublicDecisionKind.VisibleCardSelection,
            DisplayQuestion = $"Give up to {group.Maximum} characters each a {group.Status} status card",
            Description = description,
            ContextCardIds = [context.Expressions.Source.ObjectId],
        };
    }

    internal static void Commit(AbilityStructuralContext context, AbilityResolutionState cast,
        AbilityEffect.ChooseStatusRecipients group, Decision answer)
    {
        if (answer.IsDecline || answer.Affordance != context.Expressions.Source.ObjectId
            || answer.Spent.Count != 0 || !Request(context, group).Allows(answer.Targets))
            throw new ArgumentException("Select a bounded group of distinct currently eligible characters", nameof(answer));
        // rr:choose-game-element.3.1: "The same target cannot be chosen multiple times this way."
        // The engine chooses physical-id order for committing the complete selected group.
        foreach (int id in answer.Targets.Order())
            Reveal.Afflict(cast.World, cast.World.Facts, cast.World.Cards[id], group.Status, cast.Trigger, cast.Events);
        if (answer.Targets.Count > 0) cast.ResolveEffect();
    }
}
