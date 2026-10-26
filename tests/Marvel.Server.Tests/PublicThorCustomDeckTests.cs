using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicThorCustomDeckTests
{
    [Theory]
    [InlineData("06031", "spider_man")]
    [InlineData("06032", "captain_america")]
    [InlineData("06033", "ms_marvel")]
    [InlineData("06034", "thor")]
    public void EachExtraPoolCardEntersALegalCustomDeckAndThePublicDeal(string extra, string aspectStarter)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var catalog = factory.DiscoverDecks();
        var hero = catalog.Heroes.Single(item => item.Key == "thor");
        var donor = catalog.Heroes.Single(item => item.Key == aspectStarter);
        string[] extras = donor.Starter.Skip(donor.Signature.Count).ToArray();
        string[] custom = [.. hero.Signature, .. extras.SkipLast(1), extra];

        Assert.Equal(40, custom.Length);
        Assert.Contains(catalog.Cards, card => card.Face == extra);
        Assert.True(factory.AssessDeck(hero.Key, custom).IsValid);
        var host = new EngineHost(factory);
        var opened = host.Exchange(EngineRequest.OpenGame("custom", "thor-extra-" + extra,
            new GameSpecification("rhino", [hero.Key], [], 3, [new(custom)])));

        Assert.Null(opened.Error);
        Assert.NotNull(opened.Capability);
        var dealt = factory.Create(new GameSpecification("rhino", [hero.Key], [], 3, [new(custom)])).Game;
        Assert.Equal(GamePhase.Mulligan, dealt.Phase);
        var seat = Assert.Single(dealt.State.Seats);
        Assert.Equal(custom.Order(StringComparer.Ordinal), seat.Deck.Cards.Concat(seat.Hand.Cards)
            .Select(card => card.FaceId).Order(StringComparer.Ordinal));
        Assert.Equal("06001b", seat.IdentityCard.FaceId);
    }

}
