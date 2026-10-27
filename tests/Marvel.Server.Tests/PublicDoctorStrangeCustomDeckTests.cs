using Marvel.Protocol;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicDoctorStrangeCustomDeckTests
{
    [Theory]
    [InlineData("09037", "she_hulk", 3)]
    [InlineData("09038", "spider_man", 3)]
    [InlineData("09039", "captain_america", 1)]
    public void ExtraCardsEnterLegalCustomDecksWithTheirPrintedCopyLimits(string extra, string donorKey, int limit)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var catalog = factory.DiscoverDecks();
        var hero = catalog.Heroes.Single(item => item.Key == "doctor_strange");
        var donor = catalog.Heroes.Single(item => item.Key == donorKey);
        string[] custom = [.. hero.Signature, .. donor.Starter.Skip(donor.Signature.Count).SkipLast(limit),
            .. Enumerable.Repeat(extra, limit)];
        Assert.DoesNotContain(extra, hero.Starter);
        Assert.Contains(catalog.Cards, card => card.Face == extra);
        Assert.Equal(40, custom.Length);
        Assert.True(factory.AssessDeck(hero.Key, custom).IsValid);
        Assert.False(factory.AssessDeck(hero.Key, [.. custom, extra]).IsValid);
        var host = new EngineHost(factory);
        var opened = host.Exchange(EngineRequest.OpenGame("custom", "strange-extra-" + extra,
            new GameSpecification("rhino", [hero.Key], [], 3, [new(custom)])));
        Assert.Null(opened.Error);
        var game = factory.Create(new GameSpecification("rhino", [hero.Key], [], 3, [new(custom)])).Game;
        Assert.Equal(custom.Order(StringComparer.Ordinal), game.State.Seats[0].Deck.Cards.Concat(game.State.Seats[0].Hand.Cards)
            .Select(card => card.FaceId).Order(StringComparer.Ordinal));
        Assert.Equal(5, game.State.Seats[0].InvocationDeck!.Deck.Cards.Count);
    }

    [Fact]
    public void ProtectionSubstitutionKeepsInvocationsSeparateAndOrdinaryDeckRejectsThem()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var hero = factory.DiscoverDecks().Heroes.Single(item => item.Key == "doctor_strange");
        string[] custom = [.. hero.Starter.SkipLast(1), "01077"];
        Assert.True(factory.AssessDeck(hero.Key, custom).IsValid);
        Assert.NotNull(factory.Create(new GameSpecification("rhino", [hero.Key], [], 3, [new(custom)])));
        Assert.False(factory.AssessDeck(hero.Key, [.. custom, "09032"]).CanExtend);
        Assert.Throws<ArgumentException>(() => factory.Create(new GameSpecification("rhino", [hero.Key], [], 3,
            [new([.. custom, "09032"])])));
        var quantities = factory.DeckQuantities(hero.Key, hero.Signature);
        Assert.Equal([0], quantities["09001a"]);
        Assert.Equal([0], quantities["09001b"]);
        Assert.False(quantities.ContainsKey("10001a"));
    }
}
