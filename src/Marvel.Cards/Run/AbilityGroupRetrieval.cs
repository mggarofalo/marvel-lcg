using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>One optional bounded physical-card selection, validated before any movement.</summary>
internal static class AbilityGroupRetrieval
{
    internal static AbilityTargetAdmission.TargetLegality Target(
        AbilityEffect.ChooseCardsToHand group, AbilityAdmissionContext context) =>
        Candidates(group, context).Count > 0
            ? AbilityTargetAdmission.TargetLegality.Valid : AbilityTargetAdmission.TargetLegality.Invalid;

    internal static IReadOnlyList<Card> Candidates(AbilityEffect.ChooseCardsToHand group,
        AbilityAdmissionContext context)
    {
        if (!DeckTypes.IsInPlay(context.Expressions.Source.Area.Type)) return [];
        var selector = new AbilitySelectorEvaluation(context.Expressions.Bindings);
        return selector.Every(group.From).Where(card => !DeckTypes.IsInPlay(card.Area.Type)
            && card.Owner >= 0 && card.Owner < context.Expressions.World.Players).ToList();
    }

    private static TargetRequest Request(AbilityStructuralContext context, AbilityEffect.ChooseCardsToHand group)
    {
        var cards = Candidates(group, context.Admission());
        return new([.. cards.Select(card => card.ObjectId)], 0, Math.Min(group.Maximum, cards.Count),
            IsSearch: cards.Any(card => !card.FaceUp));
    }

    internal static Prompt Describe(AbilityStructuralContext context, AbilityEffect.ChooseCardsToHand group)
    {
        string description = $"Choose up to {group.Maximum} cards to add to your hand. Confirm with none selected to take no cards.";
        return new(context.Player, Question.Element, TimingPriority.Untimed, Steps.TurnAction,
            $"{context.SourceFace}: choose cards to add to hand", false,
            [new Affordance(context.Expressions.Source.ObjectId, AbilityStructuralExecution.ChooseVerb,
                context.Expressions.Source.ObjectId, context.Player, group.OperationName(), Request(context, group),
                Description: description)
            { DisplayLabel = "Choose cards to add to hand", CommitLabel = "Add selected cards to hand" }])
        {
            PublicKind = PublicDecisionKind.VisibleCardSelection,
            DisplayQuestion = $"Choose up to {group.Maximum} cards to add to your hand",
            Description = description,
            ContextCardIds = [context.Expressions.Source.ObjectId],
            ExposesConcealedCandidates = true,
        };
    }

    internal static AbilityStructuralTransition Answer(AbilityStructuralContext context,
        AbilityEffect.ChooseCardsToHand group, Decision answer) =>
        answer.IsDecline || answer.Affordance != context.Expressions.Source.ObjectId
            || answer.Spent.Count != 0 || !DeckTypes.IsInPlay(context.Expressions.Source.Area.Type)
            || !Request(context, group).Allows(answer.Targets)
            ? new Unsupported("select a bounded group of distinct currently eligible cards")
            : new RetrieveCardsCommand([.. answer.Targets]);

    internal static void Commit(AbilityResolutionState cast, IReadOnlyList<int> targets)
    {
        // Printed "your hand" names the resolving controller's hand. Stored cards are
        // already out of play (ruling:6768213a7e0f4606); this does not transfer ownership.
        foreach (int id in targets)
        {
            var card = cast.World.Cards[id];
            AbilityCardMovementExecution.MoveToHand(cast.World, card, cast.World.Seats[cast.Player].Hand,
                cast.Trigger, cast.Events, "Add_To_Hand");
            card.TurnFaceUp();
        }
    }
}
