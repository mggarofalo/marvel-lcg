using Marvel.Protocol;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicBlackWidowOwnershipTests
{
    [Fact]
    public void BlackWidowCustomDeckRetainsFourthSeatOwnershipAndRejectsAnotherIdentity()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var catalog = factory.DiscoverDecks();
        string[] heroes = ["spider_man", "captain_marvel", "hawkeye", "black_widow"];
        var decks = heroes.Select(key => new PlayerDeckRecipe(catalog.Heroes.Single(hero => hero.Key == key).Starter)).ToArray();
        var widow = catalog.Heroes.Single(hero => hero.Key == "black_widow");
        string[] custom = [.. widow.Starter.SkipLast(1), "08033"];
        decks[3] = new(custom);
        Assert.True(factory.AssessDeck("black_widow", custom).IsValid);
        Assert.False(factory.AssessDeck("spider_man", custom).IsValid);
        var game = factory.Create(new GameSpecification("rhino", heroes, [], 3, decks)).Game;
        Assert.Equal("08001b", game.State.Seats[3].IdentityCard.FaceId);
        Assert.Equal(custom.Order(StringComparer.Ordinal), game.State.Seats[3].Deck.Cards
            .Concat(game.State.Seats[3].Hand.Cards).Select(card => card.FaceId).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(game.State.Seats[0].Deck.Cards.Concat(game.State.Seats[0].Hand.Cards),
            card => card.FaceId == "08033");

        decks[0] = new(custom);
        var store = new MemorySessionStore();
        var rejected = new EngineHost(factory, store: store).Exchange(EngineRequest.OpenGame(
            "wrong-signature", "black_widow-wrong-seat", new GameSpecification("rhino", heroes, [], 3, decks)));
        Assert.NotNull(rejected.Error);
        Assert.Null(rejected.Capability);
        Assert.Empty(store.Load());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void UnsupportedSeatCountsCannotPublishABlackWidowGame(int count)
    {
        // Core Learn to Play: "a cooperative card game for one to four players".
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        string[] heroes = ["black_widow", "spider_man", "captain_marvel", "hawkeye", "ms_marvel"];
        var specification = new GameSpecification("rhino", heroes.Take(count).ToArray(), [], 3);
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.Create(specification));
        var store = new MemorySessionStore();
        var rejected = new EngineHost(factory, store: store).Exchange(EngineRequest.OpenGame(
            "seat-count", "black_widow-seat-count", specification));
        Assert.NotNull(rejected.Error);
        Assert.Null(rejected.Capability);
        Assert.Empty(store.Load());
    }
}
