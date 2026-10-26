using Marvel.Protocol;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicThorAdmissionIndependentTests
{
    [Fact]
    public void ThorCustomDeckRetainsFourthSeatOwnershipAndRejectsAnotherIdentity()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var catalog = factory.DiscoverDecks();
        string[] heroes = ["spider_man", "captain_marvel", "hawkeye", "thor"];
        var decks = heroes.Select(key => new PlayerDeckRecipe(catalog.Heroes.Single(hero => hero.Key == key).Starter)).ToArray();
        var thor = catalog.Heroes.Single(hero => hero.Key == "thor");
        string[] custom = [.. thor.Starter.SkipLast(1), "06034"];
        decks[3] = new(custom);
        Assert.True(factory.AssessDeck("thor", custom).IsValid);
        Assert.False(factory.AssessDeck("spider_man", custom).IsValid);
        var game = factory.Create(new GameSpecification("rhino", heroes, [], 3, decks)).Game;
        Assert.Equal("06001b", game.State.Seats[3].IdentityCard.FaceId);
        Assert.Equal(custom.Order(StringComparer.Ordinal), game.State.Seats[3].Deck.Cards
            .Concat(game.State.Seats[3].Hand.Cards).Select(card => card.FaceId).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(game.State.Seats[0].Deck.Cards.Concat(game.State.Seats[0].Hand.Cards),
            card => card.FaceId == "06034");

        decks[0] = new(custom);
        var store = new MemorySessionStore();
        var rejected = new EngineHost(factory, store: store).Exchange(EngineRequest.OpenGame(
            "wrong-signature", "thor-wrong-seat", new GameSpecification("rhino", heroes, [], 3, decks)));
        Assert.NotNull(rejected.Error);
        Assert.Null(rejected.Capability);
        Assert.Empty(store.Load());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void UnsupportedSeatCountsCannotPublishAThorGame(int count)
    {
        // Core Learn to Play: "a cooperative card game for one to four players".
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        string[] heroes = ["thor", "spider_man", "captain_marvel", "hawkeye", "ms_marvel"];
        var specification = new GameSpecification("rhino", heroes.Take(count).ToArray(), [], 3);
        Assert.Throws<ArgumentOutOfRangeException>(() => factory.Create(specification));
        var store = new MemorySessionStore();
        var rejected = new EngineHost(factory, store: store).Exchange(EngineRequest.OpenGame(
            "seat-count", "thor-seat-count", specification));
        Assert.NotNull(rejected.Error);
        Assert.Null(rejected.Capability);
        Assert.Empty(store.Load());
    }
}
