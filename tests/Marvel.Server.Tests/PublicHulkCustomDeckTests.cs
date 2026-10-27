using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicHulkCustomDeckTests
{
    [Theory]
    [InlineData("10029", "spider_man")]
    [InlineData("10030", "captain_america")]
    [InlineData("10031", "ms_marvel")]
    [InlineData("10032", "hulk")]
    public void AllThreeExtraCopiesEnterADeckAndFourthCopyCannotPublishASession(string extra, string donorKey)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var catalog = factory.DiscoverDecks();
        var hero = catalog.Heroes.Single(item => item.Key == "hulk");
        var donor = catalog.Heroes.Single(item => item.Key == donorKey);
        string[] custom = [.. hero.Signature, .. donor.Starter.Skip(donor.Signature.Count), extra, extra, extra];
        Assert.Equal(43, custom.Length);
        Assert.Contains(catalog.Cards, card => card.Face == extra);
        Assert.Equal([0, 1, 2, 3], factory.DeckQuantities(hero.Key, hero.Signature)[extra]);
        Assert.True(factory.AssessDeck(hero.Key, custom).IsValid);
        var specification = new GameSpecification("rhino", [hero.Key], [], 851, [new(custom)]);
        var opened = new EngineHost(factory).Exchange(EngineRequest.OpenGame("custom", "hulk-extra-" + extra, specification));
        Assert.Null(opened.Error);
        Assert.NotNull(opened.Capability);
        var dealt = factory.Create(specification).Game;
        Assert.Equal(GamePhase.Mulligan, dealt.Phase);
        var seat = Assert.Single(dealt.State.Seats);
        Assert.Equal(custom.Order(StringComparer.Ordinal), seat.Deck.Cards.Concat(seat.Hand.Cards)
            .Select(card => card.FaceId).Order(StringComparer.Ordinal));
        string[] invalid = [.. custom, extra];
        Assert.False(factory.AssessDeck(hero.Key, invalid).CanExtend);
        var store = new MemorySessionStore();
        var rejected = new EngineHost(factory, store: store).Exchange(EngineRequest.OpenGame("invalid", "hulk-fourth-" + extra,
            specification with { PlayerDecks = [new(invalid)] }));
        Assert.NotNull(rejected.Error);
        Assert.Null(rejected.Capability);
        Assert.Empty(store.Load());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void OrderedCustomDeckBelongsToTheSelectedSeat(int owner)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var catalog = factory.DiscoverDecks();
        var hulk = catalog.Heroes.Single(item => item.Key == "hulk");
        string[] custom = [.. hulk.Starter.SkipLast(1), "10032"];
        Assert.True(factory.AssessDeck(hulk.Key, custom).IsValid);
        var heroes = new List<string> { "spider_man", "captain_marvel", "hawkeye" };
        heroes.Insert(owner, hulk.Key);
        var decks = heroes.Select(key => new PlayerDeckRecipe(key == hulk.Key
            ? custom : catalog.Heroes.Single(item => item.Key == key).Starter)).ToArray();
        var specification = new GameSpecification("rhino_expert", heroes, [], 851, decks);
        var opened = new EngineHost(factory).Exchange(EngineRequest.OpenGame("owned", "hulk-owned", specification));
        Assert.Null(opened.Error);
        Assert.NotNull(opened.Capability);
        var game = factory.Create(specification).Game;
        for (int seat = 0; seat < heroes.Count; seat++)
        {
            var player = game.State.Seats[seat];
            Assert.Equal(decks[seat].Cards.Order(StringComparer.Ordinal),
                player.Hand.Cards.Concat(player.Deck.Cards).Select(card => card.FaceId).Order(StringComparer.Ordinal));
            Assert.All(player.Hand.Cards.Concat(player.Deck.Cards), card => Assert.Equal(seat, card.Owner));
        }
    }

    [Theory]
    [InlineData("10020")]
    [InlineData("10021")]
    [InlineData("10022")]
    public void PrintedSingletonResourcesRejectASecondCopy(string face)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var hero = factory.DiscoverDecks().Heroes.Single(item => item.Key == "hulk");
        Assert.True(factory.AssessDeck(hero.Key, hero.Starter).IsValid);
        Assert.False(factory.AssessDeck(hero.Key, [.. hero.Starter, face]).CanExtend);
    }
}
