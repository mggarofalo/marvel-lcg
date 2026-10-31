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
        string distinct = discard.DifferentTitles ? " with different titles" : string.Empty;
        string printed = discard.Resource is { } resource ? $" with a printed {ResourceName(resource)} resource" : string.Empty;
        string description = $"Choose {request.Min}–{request.Max} cards{distinct}{printed} from your discard pile. "
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
            ? new Unsupported($"'{context.SourceFace}' requires {discard.Minimum} to {discard.Maximum} legal discard cards")
            : new ShuffleDiscardCommand([.. answer.Targets]);

    private static string ResourceName(char resource) => resource switch
    {
        'B' => "mental",
        'R' => "physical",
        'Y' => "energy",
        _ => "wild",
    };

    private static TargetRequest Request(AbilityStructuralContext context, AbilityEffect.ChooseDiscardToShuffle discard)
    {
        World world = context.Expressions.World;
        Area area = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(context.Player), cardOwner: context.Player);
        // A printed resource restriction reads the card's own icons; a wild icon is
        // not a mental, energy or physical icon outside payment (rr:wild-resource.3).
        Card[] eligible = [.. area.Cards.Where(card => discard.Resource is not { } resource
            || Resources.GeneratedBy(card.FaceId, world.Facts).Contains(resource))];
        IGrouping<string, Card>[] titles = [.. eligible.GroupBy(card => world.Facts.Title(card.FaceId), StringComparer.Ordinal)];
        return new TargetRequest([.. eligible.Select(card => card.ObjectId)], discard.Minimum, Math.Min(discard.Maximum, discard.DifferentTitles ? titles.Length : eligible.Length))
        {
            ExclusiveSets = !discard.DifferentTitles ? [] : [.. titles.Where(group => group.Count() > 1)
                .Select(group => (IReadOnlyList<int>)group.Select(card => card.ObjectId).ToArray())],
        };
    }
}
