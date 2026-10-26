using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicBlackWidowCustomDeckTests
{
    [Theory]
    [InlineData("08030", "she_hulk")]
    [InlineData("08031", "captain_america")]
    [InlineData("08032", "ms_marvel")]
    [InlineData("08033", "black_widow")]
    public void EachExtraPoolCardEntersALegalCustomDeckAndThePublicDeal(string extra, string aspectStarter)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var catalog = factory.DiscoverDecks();
        var hero = catalog.Heroes.Single(item => item.Key == "black_widow");
        var donor = catalog.Heroes.Single(item => item.Key == aspectStarter);
        string[] extras = donor.Starter.Skip(donor.Signature.Count).ToArray();
        string[] custom = [.. hero.Signature, .. extras.SkipLast(1), extra];

        Assert.Equal(40, custom.Length);
        Assert.Contains(catalog.Cards, card => card.Face == extra);
        Assert.True(factory.AssessDeck(hero.Key, custom).IsValid);
        var host = new EngineHost(factory);
        var opened = host.Exchange(EngineRequest.OpenGame("custom", "black_widow-extra-" + extra,
            new GameSpecification("rhino", [hero.Key], [], 3, [new(custom)])));

        Assert.Null(opened.Error);
        Assert.NotNull(opened.Capability);
        var dealt = factory.Create(new GameSpecification("rhino", [hero.Key], [], 3, [new(custom)])).Game;
        Assert.Equal(GamePhase.Mulligan, dealt.Phase);
        var seat = Assert.Single(dealt.State.Seats);
        Assert.Equal(custom.Order(StringComparer.Ordinal), seat.Deck.Cards.Concat(seat.Hand.Cards)
            .Select(card => card.FaceId).Order(StringComparer.Ordinal));
        Assert.Equal("08001b", seat.IdentityCard.FaceId);
    }

}
