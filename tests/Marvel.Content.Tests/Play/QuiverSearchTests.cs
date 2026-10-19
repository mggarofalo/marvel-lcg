using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class QuiverSearchTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:search.3")]
    public void QuiverStoresOnlyAnArrowAmongTheTopFiveAndShufflesAfterSearching(bool matchingTop)
    {
        // ruling:b216c9af67e04d26: after the top-five search, "the entire deck is shuffled."
        // ruling:3bdad6b3608ce352 permits the paid search with a nonempty deck but no matching Arrow.
        Card? quiver = null;
        var (game, world) = Playing(board => quiver = InPlay(board, "04003"), hero: true, abilities: AuthoredCards.Runner());
        var deck = world.Seats[0].Deck;
        foreach (var card in deck.Cards.ToArray()) World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        var below = world.CreateCard("04006", deck);
        World.MoveToTop(below, deck);
        for (int index = 0; index < 4; index++) World.MoveToTop(world.CreateCard("01088", deck), deck);
        var top = world.CreateCard(matchingTop ? "04006" : "01088", deck);
        World.MoveToTop(top, deck);
        long randomWords = world.Random.Generator.WordsConsumed;
        int handSize = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == quiver!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        if (matchingTop)
        {
            var choice = Assert.Single(game.Pending!.Affordances);
            Assert.Equal(top.ObjectId, choice.AnchorId);
            game.Resolve(Decision.Take(choice.Id));
        }

        Assert.True(world.Random.Generator.WordsConsumed > randomWords);
        Assert.False(quiver!.Ready);
        Assert.Same(deck, below.Area);
        Assert.Equal(handSize, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(matchingTop ? 1 : 0, BeneathCards.Of(world, quiver).Count);
        Assert.Equal(matchingTop ? 5 : 6, deck.Cards.Count);
        if (matchingTop)
        {
            Assert.Same(top, Assert.Single(BeneathCards.Of(world, quiver)));
            Assert.True(top.FaceUp);
            Assert.False(DeckTypes.IsInPlay(top.Area.Type));
        }
    }
}
