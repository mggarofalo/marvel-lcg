using System.Net;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Tests;
using Marvel.Transport;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class PublicAntManSetupTests : TransportTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompleteSetupCatalogCrossesBothTransports(bool socket)
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        var request = EngineRequest.ReadSetup("ant-man-catalog");
        var response = socket
            ? await ExchangeOverSocket(new SocketEngineServer(host, IPAddress.Loopback, 0), request)
            : await new InProcessTransport(host).ExchangeAsync(request, TestContext.Current.CancellationToken);
        Assert.Null(response.Error);
        Assert.Equal(19, response.Setup!.Heroes.Count);
        Assert.Equal(39, response.Setup.Scenarios.Count);
        Assert.Equal("Ant-Man", response.Setup.Heroes.Single(hero => hero.Key == "ant_man").Name);
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
        var hero = catalog.Heroes.Single(item => item.Key == "ant_man");
        Assert.Equal(19, catalog.Heroes.Count);
        Assert.Equal(40, hero.Starter.Count);
        Assert.Equal(15, hero.Signature.Count);
        Assert.Equal(Signature, hero.Signature);
        Assert.Equal(Customization, hero.Starter.Skip(15));
        Assert.True(factory.AssessDeck(hero.Key, hero.Starter).IsValid);
        foreach (int count in Enumerable.Range(1, 4))
        {
            string[] others = ["spider_man", "captain_marvel", "hawkeye"];
            string[] heroes = [.. others.Take(count - 1), "ant_man"];
            var specification = new GameSpecification(scenario, heroes, null, 851);
            var opened = new EngineHost(factory).Exchange(EngineRequest.OpenGame("open", "ant-man-setup", specification));
            Assert.Null(opened.Error);
            Assert.NotNull(opened.Capability);
            Assert.NotNull(opened.World);
            var game = factory.Create(specification).Game;
            Assert.Equal(GamePhase.Mulligan, game.Phase);
            var seat = game.State.Seats[count - 1];
            Assert.Equal("12001b", seat.IdentityCard.FaceId);
            Assert.Equal(6, seat.Hand.Cards.Count);
            Assert.Equal(hero.Starter.Order(StringComparer.Ordinal), seat.Deck.Cards.Concat(seat.Hand.Cards)
                .Select(card => card.FaceId).Order(StringComparer.Ordinal));
            Assert.All(seat.Deck.Cards.Concat(seat.Hand.Cards), card => Assert.Equal(count - 1, card.Owner));
            Assert.Single(game.State.Cards, card => card.FaceId == "12025");
            Assert.Single(game.State.Cards, card => card.FaceId == "12026");
            Assert.Single(game.State.Cards, card => card.FaceId == "12027");
            Assert.Equal(2, game.State.Cards.Count(card => card.FaceId == "12028"));
            Assert.Single(game.State.Cards, card => card.FaceId == "12029");
            Assert.All(game.State.Cards.Where(card => card.FaceId is "12025" or "12026" or "12027" or "12028" or "12029"),
                card => Assert.Equal(-1, card.Owner));
            for (int index = 0; index < count; index++)
                game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));
            Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        }
    }
    private static readonly string[] Signature =
    [
        "12002", "12003", "12003", "12004", "12005", "12005", "12006", "12006",
        "12007", "12007", "12007", "12008", "12009", "12009", "12010",
    ];
    private static readonly string[] Customization =
    [
        "12011", "12012", "12013", "12014", "12015", "12015", "12015",
        "12016", "12016", "12016", "12017", "12017", "12017", "12018", "12018", "12018",
        "12019", "12019", "12020", "12021", "12022", "12023", "12024", "12024", "12024",
    ];
}
