using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicBlackWidowSetupTests
{
    [Fact]
    public void DiscoveryAndProductionSetupIncludeExactStarterAtOneThroughFourSeats()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var discovery = new EngineHost(factory).Exchange(EngineRequest.ReadSetup("setup"));
        Assert.Null(discovery.Error);
        Assert.Equal(15, discovery.Setup!.Heroes.Count);
        Assert.Contains(discovery.Setup.Heroes, hero => hero.Key == "black_widow");
        var hero = factory.DiscoverDecks().Heroes.Single(hero => hero.Key == "black_widow");
        Assert.Equal(40, hero.Starter.Count);
        Assert.True(factory.AssessDeck(hero.Key, hero.Starter).IsValid);
        foreach (string scenario in new[] { "rhino", "rhino_expert", "absorbing_man", "absorbing_man_expert" })
        foreach (int count in Enumerable.Range(1, 4))
        {
            string[] heroes = ["black_widow", "captain_marvel", "spider_man", "hawkeye"];
            var game = factory.Create(new GameSpecification(scenario, heroes.Take(count).ToArray(), null, 693)).Game;
            Assert.Equal(GamePhase.Mulligan, game.Phase);
            Assert.Equal(count, game.State.Seats.Count);
            var seat = game.State.Seats[0];
            Assert.Equal("08001b", seat.IdentityCard.FaceId);
            Assert.Equal(hero.Starter.Order(StringComparer.Ordinal), seat.Deck.Cards.Concat(seat.Hand.Cards)
                .Select(card => card.FaceId).Order(StringComparer.Ordinal));
            Assert.Single(game.State.Cards, card => card.FaceId == "08025");
            Assert.Equal(5, game.State.Cards.Count(card => card.FaceId is "08026" or "08027" or "08028" or "08029"));
        }
        string[] custom = [.. hero.Starter.SkipLast(1), "08033"];
        Assert.True(factory.AssessDeck(hero.Key, custom).IsValid);
        Assert.NotNull(factory.Create(new GameSpecification("rhino", [hero.Key], null, 693, [new(custom)])));
    }

}
