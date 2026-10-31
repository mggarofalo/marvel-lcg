using System.Net;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Tests;
using Marvel.Transport;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicHulkSetupTests : TransportTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompleteSetupCatalogCrossesBothTransports(bool socket)
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        var request = EngineRequest.ReadSetup("hulk-catalog");
        var response = socket
            ? await ExchangeOverSocket(new SocketEngineServer(host, IPAddress.Loopback, 0), request)
            : await new InProcessTransport(host).ExchangeAsync(request, TestContext.Current.CancellationToken);
        Assert.Null(response.Error);
        Assert.Equal(15, response.Setup!.Heroes.Count);
        Assert.Equal(39, response.Setup.Scenarios.Count);
        Assert.Equal("Hulk", response.Setup.Heroes.Single(hero => hero.Key == "hulk").Name);
        Assert.Contains(response.Setup.Heroes, hero => hero.Key == "ant_man");
        Assert.Null(response.Capability);
        Assert.Null(response.World);
    }

    [Theory]
    [InlineData("rhino")]
    [InlineData("rhino_expert")]
    [InlineData("absorbing_man")]
    [InlineData("absorbing_man_expert")]
    public void PublishedStarterAndEncounterComponentsReachPlayAtOneThroughFourSeats(string scenario)
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var catalog = factory.DiscoverDecks();
        var hero = catalog.Heroes.Single(item => item.Key == "hulk");
        Assert.Equal(15, catalog.Heroes.Count);
        Assert.Equal(40, hero.Starter.Count);
        Assert.Equal(15, hero.Signature.Count);
        Assert.True(factory.AssessDeck(hero.Key, hero.Starter).IsValid);
        foreach (int count in Enumerable.Range(1, 4))
        {
            string[] others = ["spider_man", "captain_marvel", "hawkeye"];
            string[] heroes = [.. others.Take(count - 1), "hulk"];
            var specification = new GameSpecification(scenario, heroes, null, 851);
            var opened = new EngineHost(factory).Exchange(EngineRequest.OpenGame("open", "hulk-setup", specification));
            Assert.Null(opened.Error);
            Assert.NotNull(opened.Capability);
            Assert.NotNull(opened.World);
            var game = factory.Create(specification).Game;
            Assert.Equal(GamePhase.Mulligan, game.Phase);
            var seat = game.State.Seats[count - 1];
            Assert.Equal("10001b", seat.IdentityCard.FaceId);
            Assert.Equal(5, seat.Hand.Cards.Count);
            Assert.Equal(hero.Starter.Order(StringComparer.Ordinal), seat.Deck.Cards.Concat(seat.Hand.Cards)
                .Select(card => card.FaceId).Order(StringComparer.Ordinal));
            Assert.All(seat.Deck.Cards.Concat(seat.Hand.Cards), card => Assert.Equal(count - 1, card.Owner));
            Assert.Single(game.State.Cards, card => card.FaceId == "10025");
            Assert.Single(game.State.Cards, card => card.FaceId == "10026");
            Assert.Single(game.State.Cards, card => card.FaceId == "10027");
            Assert.Equal(3, game.State.Cards.Count(card => card.FaceId == "10028"));
            Assert.All(game.State.Cards.Where(card => card.FaceId is "10025" or "10026" or "10027" or "10028"),
                card => Assert.Equal(-1, card.Owner));
            for (int index = 0; index < count; index++)
                game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));
            Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        }
    }
}
