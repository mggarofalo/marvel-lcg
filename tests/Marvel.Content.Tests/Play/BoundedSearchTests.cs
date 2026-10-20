using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BoundedSearchTests
{
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [Rule("rr:search.2")]
    public void TopCardBoundAppliesBeforePrintedCriteria(bool aspectOnly, bool matchesAspect)
    {
        // rr:search.2: cards being searched are not considered to leave the searched area.
        Card? source = null;
        var runner = Runner(AuthoredCards.AuntMay, "Action",
            """{"seq":[{"returnToHand":{"cardsIn":{"area":"yourDeck","top":2,"kind":"Event","aspectOnly":"ASPECT"}}},{"heal":{"card":"you","amount":1}}]}"""
                .Replace("ASPECT", aspectOnly ? "true" : "false", StringComparison.Ordinal), cost: """{"exhaust":"this"}""");
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(1);
        }, abilities: runner);
        var deck = world.Seats[0].Deck;
        foreach (var card in deck.Cards.ToArray()) World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        var below = world.CreateCard("01060", deck);
        var middle = world.CreateCard("01088", deck);
        var top = world.CreateCard(aspectOnly && matchesAspect ? "01054" : "01005", deck);
        World.MoveToTop(below, deck);
        World.MoveToTop(middle, deck);
        World.MoveToTop(top, deck);
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Same(matchesAspect ? world.Seats[0].Hand : deck, top.Area);
        Assert.Same(deck, middle.Area);
        Assert.Same(deck, below.Area);
        Assert.Equal(matchesAspect ? 2 : 3, deck.Cards.Count);
    }

    [Fact]
    [Rule("rr:search.2")]
    public void PlayerDiscardSearchCannotSelectAnotherPlayersMatchingCard()
    {
        Card? source = null;
        var runner = Runner(AuthoredCards.AuntMay, "Action",
            """{"returnToHand":{"cardsIn":{"area":"yourDiscardPile","title":"Swinging Web Kick"}}}""", cost: """{"exhaust":"this"}""");
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01005", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
            board.CreateCard("01005", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var other = Assert.Single(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1).Cards);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, other.Area.Type);
        Assert.Equal(1, other.Owner);
    }
}
