using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicThorSetupTests : EngineHostTestBase
{
    [Fact]
    public void DiscoveryAndProductionSetupIncludeExactStarterAtOneThroughFourSeats()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var discovery = new EngineHost(factory).Exchange(EngineRequest.ReadSetup("setup"));
        Assert.Null(discovery.Error);
        Assert.Equal(16, discovery.Setup!.Heroes.Count);
        Assert.Contains(discovery.Setup.Heroes, hero => hero.Key == "thor");
        var hero = factory.DiscoverDecks().Heroes.Single(hero => hero.Key == "thor");
        Assert.Equal(40, hero.Starter.Count);
        Assert.True(factory.AssessDeck(hero.Key, hero.Starter).IsValid);
        foreach (string scenario in new[] { "rhino", "rhino_expert", "absorbing_man", "absorbing_man_expert" })
        foreach (int count in Enumerable.Range(1, 4))
        {
            string[] heroes = ["thor", "captain_marvel", "spider_man", "hawkeye"];
            var game = factory.Create(new GameSpecification(scenario, heroes.Take(count).ToArray(), null, 693)).Game;
            Assert.Equal(GamePhase.Mulligan, game.Phase);
            Assert.Equal(count, game.State.Seats.Count);
            var seat = game.State.Seats[0];
            Assert.Equal("06001b", seat.IdentityCard.FaceId);
            Assert.Equal(hero.Starter.Order(StringComparer.Ordinal), seat.Deck.Cards.Concat(seat.Hand.Cards)
                .Select(card => card.FaceId).Order(StringComparer.Ordinal));
            Assert.Single(game.State.Cards, card => card.FaceId == "06026");
            Assert.Equal(5, game.State.Cards.Count(card => card.FaceId is "06027" or "06028" or "06029" or "06030"));
        }
        string[] custom = [.. hero.Starter.SkipLast(1), "06034"];
        Assert.True(factory.AssessDeck(hero.Key, custom).IsValid);
        Assert.NotNull(factory.Create(new GameSpecification("rhino", [hero.Key], null, 693, [new(custom)])));
    }

}
