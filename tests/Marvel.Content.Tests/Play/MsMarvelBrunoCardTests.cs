using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MsMarvelBrunoCardTests
{
    [Fact]
    [Rule("rr:in-play-and-out-of-play.10")]
    public void AlterEgoStoragePaysExhaustionAndStoresExactlyOneOwnHandCardFacedown()
    {
        // "Facedown cards attached to in-play cards are out of play."
        Card host = null!;
        Card held = null!;
        var (game, world) = Playing(board =>
        {
            host = InPlay(board, "05007");
            held = board.Seats[0].Hand.Cards[0];
        }, hero: false, heroes: ["spider_man", "captain_marvel"], abilities: AuthoredCards.Runner());
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == host.ObjectId).Id));
        Assert.False(host.Ready);
        Assert.Equal(world.Seats[0].Hand.Cards.Select(c => c.ObjectId), game.Pending!.Affordances.Select(a => a.AnchorId));
        var before = world.Digest().Canonical();
        Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(world.Seats[1].Hand.Cards[0].ObjectId)));
        Assert.Equal(before, world.Digest().Canonical());
        game.Resolve(Decision.Take(held.ObjectId));
        Assert.Same(held, Assert.Single(BeneathCards.Of(world, host)));
        Assert.False(held.FaceUp);
        Assert.False(DeckTypes.IsInPlay(held.Area.Type));
        Assert.Equal(0, held.Owner);
        Assert.True(game.IsRootPrompt);
        Assert.DoesNotContain(game.Pending!.Affordances, a => a.AnchorId == host.ObjectId);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(true, 0)]
    [InlineData(true, 3)]
    public void RetrievalInEitherFormTakesOneBoundedGroupAndRetainsCostForEmptyGroup(bool hero, int count)
    {
        Card host = null!;
        Card[] stored = [];
        var (game, world) = Playing(board =>
        {
            host = InPlay(board, "05007");
            stored = Enumerable.Range(0, 4).Select(_ => board.CreateCard("01005", board.Seats[0].Hand)).ToArray();
            foreach (var card in stored) BeneathCards.Place(board, card, host, false, "fixture", []);
        }, hero: hero, abilities: AuthoredCards.Runner());
        var actions = game.Pending!.Affordances.Where(a => a.AnchorId == host.ObjectId).ToArray();
        Assert.Equal(hero ? 1 : 2, actions.Length);
        var action = actions.Last();
        game.Resolve(Decision.Take(action.Id));
        Assert.False(host.Ready);
        var prompt = game.Pending!;
        var choice = Assert.Single(prompt.Affordances);
        Assert.Equal(0, choice.Targets!.Min);
        Assert.Equal(3, choice.Targets.Max);
        Assert.Equal(stored.Select(c => c.ObjectId), choice.Targets.Legal);
        var before = world.Digest().Canonical();
        Assert.Throws<ArgumentException>(() => game.Resolve(new Decision(choice.Id, stored.Select(c => c.ObjectId).ToArray())));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Same(prompt, game.Pending);
        game.Resolve(new Decision(choice.Id, stored.AsEnumerable().Reverse().Take(count).Select(c => c.ObjectId).ToArray()));
        Assert.Equal(4 - count, BeneathCards.Of(world, host).Count);
        Assert.All(stored.AsEnumerable().Reverse().Take(count), c => { Assert.Same(world.Seats[0].Hand, c.Area); Assert.True(c.FaceUp); });
        Assert.False(host.Ready);
        Assert.True(game.IsRootPrompt);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void StorageIsAbsentInHeroFormOrWhileExhausted(bool hero, bool exhausted)
    {
        Card host = null!;
        var (game, world) = Playing(board =>
        {
            host = InPlay(board, "05007");
            if (exhausted) host.Exhaust();
        }, hero: hero, abilities: AuthoredCards.Runner());
        Assert.DoesNotContain(game.Pending!.Affordances, a => a.AnchorId == host.ObjectId);
        Assert.Empty(BeneathCards.Of(world, host));
    }

    [Fact]
    [Rule("rr:attach-to.1")]
    public void HostDepartureDiscardsStoredCardsToTheirOwners()
    {
        // "If a card is attached to a game element that leaves play, discard the attachment."
        Card host = null!;
        Card own = null!;
        Card foreign = null!;
        var (_, world) = Playing(board =>
        {
            host = InPlay(board, "05007");
            own = board.Seats[0].Hand.Cards[0];
            foreign = board.Seats[1].Hand.Cards[0];
            BeneathCards.Place(board, own, host, false, "fixture", []);
            BeneathCards.Place(board, foreign, host, false, "fixture", []);
        }, heroes: ["spider_man", "captain_marvel"], abilities: AuthoredCards.Runner());
        Discard.Card(world, host, "fixture", []);
        Assert.Empty(BeneathCards.Of(world, host));
        Assert.Equal(DeckType.DiscardPile, own.Area.Type);
        Assert.Equal(DeckType.DiscardPile, foreign.Area.Type);
        Assert.Equal(0, own.Owner);
        Assert.Equal(1, foreign.Owner);
        Assert.True(own.FaceUp && foreign.FaceUp);
    }
}
