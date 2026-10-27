using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>Synthetic boards distinguish ownership from location during elimination.</summary>
public sealed class EliminationOwnedPlacementTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:player-elimination.step.2")]
    [Rule("rr:player-elimination.step.4")]
    [Rule("rr:player-elimination.step.5")]
    [Rule("rr:discard.4")]
    public void OwnedCardsLeaveEveryPlayAreaAfterMinionTransfer(bool transferred)
    {
        // Step 2 retains "attached cards" during transfer; step 4 then places
        // "each card owned by the eliminated player" in that player's discard.
        var world = new World(new Facts(), players: 2);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("hero", seat.Hero);
        }
        var deckFirst = world.CreateCard("resource", world.Seats[0].Deck);
        var deckSecond = world.CreateCard("resource", world.Seats[0].Deck);
        World.MoveToTop(deckFirst, world.Seats[0].Deck);
        var host = world.CreateCard("minion", world.AreaOf(
            transferred ? DeckType.EngagedEnemiesArea : DeckType.SideSchemesArea,
            transferred ? PlayArea.Of(0) : PlayArea.Villains, cardOwner: -1));
        var area = world.AreaOf(DeckType.BeneathCardsPile, host.Area.PlayArea, host.ObjectId, cardOwner: 0);
        var first = world.CreateCard("resource", area);
        var second = world.CreateCard("resource", area);
        World.MoveToTop(first, area);
        var foreign = world.CreateCard("resource", world.CreateArea(
            DeckType.BeneathCardsPile, playArea: host.Area.PlayArea, host: host.ObjectId, cardOwner: 1));
        var encounter = world.CreateCard("resource", world.CreateArea(
            DeckType.BeneathCardsPile, playArea: host.Area.PlayArea, host: host.ObjectId, cardOwner: -1));
        var discarded = world.CreateCard("resource", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        var removed = world.CreateCard("resource", world.AreaOf(DeckType.RemovedArea, cardOwner: 0));
        var other = world.CreateCard("resource", world.Seats[1].Hand);
        string before = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        var layout = EliminationLayout.Calculate(new WorldEliminationLayout(world), 0);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Contains(first.ObjectId, layout.Leaving);
        Assert.Contains(second.ObjectId, layout.Leaving);
        Assert.DoesNotContain(foreign.ObjectId, layout.Leaving);
        Assert.DoesNotContain(encounter.ObjectId, layout.Leaving);
        Assert.DoesNotContain(other.ObjectId, layout.Leaving);
        Assert.DoesNotContain(removed.ObjectId, layout.Leaving);
        var events = new List<GameEvent>();

        Elimination.Eliminate(world, world.Facts, 0, "test", events);

        Assert.Equal(DeckType.RemovedArea, first.Area.Type);
        Assert.Equal(DeckType.RemovedArea, second.Area.Type);
        Assert.Equal(host.ObjectId, foreign.Area.Host);
        Assert.Equal(host.ObjectId, encounter.Area.Host);
        Assert.Same(world.Seats[1].Hand, other.Area);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Single(events.OfType<CardsMoved>(), move =>
            move.Cards.Any(card => card.Card == discarded.ObjectId));
        Assert.DoesNotContain(events.OfType<CardsMoved>(), move =>
            move.Cards.Any(card => card.Card == removed.ObjectId));
        var discards = events.OfType<CardsMoved>().Where(move => move.Verb == "Discard")
            .SelectMany(move => move.Cards.Select(card => card.Card)).ToArray();
        Assert.Equal([deckFirst.ObjectId, deckSecond.ObjectId, first.ObjectId, second.ObjectId], discards);
        foreach (var card in new[] { first, second })
        {
            var moves = events.OfType<CardsMoved>()
                .Where(move => move.Cards.Any(landing => landing.Card == card.ObjectId)).ToArray();
            Assert.Equal(transferred ? 3 : 2, moves.Length);
            if (transferred) Assert.Equal("Engage", moves[0].Verb);
            Assert.Equal(nameof(DeckType.DiscardPile), moves[^2].To.Zone);
            Assert.Equal(nameof(DeckType.RemovedArea), moves[^1].To.Zone);
            Assert.Contains(events.OfType<CardDetached>(), detached => detached.Card == card.ObjectId);
        }
    }

    private sealed class Facts : ICardFacts
    {
        public CardKind Kind(string id) => id == "hero" ? CardKind.Hero : id == "minion" ? CardKind.Minion : CardKind.Resource;
        public IReadOnlyList<string> Traits(string id) => [];
        public IReadOnlyDictionary<string, string> Attributes(string id) => new Dictionary<string, string>();
        public long PrintedValue(string id, string attribute, int players, long fallback = 0) => fallback;
    }
}