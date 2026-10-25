using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal static class AbilitySelectedHandDiscard
{
    internal static int Count(AbilityStructuralContext context, AbilityEffect.DiscardHandCards discard) =>
        checked((int)Math.Min(context.Expressions.World.Seats[context.Player].Hand.Cards.Count,
            Math.Max(0, context.Admission().Evaluator().Amount(discard.Count))));

    internal static Prompt Describe(AbilityStructuralContext context, AbilityEffect.DiscardHandCards discard)
    {
        int count = Count(context, discard);
        string description = $"Choose {count} card(s) from your hand to discard.";
        return new(context.Player, Question.Element, TimingPriority.Untimed, Steps.TurnAction,
            $"{context.SourceFace}: discard hand cards", false,
            [new Affordance(context.Expressions.Source.ObjectId, AbilityStructuralExecution.ChooseVerb,
                context.Expressions.Source.ObjectId, context.Player, discard.OperationName(), Request(context, discard),
                Description: description)
            { DisplayLabel = "Choose cards from hand", CommitLabel = "Discard selected cards" }])
        {
            PublicKind = PublicDecisionKind.Choice,
            DisplayQuestion = description,
            Description = description,
            ContextCardIds = [context.Expressions.Source.ObjectId],
        };
    }

    internal static AbilityStructuralTransition Answer(AbilityStructuralContext context,
        AbilityEffect.DiscardHandCards discard, Decision answer) =>
        answer.IsDecline || answer.Affordance != context.Expressions.Source.ObjectId
            || answer.Spent.Count != 0 || !Request(context, discard).Allows(answer.Targets)
            ? new Unsupported("select the required number of distinct cards from your hand")
            : new DiscardSelectedHandCardsCommand([.. answer.Targets]);

    private static TargetRequest Request(AbilityStructuralContext context, AbilityEffect.DiscardHandCards discard)
    {
        int count = Count(context, discard);
        return new([.. context.Expressions.World.Seats[context.Player].Hand.Cards.Select(card => card.ObjectId)],
            count, count);
    }

    internal static void Commit(AbilityResolutionState cast, IReadOnlyList<int> targets)
    {
        int start = cast.Events.Count;
        foreach (int id in targets)
        {
            var card = cast.World.Cards[id];
            Discard.Card(cast.World, card, cast.Trigger, cast.Events);
            cast.Discarded.Add(card);
        }
        cast.Results["discarded"] = cast.Discarded.Count;
        ActivityBatches.Complete(cast.Events, start, "Discard", cast.Player);
    }

}
