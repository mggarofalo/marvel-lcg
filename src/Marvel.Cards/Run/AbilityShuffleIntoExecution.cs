using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Moves selected physical cards and shuffles their destination decks.</summary>
internal static class AbilityShuffleIntoExecution
{
    internal static AbilityDeckAndRevealResult Run(
        AbilityEffect.ShuffleInto shuffle, AbilityDeckAndRevealContext context)
    {
        var evaluation = new AbilitySelectorEvaluation(context.Expressions.Bindings);
        var selected = evaluation.Result(evaluation.Every(shuffle.Cards));
        foreach (var observation in selected.Information) context.World.RecordInformation(observation);
        var cards = selected.Value;
        bool applied = false;
        if (shuffle.Deck == AbilityShuffleDestination.OwnerDeck)
        {
            // rr:ownership-and-control: ownership belongs to the starting deck, not the controller.
            // Validate every owner before moving any selected card; scenario ownership is not a player deck.
            foreach (var card in cards)
                if (card.Owner < 0 || card.Owner >= context.World.Seats.Count)
                    throw new RulesNotImplementedException(
                        $"'{context.Source.FaceId}' ownerDeck cannot route card {card.ObjectId} with owner {card.Owner}");
            // Engine choice: ascending owner order fixes shared RNG consumption; selector order is retained within each deck.
            foreach (var group in cards.GroupBy(card => card.Owner).OrderBy(group => group.Key))
                applied |= MoveOwnerGroup(group, context);
        }
        else
        {
            var deck = shuffle.Deck switch
            {
                AbilityShuffleDestination.YourDeck => context.World.Seats[context.Player].Deck,
                AbilityShuffleDestination.EncounterDeck => context.World.AreaOf(DeckType.EncounterDeck),
                _ => throw new InvalidOperationException("Unknown compiled shuffle destination"),
            };
            applied = MoveAndShuffle(cards, deck, context);
        }
        return new(true, applied, null, ImmutableDictionary<string, long>.Empty);
    }

    private static bool MoveAndShuffle(
        IEnumerable<Card> cards, Area deck, AbilityDeckAndRevealContext context)
    {
        bool applied = Move(cards, deck, context);
        // rr:shuffle: "Shuffling is the game function of randomizing the contents of a deck."
        // Engine choice: move all selected cards before one existing seeded shuffle per destination.
        return context.World.Shuffle(deck) | applied;
    }

    private static bool MoveOwnerGroup(
        IGrouping<int, Card> cards, AbilityDeckAndRevealContext context)
    {
        var owner = context.World.Seats[cards.Key];
        // rr:ownership-and-control.7.2: "removed from the game if that player is no longer in the game".
        // The removed area is not a deck: removing these cards consumes no shuffle draws.
        return owner.Eliminated
            ? Move(cards, context.World.AreaOf(DeckType.RemovedArea), context)
            : MoveAndShuffle(cards, owner.Deck, context);
    }

    private static bool Move(
        IEnumerable<Card> cards, Area deck, AbilityDeckAndRevealContext context)
    {
        bool applied = false;
        foreach (var card in cards)
        {
            if (!SideSchemeDeparture.Allowed(context.World, card)) continue;
            var from = card.Area;
            World.MoveToTop(card, deck);
            context.Events.Add(new CardsMoved(
                Places.Reference(from), Places.Reference(deck),
                [new Landing(card.ObjectId, deck.Cards.Count - 1)])
            { Trigger = context.Trigger, Verb = "Shuffle_Into" });
            applied = true;
        }
        return applied;
    }
}
