using System.Net;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Tests;
using Marvel.Transport;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class KreeFanaticIndependentTransportTests : TransportTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PublicSelectionDealsAllSevenCopiesAndDoesNotChangeTheDefault(bool socket)
    {
        var factory = new ObservedFactory(DatasetGameFactory.Load(RepositoryPaths.Root));
        var host = new EngineHost(factory);
        var selected = await Exchange(host, socket, EngineRequest.OpenGame("selected", "selected",
            new GameSpecification("rhino", ["spider_man"], ["kree_fanatic"], 567)));
        Assert.Null(selected.Error);
        Assert.NotNull(selected.Capability);
        var selectedGame = Assert.IsType<Game>(factory.LastGame);
        Assert.Equal(["90001", "90002", "90003", "90004", "90004", "90005", "90005"],
            selectedGame.State.Cards.Where(card => card.FaceId.StartsWith("900", StringComparison.Ordinal))
                .Select(card => card.FaceId).Order(StringComparer.Ordinal));
        Assert.DoesNotContain(selectedGame.State.Cards, card => card.FaceId == "01109");
        var defaults = await Exchange(host, socket, EngineRequest.OpenGame("default", "default",
            new GameSpecification("rhino", ["spider_man"], null, 567)));
        Assert.Null(defaults.Error);
        var defaultGame = Assert.IsType<Game>(factory.LastGame);
        Assert.NotSame(selectedGame, defaultGame);
        Assert.DoesNotContain(defaultGame.State.Cards, card => card.FaceId.StartsWith("900", StringComparison.Ordinal));
        Assert.Contains(defaultGame.State.Cards, card => card.FaceId == "01109");
    }

    private static async Task<EngineResponse> Exchange(EngineHost host, bool socket, EngineRequest request) => socket
        ? await ExchangeOverSocket(new SocketEngineServer(host, IPAddress.Loopback, 0), request)
        : await new InProcessTransport(host).ExchangeAsync(request, TestContext.Current.CancellationToken);

    // This observer delegates unchanged to production setup and never edits the game.
    private sealed class ObservedFactory(IGameFactory inner) : IGameFactory
    {
        public Game? LastGame { get; private set; }

        public OpenedGame Create(GameSpecification specification)
        {
            var opened = inner.Create(specification);
            LastGame = opened.Game;
            return opened;
        }
    }
}
