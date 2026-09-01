using System.Net;
using System.Net.Sockets;
using Marvel.Rules.Play;
using Marvel.Server;
using Marvel.Tests;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class LocalGameClientTests
{
    [Fact]
    public async Task CommittedCoreContentOpensACompleteVisibleGame()
    {
        ClientStartupResult startup = await LocalGameClient.OpenLocalAsync(
            RepositoryPaths.Root,
            TestContext.Current.CancellationToken);

        EngineResponse opened = Assert.IsType<EngineResponse>(startup.Response);
        Assert.True(startup.Succeeded);
        Assert.Null(startup.Error);
        Assert.NotNull(opened.World);
        Assert.NotNull(opened.Prompt);
        Assert.NotNull(opened.Events);
        Assert.Equal(Outcome.Unfinished, opened.World.Outcome);
        Assert.Equal(DevelopmentGame.GameId, opened.GameId);
    }

    [Fact]
    public async Task AppBootstrapUsesTheSameRequestOverLocalAndRemoteTransports()
    {
        var localHost = Host();
        ClientStartupResult local = await new LocalGameClient(
            new InProcessTransport(localHost)).OpenAsync(TestContext.Current.CancellationToken);

        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var remoteHost = Host();
        var server = new SocketEngineServer(remoteHost, IPAddress.Loopback, port: 0);
        Task serving = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            server.Serve(accepted);
        }, TestContext.Current.CancellationToken);

        ClientStartupResult remote;
        try
        {
            remote = await new LocalGameClient(
                new SocketTransport(IPAddress.Loopback.ToString(), port))
                .OpenAsync(TestContext.Current.CancellationToken);
        }
        finally
        {
            await serving;
        }

        Assert.True(local.Succeeded);
        Assert.True(remote.Succeeded);
        Assert.Equal(EngineJson.Write(local.Response!), EngineJson.Write(remote.Response!));
    }

    [Fact]
    public async Task MissingContentBecomesABoundedProductError()
    {
        string missing = Path.Combine(
            Path.GetTempPath(), "marvel-missing-content", Guid.NewGuid().ToString("N"));

        ClientStartupResult startup = await LocalGameClient.OpenLocalAsync(
            missing,
            TestContext.Current.CancellationToken);

        Assert.False(startup.Succeeded);
        Assert.Null(startup.Response);
        Assert.Equal("content_unavailable", startup.Error?.Code);
        Assert.DoesNotContain(missing, startup.Error?.Message, StringComparison.Ordinal);
        Assert.True(startup.Error?.Message.Length <= 240);
    }

    [Fact]
    public async Task EngineRejectionsAreBoundedBeforeDisplay()
    {
        string diagnostic = new('x', 400);
        var response = new EngineResponse(
            EngineProtocol.Version,
            "local-open",
            DevelopmentGame.GameId,
            Capability: null,
            Prompt: null,
            Events: [],
            Error: new EngineError(diagnostic, diagnostic));

        ClientStartupResult startup = await new LocalGameClient(
            new FixedTransport(response)).OpenAsync(TestContext.Current.CancellationToken);

        Assert.False(startup.Succeeded);
        Assert.Equal(240, startup.Error?.Code.Length);
        Assert.Equal(240, startup.Error?.Message.Length);
    }

    [Fact]
    public async Task IncompleteSuccessfulResponsesAreRejected()
    {
        var response = new EngineResponse(
            EngineProtocol.Version,
            "local-open",
            DevelopmentGame.GameId,
            "capability",
            Prompt: null,
            Events: []);

        ClientStartupResult startup = await new LocalGameClient(
            new FixedTransport(response)).OpenAsync(TestContext.Current.CancellationToken);

        Assert.Equal("invalid_response", startup.Error?.Code);
    }

    [Fact]
    public async Task TransportDiagnosticsDoNotEscapeToTheProduct()
    {
        ClientStartupResult startup = await new LocalGameClient(
            new FailingTransport("secret socket diagnostic"))
            .OpenAsync(TestContext.Current.CancellationToken);

        Assert.Equal("transport_unavailable", startup.Error?.Code);
        Assert.DoesNotContain(
            "secret socket diagnostic", startup.Error?.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CallerCancellationIsNotPresentedAsAStartupFailure()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await new LocalGameClient(new InProcessTransport(Host()))
                .OpenAsync(cancelled.Token));
    }

    private static EngineHost Host() =>
        new(
            DatasetGameFactory.Load(RepositoryPaths.Root),
            new FixedCapabilityIssuer());

    private sealed class FixedCapabilityIssuer : ISessionCapabilityIssuer
    {
        public string Issue() => "development-capability";
    }

    private sealed class FixedTransport(EngineResponse response) : IEngineTransport
    {
        public ValueTask<EngineResponse> ExchangeAsync(
            EngineRequest request,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(response);
    }

    private sealed class FailingTransport(string message) : IEngineTransport
    {
        public ValueTask<EngineResponse> ExchangeAsync(
            EngineRequest request,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromException<EngineResponse>(new IOException(message));
    }
}
