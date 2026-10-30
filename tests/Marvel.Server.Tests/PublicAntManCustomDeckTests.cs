using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicAntManCustomDeckTests
{
    [Theory]
    [InlineData("12030", "she_hulk")]
    [InlineData("12031", "spider_man")]
    [InlineData("12032", "doctor_strange")]
    [InlineData("12033", "ant_man")]
    public void AllThreeExtraCopiesEnterADeckAndFourthCopyCannotPublishASession(string extra, string donorKey)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var catalog = factory.DiscoverDecks();
        var hero = catalog.Heroes.Single(item => item.Key == "ant_man");
        var donor = catalog.Heroes.Single(item => item.Key == donorKey);
        string[] custom = [.. hero.Signature, .. donor.Starter.Skip(donor.Signature.Count), extra, extra, extra];
        Assert.Equal(43, custom.Length);
        Assert.Contains(catalog.Cards, card => card.Face == extra);
        Assert.Equal([0, 1, 2, 3], factory.DeckQuantities(hero.Key, hero.Signature)[extra]);
        Assert.True(factory.AssessDeck(hero.Key, custom).IsValid);
        var specification = new GameSpecification("rhino", [hero.Key], [], 851, [new(custom)]);
        var opened = new EngineHost(factory).Exchange(EngineRequest.OpenGame("custom", "ant-man-extra-" + extra, specification));
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
        var rejected = new EngineHost(factory, store: store).Exchange(EngineRequest.OpenGame("invalid", "ant-man-fourth-" + extra,
            specification with { PlayerDecks = [new(invalid)] }));
        Assert.NotNull(rejected.Error);
        Assert.Null(rejected.Capability);
        Assert.Empty(store.Load());
    }

}
