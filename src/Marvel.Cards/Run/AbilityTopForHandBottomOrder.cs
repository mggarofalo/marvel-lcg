using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Places the cards a top-of-deck look did not add to hand on the deck bottom in a chosen order.</summary>
/// <remarks>
/// Agatha Harkness (15007): "Add 1 of those to your hand and place the rest on the
/// bottom of your deck in any order." One remaining card needs no order; two or
/// more are retained in the resolution and asked for as one ordered answer.
/// </remarks>
internal static class AbilityTopForHandBottomOrder
{
    private const string Prefix = "topForHandRest:";
    private const string Count = Prefix + "count";

    /// <summary>Whether the look already added its card and now waits for the order.</summary>
    internal static bool Ordering(IReadOnlyDictionary<string, long> memory) => memory.ContainsKey(Count);

    /// <summary>Answer the look or the order.</summary>
    /// <returns>True when the effect is complete; false when it waits for the order.</returns>
    internal static bool Apply(AbilityResolutionExecution execution, AbilityEffect.ChooseTopForHand choice,
        AbilityResolutionState cast, Decision input)
    {
        if (Ordering(cast.Results))
        {
            Answer(cast, input);
            cast.ResolveEffect();
            return true;
        }
        var answer = AbilitySpecialChoiceDispatch.AnswerSpecialChoice(execution.StructuralContext(cast), choice, input);
        if (answer is Unsupported unsupported) throw new RulesNotImplementedException(unsupported.Reason);
        var command = answer as ChooseTopForHandCommand
            ?? throw new InvalidOperationException("Structural owner did not return top-card selection");
        if (!Select([.. command.Top.Select(id => cast.World.Cards[id])], cast.World.Cards[command.Selected], cast))
        {
            execution.SuspendForChoice(choice, cast);
            return false;
        }
        cast.ResolveEffect();
        return true;
    }

    private static bool Select(IReadOnlyList<Card> top, Card selected, AbilityResolutionState cast)
    {
        var deck = cast.World.Seats[cast.Player].Deck;
        var hand = cast.World.Seats[cast.Player].Hand;
        World.MoveToTop(selected, hand);
        cast.Events.Add(new CardsMoved(Places.Reference(deck), Places.Reference(hand),
            [new Landing(selected.ObjectId, hand.Cards.Count - 1)])
        { Trigger = cast.Trigger, Verb = "Add_To_Hand" });
        var rest = top.Where(card => !ReferenceEquals(card, selected)).ToList();
        if (rest.Count < 2)
        {
            PlaceOnBottom(rest, deck, cast.Trigger, cast.Events);
            return true;
        }
        cast.Results[Count] = rest.Count;
        for (int index = 0; index < rest.Count; index++)
            cast.Results[Prefix + index] = rest[index].ObjectId;
        return false;
    }

    internal static Prompt Describe(AbilityStructuralContext context)
    {
        var world = context.Expressions.World;
        var source = context.Expressions.Source;
        var cards = Retained(world, context.Expressions.Results);
        const string description =
            "Choose all of these cards in top-to-bottom order. They go to the bottom of your deck in that order; the last selected card becomes the bottom card.";
        return new(context.Player, Question.Order, TimingPriority.Untimed, context.Trigger,
            "Order cards for the bottom of your deck", false,
            [new Affordance(source.ObjectId, "Order", source.ObjectId, context.Player,
                "Place on the bottom of your deck",
                new TargetRequest([.. cards.Select(card => card.ObjectId)], cards.Count, cards.Count))
            { DisplayLabel = "Order cards for the bottom of your deck", CommitLabel = "Place on bottom in selected order", Description = description }])
        {
            PublicKind = PublicDecisionKind.CardLook,
            DisplayQuestion = $"{world.Facts.Title(context.SourceFace)}: choose the order of the other cards",
            Description = description,
            ContextCardIds = [.. cards.Select(card => card.ObjectId)],
            CauseCardIds = [source.ObjectId],
            ExposesConcealedCandidates = true,
        };
    }

    /// <summary>Apply the ordered answer; an answer that is not every retained card exactly once raises.</summary>
    private static void Answer(AbilityResolutionState cast, Decision answer)
    {
        var cards = Retained(cast.World, cast.Results);
        var ids = answer.Targets;
        if (ids.Count != cards.Count || !ids.Order().SequenceEqual(cards.Select(card => card.ObjectId).Order()))
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' must order each of its {cards.Count} remaining cards exactly once");
        var deck = cast.World.Seats[cast.Player].Deck;
        PlaceOnBottom([.. ids.Select(id => cast.World.Cards[id])], deck, cast.Trigger, cast.Events);
        foreach (string key in cast.Results.Keys.Where(key => key.StartsWith(Prefix, StringComparison.Ordinal)).ToArray())
            cast.Results.Remove(key);
    }

    // The engine chooses top-first answer order. Each card goes beneath the ones
    // already placed, so the last named card ends up as the bottom card.
    private static void PlaceOnBottom(List<Card> topFirst, Area deck,
        string trigger, List<GameEvent> events)
    {
        if (topFirst.Count == 0) return;
        foreach (var card in topFirst) CardPlacement.MoveToBottom(card, deck);
        events.Add(new AreaReordered(Places.Reference(deck), [.. deck.Cards.Select(card => card.ObjectId)])
        { Trigger = trigger, Verb = "Place_On_Bottom" });
    }

    private static List<Card> Retained(World world, IReadOnlyDictionary<string, long> memory)
    {
        int count = checked((int)memory[Count]);
        var cards = new List<Card>(count);
        for (int index = 0; index < count; index++)
            cards.Add(world.Cards[checked((int)memory[Prefix + index])]);
        return cards;
    }
}
