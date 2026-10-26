using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.ActionAbilitySupportFixtures;
using static Marvel.Content.Tests.Play.ActionAbilitySupportFixtures6;

namespace Marvel.Content.Tests.Play;

/// <summary>Independent synthetic capability probes; no expansion admission is implied.</summary>
public sealed class DeckBottomMovementReviewTests
{
    [Fact]
    [Rule("rr:removed-from-the-game.2")]
    public void RemovedSourceRefusesPlacementBeforeChangingEitherArea()
    {
        // rr:removed-from-the-game.2: "A card that has been removed from the game cannot reenter the game through any means."
        var (_, world) = Playing(_ => { });
        var card = world.CreateCard("01003", world.AreaOf(DeckType.RemovedArea));
        var before = world.Digest().Canonical();
        var deck = world.Seats[0].Deck.Cards.ToArray();
        long words = world.Random.Generator.WordsConsumed;
        Assert.Throws<InvalidOperationException>(() => CardPlacement.MoveToBottom(card, world.Seats[0].Deck));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(deck, world.Seats[0].Deck.Cards);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
    }

    [Fact]
    [Rule("rr:in-play-and-out-of-play.4")]
    public void DepartedHostDestinationRefusesPlacementBeforeRemovingSource()
    {
        // rr:in-play-and-out-of-play.4: "Card abilities only interact with, and can only target, cards that are in play."
        var (_, world) = Playing(_ => { });
        var host = InPlay(world, "01091");
        var hosted = world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), host.ObjectId);
        World.MoveToTop(host, world.Seats[0].Hand);
        var card = world.CreateCard("01003", world.Seats[0].Hand);
        var before = world.Digest().Canonical();
        var hand = world.Seats[0].Hand.Cards.ToArray();
        Assert.Throws<InvalidOperationException>(() => CardPlacement.MoveToBottom(card, hosted));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(hand, world.Seats[0].Hand.Cards);
        Assert.Empty(hosted.Cards);
    }

    [Fact]
    public void SecondPlayerPlacementUsesResolvingDeckAndObservedCompletion()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "then": {
              "effect": { "placeOnBottomOfDeck": { "titled": "Avengers Mansion" } },
              "then": { "draw": { "player": "you", "count": 1 } }
            } }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? selected = null;
        var (_, world) = Playing(board =>
        {
            source = board.CreateCard(AuthoredCards.AuntMay,
                board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
            selected = board.CreateCard("01091", board.Seats[0].Hand);
            World.MoveToTop(selected, board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1)));
        }, heroes: ["spider_man", "captain_marvel"], abilities: AuthoredCards.Runner());
        var firstDeck = world.Seats[0].Deck.Cards.ToArray();
        var secondDeck = world.Seats[1].Deck.Cards.ToArray();
        var firstHand = world.Seats[0].Hand.Cards.ToArray();
        int held = world.Seats[1].Hand.Cards.Count;
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Current;
        var digest = world.Digest().Canonical();
        var action = Assert.Single(runner.Actions(world, 1), a => a.Card == source!.ObjectId);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Current);
        var result = runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.True(result.IsComplete);
        Assert.Equal(firstDeck, world.Seats[0].Deck.Cards);
        Assert.Equal(firstHand, world.Seats[0].Hand.Cards);
        Assert.Equal(new[] { selected }.Concat(secondDeck.SkipLast(1)), world.Seats[1].Deck.Cards);
        Assert.Equal(held + 1, world.Seats[1].Hand.Cards.Count);
        Assert.Same(secondDeck[^1], world.Seats[1].Hand.Cards[^1]);
        Assert.Equal(0, selected!.Owner);
        Assert.False(source!.Ready);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
    }

    [Theory]
    [InlineData("yourDeck")]
    [InlineData("yourDiscardPile")]
    public void MovementBeforeSingularAreaReadIsRefusedBeforeCost(string area)
    {
        var (_, world) = Playing(board =>
        {
            InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01091", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
            board.CreateCard("01091", board.Seats[0].Deck);
        }, abilities: AuthoredCards.Runner());
        string read = $$"""{ "removeFromGame": { "cardsIn": { "area": "{{area}}", "title": "Avengers Mansion" } } }""";
        var control = Runner(AuthoredCards.AuntMay, "Action", read, cost: """{ "exhaust": "this" }""");
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$"""
            { "seq": [
              { "placeOnBottomOfDeck": { "cardsIn": { "area": "yourDiscardPile", "title": "Avengers Mansion" } } },
              {{read}}
            ] }
            """, cost: """{ "exhaust": "this" }""");
        var digest = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Current;
        Assert.Single(control.Actions(world, 0));
        var refused = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
        Assert.Contains("singular area query", refused.Message);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Current);
    }

    [Fact]
    [Rule("rr:otherwise.1.2")]
    public void MissingSelectionCannotCompletePlacementAndUsesOtherwiseDraw()
    {
        // rr:otherwise.1.2: "It has an effect that cannot at least partially resolve."
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "otherwise": {
              "effect": { "placeOnBottomOfDeck": { "titled": "Avengers Mansion" } },
              "otherwise": { "draw": { "player": "you", "count": 1 } }
            } }
            """);
        Card? source = null;
        var (game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var deck = world.Seats[0].Deck.Cards.ToArray();
        var action = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == source!.ObjectId);
        var result = game.Resolve(Decision.Take(action.Id));
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
        Assert.Equal(deck.SkipLast(1), world.Seats[0].Deck.Cards);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Same(deck[^1], world.Seats[0].Hand.Cards[^1]);
        Assert.DoesNotContain(result.Events.OfType<CardsMoved>(), e => e.Verb == "Place_On_Bottom");
    }
}
