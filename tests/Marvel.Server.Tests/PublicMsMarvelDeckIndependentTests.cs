using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicMsMarvelDeckIndependentTests
{
    private static readonly string[] ReviewedHeroes =
        ["ant_man", "black_panther", "black_widow", "captain_america", "captain_marvel", "doctor_strange", "hawkeye", "hulk", "iron_man",
            "ms_marvel", "quicksilver", "she_hulk", "spider_man", "spider_woman", "thor", "wasp"];

    [Theory]
    [InlineData("05030", "she_hulk")]
    [InlineData("05031", "spider_man")]
    [InlineData("05032", "captain_america")]
    [InlineData("05033", "ms_marvel")]
    public void EachExtraPoolCardEntersALegalCustomDeckAndThePublicDeal(string extra, string aspectStarter)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var catalog = factory.DiscoverDecks();
        var hero = catalog.Heroes.Single(item => item.Key == "ms_marvel");
        var donor = catalog.Heroes.Single(item => item.Key == aspectStarter);
        string[] extras = donor.Starter.Skip(donor.Signature.Count).ToArray();
        string[] custom = [.. hero.Signature, .. extras.SkipLast(1), extra];

        Assert.Equal(40, custom.Length);
        Assert.Contains(catalog.Cards, card => card.Face == extra);
        Assert.True(factory.AssessDeck(hero.Key, custom).IsValid);
        var host = new EngineHost(factory);
        var opened = host.Exchange(EngineRequest.OpenGame("custom", "ms-extra-" + extra,
            new GameSpecification("rhino", [hero.Key], [], 3, [new(custom)])));

        Assert.Null(opened.Error);
        Assert.NotNull(opened.Capability);
        var dealt = factory.Create(new GameSpecification("rhino", [hero.Key], [], 3, [new(custom)])).Game;
        Assert.Equal(GamePhase.Mulligan, dealt.Phase);
        var seat = Assert.Single(dealt.State.Seats);
        Assert.Equal(custom.Order(StringComparer.Ordinal), seat.Deck.Cards.Concat(seat.Hand.Cards)
            .Select(card => card.FaceId).Order(StringComparer.Ordinal));
        Assert.Equal("05001b", seat.IdentityCard.FaceId);
    }

    [Fact]
    public void AdjacentUnadmittedProductRemainsExcluded()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        Assert.Equal(ReviewedHeroes,
            factory.DiscoverSetup().Heroes.Select(hero => hero.Key).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(factory.DiscoverDecks().Cards, card => card.Face.StartsWith("16", StringComparison.Ordinal));
        var rejected = new EngineHost(factory).Exchange(EngineRequest.OpenGame("adjacent", "adjacent-groot",
            new GameSpecification("rhino", ["groot"], [], 3)));
        Assert.NotNull(rejected.Error);
        Assert.Null(rejected.Capability);
    }
}
