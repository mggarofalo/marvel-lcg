using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal static class AbilityResourceIconDiscard
{
    internal static int Required(AbilityStructuralContext context, AbilityEffect.DiscardResourceIcons discard) =>
        checked((int)Math.Min(Math.Max(0, context.Admission().Evaluator().Amount(discard.Count)),
            Cards(context, discard).Sum(card => Icons(context, discard, card))));

    internal static Prompt Describe(AbilityStructuralContext context, AbilityEffect.DiscardResourceIcons discard)
    {
        var request = Request(context, discard);
        string types = discard.Resources.Length == 0 ? "of any type" : "of type " + string.Join(" or ",
            discard.Resources.Select(type => type switch
            {
                Resources.Mental => "mental", Resources.Physical => "physical",
                Resources.Energy => "energy", Resources.Wild => "wild",
                _ => throw new InvalidOperationException("Unknown printed resource type"),
            }));
        string description = $"Discard a minimal group of hand cards providing {Required(context, discard)} printed resource icon(s) {types}. "
            + "If your hand has fewer eligible icons, discard all cards with eligible icons. Other cards stay in hand.";
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

    private static List<Card> Cards(AbilityStructuralContext context, AbilityEffect.DiscardResourceIcons discard) =>
        [.. context.Expressions.World.Seats[context.Player].Hand.Cards.Where(card => Icons(context, discard, card) > 0)];

    private static int Icons(AbilityStructuralContext context, AbilityEffect.DiscardResourceIcons discard, Card card) =>
        // rr:wild-resource.3: "wild resources cannot be interpreted as any of the other resource types."
        Resources.GeneratedBy(card.FaceId, context.Expressions.World.Facts)
            .Count(icon => discard.Resources.Length == 0 || discard.Resources.Contains(icon));

    private static TargetRequest Request(AbilityStructuralContext context, AbilityEffect.DiscardResourceIcons discard)
    {
        var cards = Cards(context, discard);
        int required = Required(context, discard);
        var groups = new List<IReadOnlyList<int>>();
        var selected = new List<int>();
        AddGroups(0, 0, int.MaxValue);
        void AddGroups(int start, int total, int smallest)
        {
            if (total >= required)
            {
                if (total - smallest < required) groups.Add([.. selected]);
                return;
            }
            for (int index = start; index < cards.Count; index++)
            {
                int icons = Icons(context, discard, cards[index]);
                selected.Add(cards[index].ObjectId);
                AddGroups(index + 1, total + icons, Math.Min(smallest, icons));
                selected.RemoveAt(selected.Count - 1);
            }
        }
        return new TargetRequest([.. cards.Select(card => card.ObjectId)], 1, cards.Count, Groups: groups)
        {
            Details = cards.ToDictionary(card => card.ObjectId,
                card => $"{Icons(context, discard, card)} eligible printed resource icon(s); discard this card."),
        };
    }
}
