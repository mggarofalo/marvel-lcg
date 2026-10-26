using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>Synthetic multiplayer boundaries for reset during ordered elimination.</summary>
public sealed class EliminationResetReviewTests
{
    [Fact]
    [Rule("rr:player-elimination.step.5")]
    [Rule("rr:player-elimination.6")]
    public void EliminatingAHostDoesNotRecycleItsOwnersHostedInvocation()
    {
        // "Eliminated players no longer participate in the game"; effects that
        // refer to players "ignore eliminated players". Hosted cards leave before
        // step 5 removes the owner's separate Invocation areas.
        var world = Board();
        var source = world.CreateArea(DeckType.AsideDeck, 0, PlayArea.Of(0));
        var cards = Enumerable.Range(32, 5)
            .Select(number => world.CreateCard($"090{number}", source)).ToArray();
        var invocation = InvocationDeck.Initialize(world, 0, cards, "setup", []);
        var host = world.CreateCard("upgrade",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        foreach (var card in cards[..^1]) World.MoveToTop(card, invocation.Discard);
        World.MoveToTop(cards[^1], world.AreaOf(
            DeckType.BeneathCardsPile, PlayArea.Of(0), host.ObjectId, cardOwner: 0));
        var random = world.Random.Generator.GetState();
        long consumed = world.Random.Generator.WordsConsumed;
        var events = new List<GameEvent>();

        Elimination.Eliminate(world, world.Facts, 0, "test", events);

        Assert.All(cards, card => Assert.Equal(DeckType.RemovedArea, card.Area.Type));
        Assert.Equal(DeckType.RemovedArea, host.Area.Type);
        Assert.DoesNotContain(events.OfType<CardsMoved>(), move => move.Verb == "Reset");
        Assert.Empty(events.OfType<AreaReordered>());
        Assert.Empty(events.OfType<CardsFlipped>());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(consumed, world.Random.Generator.WordsConsumed);
        Assert.Contains(events.OfType<CardDetached>(), change =>
            change.Card == cards[^1].ObjectId && change.Host == host.ObjectId);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:player-elimination.3")]
    [Rule("rr:attach-to.1")]
    [Rule("rr:player-deck.4")]
    public void EliminatingAHostStillResetsItsActiveAttachmentsOwner()
    {
        // "Place each other card in its owner's discard pile"; an attachment is
        // "discarded" when its host leaves. An empty player deck resets once
        // "there is at least one card in the player's discard pile".
        var world = Board();
        var host = world.CreateCard("upgrade",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var attached = world.CreateCard("upgrade", world.AreaOf(
            DeckType.UpgradesArea, PlayArea.Of(0), host.ObjectId, cardOwner: 1));
        World.MoveToTop(Assert.Single(world.Seats[1].Deck.Cards), world.Seats[1].Hand);
        int[] encounter = [.. world.AreaOf(DeckType.EncounterDeck).Cards.Select(card => card.ObjectId)];
        var events = new List<GameEvent>();

        Elimination.Eliminate(world, world.Facts, 0, "test", events);

        Assert.True(world.Seats[0].Eliminated);
        Assert.False(world.Seats[1].Eliminated);
        Assert.Equal(DeckType.RemovedArea, host.Area.Type);
        Assert.Same(attached, Assert.Single(world.Seats[1].Deck.Cards));
        Assert.Equal(encounter[..^1], world.AreaOf(DeckType.EncounterDeck).Cards.Select(card => card.ObjectId));
        Assert.Single(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Single(events.OfType<CardsMoved>(), move => move.Verb == "Reset");
        Assert.Contains(events.OfType<CardDetached>(), change =>
            change.Card == attached.ObjectId && change.Host == host.ObjectId);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static World Board()
    {
        var world = new World(new Printed(), players: 2);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"player {player}");
            seat.IdentityCard = world.CreateCard(player == 0 ? "09001b,09001a" : "other", seat.Hero);
            world.CreateCard("ordinary", seat.Deck);
        }
        for (int count = 0; count < 3; count++)
            world.CreateCard("treachery", world.AreaOf(DeckType.EncounterDeck));
        return world;
    }

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "09001b" or "other" => CardKind.AlterEgo,
            "upgrade" => CardKind.Upgrade,
            "treachery" => CardKind.Treachery,
            _ => CardKind.Event,
        };
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
