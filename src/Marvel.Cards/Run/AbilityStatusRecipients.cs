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

    // rr:choose-game-element.3: a fixed number chooses "as many as are available, to a
    // maximum of the specified number"; "up to" lets the player choose fewer.
    private static TargetRequest Request(AbilityStructuralContext context, AbilityEffect.ChooseStatusRecipients group)
    {
        var cards = Candidates(group, context.Admission());
        int maximum = (int)Math.Min(Bound(context, group), cards.Count);
        return new([.. cards.Select(card => card.ObjectId)], group.Count is null ? 0 : maximum, maximum)
        {
            Details = cards.ToDictionary(card => card.ObjectId, _ => $"Give a {group.Status} status card"),
        };
    }

    private static long Bound(AbilityStructuralContext context, AbilityEffect.ChooseStatusRecipients group) =>
        group.Count is { } count ? Math.Max(0, context.Admission().Evaluator().Amount(count)) : group.Maximum;

    internal static Prompt Describe(AbilityStructuralContext context, AbilityEffect.ChooseStatusRecipients group)
    {
        string description = group.Count is null
            ? $"Choose up to {group.Maximum} different characters. Give each a {group.Status} status card. Confirm with none selected to give no status cards; later instructions still resolve."
            : $"Choose {Bound(context, group)} different characters, or as many as can receive one. Give each a {group.Status} status card.";
        return new(context.Player, Question.Element, TimingPriority.Untimed, Steps.TurnAction,
            $"{context.SourceFace}: choose status recipients", false,
            [new Affordance(context.Expressions.Source.ObjectId, AbilityStructuralExecution.ChooseVerb,
                context.Expressions.Source.ObjectId, context.Player, group.OperationName(), Request(context, group),
                Description: description)
            { DisplayLabel = "Choose status recipients", CommitLabel = "Give selected characters status cards" }])
        {
            PublicKind = PublicDecisionKind.VisibleCardSelection,
            DisplayQuestion = group.Count is null
                ? $"Give up to {group.Maximum} characters each a {group.Status} status card"
                : $"Give {Bound(context, group)} characters each a {group.Status} status card",
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
