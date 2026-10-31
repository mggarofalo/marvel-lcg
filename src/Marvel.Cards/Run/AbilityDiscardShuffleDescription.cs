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
        printed += discard.Set is { } set ? $" from the {SetName(context, set)} set" : string.Empty;
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

    // Names a set by the first face of the identity printed in it, falling back to its key.
    private static string SetName(AbilityStructuralContext context, string set)
    {
        var facts = context.Expressions.World.Facts;
        return context.Expressions.World.Seats.Select(seat => seat.IdentityCard)
            .Where(identity => identity is not null
                && string.Equals(facts.EncounterSet(identity.Faces[0]), set, StringComparison.Ordinal))
            .Select(identity => facts.Title(identity.Faces[0])).FirstOrDefault() ?? set;
    }

    private static string ResourceName(char resource) => resource switch
    {
        'B' => "mental",
        'R' => "physical",
        'Y' => "energy",
        _ => "wild",
    };

    /// <summary>The cards in <paramref name="player"/>'s discard pile the selection may name.</summary>
    internal static IReadOnlyList<Card> Eligible(World world, int player, AbilityEffect.ChooseDiscardToShuffle discard)
    {
        Area area = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player);
        // A printed resource restriction reads the card's own icons; a wild icon is
        // not a mental, energy or physical icon outside payment (rr:wild-resource.3).
        // A set restriction compares the printed card set exactly; an identity's
        // cards share its set (rr:identity-specific-card.2).
        return [.. area.Cards.Where(card => (discard.Resource is not { } resource
            || Resources.GeneratedBy(card.FaceId, world.Facts).Contains(resource))
            && (discard.Set is not { } set
                || string.Equals(world.Facts.EncounterSet(card.FaceId), set, StringComparison.Ordinal)))];
    }

    private static TargetRequest Request(AbilityStructuralContext context, AbilityEffect.ChooseDiscardToShuffle discard)
    {
        World world = context.Expressions.World;
        IReadOnlyList<Card> eligible = Eligible(world, context.Player, discard);
        IGrouping<string, Card>[] titles = [.. eligible.GroupBy(card => world.Facts.Title(card.FaceId), StringComparer.Ordinal)];
        int maximum = Math.Min(discard.Maximum, discard.DifferentTitles ? titles.Length : eligible.Count);
        // rr:choose-game-element.3: a fixed number chooses as many as are available.
        return new TargetRequest([.. eligible.Select(card => card.ObjectId)], maximum == 0 ? discard.Minimum : Math.Min(discard.Minimum, maximum), maximum)
        {
            ExclusiveSets = !discard.DifferentTitles ? [] : [.. titles.Where(group => group.Count() > 1)
                .Select(group => (IReadOnlyList<int>)group.Select(card => card.ObjectId).ToArray())],
        };
    }
}
