using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Owns the visible discard selection and its authoritative different-title combinations.</summary>
internal static class AbilityDiscardShuffleDescription
{
    internal static Prompt Describe(AbilityStructuralContext context, AbilityEffect.ChooseDiscardToShuffle discard)
    {
        TargetRequest request = Request(context, discard);
        string description = $"Choose 1–{request.Max} cards with different titles from your discard pile. "
            + "Shuffle the selected cards into your deck when you confirm.";
        return new Prompt(context.Player, Question.Element, TimingPriority.Untimed, Steps.TurnAction,
            $"{context.SourceFace}: choose cards to shuffle", false,
            [new Affordance(context.Expressions.Source.ObjectId, AbilityStructuralExecution.ChooseVerb,
                context.Expressions.Source.ObjectId, context.Player, discard.OperationName(), request,
                Description: description)
            {
                DisplayLabel = "Choose cards to shuffle into your deck",
                CommitLabel = "Shuffle selected cards into your deck",
            }])
        {
            PublicKind = PublicDecisionKind.VisibleCardSelection,
            DisplayQuestion = "Choose cards to shuffle into your deck",
            Description = description,
            ContextCardIds = [context.Expressions.Source.ObjectId],
        };
    }

    internal static AbilityStructuralTransition Answer(
        AbilityStructuralContext context, AbilityEffect.ChooseDiscardToShuffle discard, Decision answer) =>
        answer.IsDecline || !Request(context, discard).Allows(answer.Targets)
            ? new Unsupported($"'{context.SourceFace}' requires one to {discard.Maximum} cards with different titles")
            : new ShuffleDiscardCommand([.. answer.Targets]);

    private static TargetRequest Request(AbilityStructuralContext context, AbilityEffect.ChooseDiscardToShuffle discard)
    {
        World world = context.Expressions.World;
        Area area = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(context.Player), cardOwner: context.Player);
        IGrouping<string, Card>[] titles = [.. area.Cards.GroupBy(card => world.Facts.Title(card.FaceId), StringComparer.Ordinal)];
        return new TargetRequest([.. area.Cards.Select(card => card.ObjectId)], 1, Math.Min(discard.Maximum, titles.Length))
        {
            ExclusiveSets = [.. titles.Where(group => group.Count() > 1)
                .Select(group => (IReadOnlyList<int>)group.Select(card => card.ObjectId).ToArray())],
        };
    }
}
