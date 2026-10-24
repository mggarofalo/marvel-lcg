using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal static class AbilityResourceIconDiscard
{
    internal static bool HasCards(AbilityStructuralContext context) => Cards(context).Count > 0;

    internal static Prompt Describe(AbilityStructuralContext context, AbilityEffect.DiscardResourceIcons discard)
    {
        var request = Request(context, discard);
        string description = $"Discard hand cards providing {discard.Count} printed resource icon(s). "
            + "If your hand has fewer icons, discard all cards with icons. Other cards stay in hand.";
        return new(context.Player, Question.Element, TimingPriority.Untimed, Steps.TurnAction,
            $"{context.SourceFace}: discard printed resources", false,
            [new Affordance(context.Expressions.Source.ObjectId, AbilityStructuralExecution.ChooseVerb,
                context.Expressions.Source.ObjectId, context.Player, discard.OperationName(), request,
                Description: description)
            { DisplayLabel = "Discard cards from hand", CommitLabel = "Discard selected cards" }])
        {
            PublicKind = PublicDecisionKind.Choice,
            DisplayQuestion = "Discard cards for their printed resource icons",
            Description = description,
            ContextCardIds = [context.Expressions.Source.ObjectId],
        };
    }

    internal static AbilityStructuralTransition Answer(AbilityStructuralContext context,
        AbilityEffect.DiscardResourceIcons discard, Decision answer) =>
        answer.IsDecline || answer.Affordance != context.Expressions.Source.ObjectId
            || answer.Spent.Count != 0 || !Request(context, discard).Allows(answer.Targets)
            ? new Unsupported("select a minimal group of hand cards satisfying the printed resource discard")
            : new DiscardSelectedHandCardsCommand([.. answer.Targets]);

    private static List<Card> Cards(AbilityStructuralContext context) =>
        [.. context.Expressions.World.Seats[context.Player].Hand.Cards.Where(card => Icons(context, card) > 0)];

    private static int Icons(AbilityStructuralContext context, Card card) =>
        Resources.GeneratedBy(card.FaceId, context.Expressions.World.Facts).Length;

    private static TargetRequest Request(AbilityStructuralContext context, AbilityEffect.DiscardResourceIcons discard)
    {
        var cards = Cards(context);
        int required = Math.Min(discard.Count, cards.Sum(card => Icons(context, card)));
        var groups = new List<IReadOnlyList<int>>();
        for (int first = 0; first < cards.Count; first++)
        {
            int firstIcons = Icons(context, cards[first]);
            if (firstIcons >= required) groups.Add([cards[first].ObjectId]);
            else for (int second = first + 1; second < cards.Count; second++)
            {
                int secondIcons = Icons(context, cards[second]);
                if (secondIcons < required && firstIcons + secondIcons >= required)
                    groups.Add([cards[first].ObjectId, cards[second].ObjectId]);
            }
        }
        return new TargetRequest([.. cards.Select(card => card.ObjectId)], 1, Math.Min(2, cards.Count), Groups: groups)
        {
            Details = cards.ToDictionary(card => card.ObjectId,
                card => $"{Icons(context, card)} printed resource icon(s); discard this card."),
        };
    }
}
