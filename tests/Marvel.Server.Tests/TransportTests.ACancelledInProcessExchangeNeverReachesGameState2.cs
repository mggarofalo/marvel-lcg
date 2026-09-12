using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;
public sealed class TransportACancelledInProcessExchangeNeverReachesGameStateTests : TransportTestBase
{
    [Fact]
    public async Task ACancelledInProcessExchangeNeverReachesGameState()
    {
        var endpoint = new CountingEndpoint();
        var transport = new InProcessTransport(endpoint);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        await Assert.ThrowsAsync<OperationCanceledException>(async () => await transport.ExchangeAsync(EngineRequest.CloseGame("cancelled", "game", "capability"), cancelled.Token));
        Assert.Equal(0, endpoint.Calls);
    }

    [Fact]
    public async Task CallerCancellationStopsAtTheCompletedRequestWrite()
    {
        var request = EngineRequest.CloseGame("commit", "game", "capability");
        var response = new EngineResponse(EngineProtocol.Version, request.RequestId, request.GameId, Capability: null, Prompt: null, Events: [], Error: new EngineError("session_not_found", "missing"));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        Task serving = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            Assert.NotNull(SocketFrame.Read(stream));
            SocketFrame.Write(stream, EngineJson.Write(response));
        }, TestContext.Current.CancellationToken);
        using var cancelled = new CancellationTokenSource();
        var socket = new SocketTransport(IPAddress.Loopback.ToString(), port, cancelled.Cancel);
        Task<EngineResponse> exchange = socket.ExchangeAsync(request, cancelled.Token).AsTask();
        Assert.Equal(EngineJson.Write(response), EngineJson.Write(await exchange));
        await serving;
    }

    [Fact]
    public async Task CallerCancellationBeforeCommitRemainsOperationCanceledException()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        var transport = new SocketTransport(IPAddress.Loopback.ToString(), 1);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await transport.ExchangeAsync(EngineRequest.CloseGame("cancelled", "game", "capability"), cancelled.Token));
    }

    [Fact]
    public async Task CallerCancellationAfterTransmissionBeginsIsReportedAsUncertain()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var cancelled = new CancellationTokenSource();
        var transport = new SocketTransport(IPAddress.Loopback.ToString(), port, cancelled.Cancel, () =>
        {
        });
        Task accepting = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
        }, TestContext.Current.CancellationToken);
        EngineTransportException failure = await Assert.ThrowsAsync<EngineTransportException>(async () => await transport.ExchangeAsync(EngineRequest.CloseGame("cancelled", "game", "capability"), cancelled.Token));
        await accepting;
        Assert.True(failure.RequestMayHaveCommitted);
        Assert.IsAssignableFrom<OperationCanceledException>(failure.InnerException);
    }

    [Fact]
    public async Task OversizedRequestIsRejectedBeforeTransmissionBegins()
    {
        string oversizedGameId = new('g', SocketFrame.MaximumPayload);
        var transport = new SocketTransport(IPAddress.Loopback.ToString(), 1);
        EngineTransportException failure = await Assert.ThrowsAsync<EngineTransportException>(async () => await transport.ExchangeAsync(EngineRequest.CloseGame("oversized", oversizedGameId, "capability"), TestContext.Current.CancellationToken));
        Assert.False(failure.RequestMayHaveCommitted);
        Assert.IsType<InvalidDataException>(failure.InnerException);
    }

    [Fact]
    public async Task CommitStateIsSetBeforeTheCompletedRequestHook()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var request = EngineRequest.CloseGame("hook", "game", "capability");
        Task serving = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            Assert.Equal(EngineJson.Write(request), SocketFrame.Read(stream));
        }, TestContext.Current.CancellationToken);
        var transport = new SocketTransport(IPAddress.Loopback.ToString(), port, () => throw new IOException("test hook failed"));
        EngineTransportException failure;
        try
        {
            failure = await Assert.ThrowsAsync<EngineTransportException>(async () => await transport.ExchangeAsync(request, TestContext.Current.CancellationToken));
        }
        finally
        {
            await serving;
        }

        Assert.True(failure.RequestMayHaveCommitted);
    }

    [Fact]
    public async Task RefusedConnectionIsReportedBeforeRequestCommitWithoutDiagnostics()
    {
        int port;
        using (var unavailable = new TcpListener(IPAddress.Loopback, 0))
        {
            unavailable.Start();
            port = ((IPEndPoint)unavailable.LocalEndpoint).Port;
            unavailable.Stop();
        }

        const string secret = "capability-that-must-not-enter-the-message";
        var transport = new SocketTransport(IPAddress.Loopback.ToString(), port);
        EngineTransportException failure = await Assert.ThrowsAsync<EngineTransportException>(async () => await transport.ExchangeAsync(EngineRequest.CloseGame("refused", "game", secret), TestContext.Current.CancellationToken));
        Assert.False(failure.RequestMayHaveCommitted);
        Assert.IsType<SocketException>(failure.InnerException);
        Assert.Equal("the engine transport exchange failed", failure.Message);
        Assert.DoesNotContain(secret, failure.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task ClosedResponseConnectionIsReportedAfterRequestCommitWithoutDiagnostics()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        const string secret = "capability-that-must-not-enter-the-message";
        var request = EngineRequest.CloseGame("closed", "game", secret);
        Task serving = Task.Run(() =>
        {
            using TcpClient accepted = listener.AcceptTcpClient();
            using NetworkStream stream = accepted.GetStream();
            Assert.Equal(EngineJson.Write(request), SocketFrame.Read(stream));
        }, TestContext.Current.CancellationToken);
        var transport = new SocketTransport(IPAddress.Loopback.ToString(), port);
        EngineTransportException failure;
        try
        {
            failure = await Assert.ThrowsAsync<EngineTransportException>(async () => await transport.ExchangeAsync(request, TestContext.Current.CancellationToken));
        }
        finally
        {
            await serving;
        }

        Assert.True(failure.RequestMayHaveCommitted);
        Assert.IsType<EndOfStreamException>(failure.InnerException);
        Assert.Equal("the engine transport exchange failed", failure.Message);
        Assert.DoesNotContain(secret, failure.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task LargeClientIdsAreBoundedAndTheNextConnectionStillWorks()
    {
        var server = new SocketEngineServer(new EngineHost(new UnusedFactory()), IPAddress.Loopback, port: 0);
        string largeGameId = new('g', 2_200_000);
        var rejected = await ExchangeOverSocket(server, EngineRequest.CloseGame("large", largeGameId, "capability"));
        var next = await ExchangeOverSocket(server, EngineRequest.CloseGame("next", "missing", "capability"));
        Assert.Equal("invalid_request", rejected.Error?.Code);
        Assert.Equal(EngineProtocol.MaximumIdentifierLength, rejected.GameId.Length);
        Assert.True(EngineJson.Write(rejected).Length < SocketFrame.MaximumPayload);
        Assert.Equal("session_not_found", next.Error?.Code);
    }

    [Fact]
    public async Task AnUnrepresentableResponseIsContainedToItsConnection()
    {
        var normal = new EngineResponse(EngineProtocol.Version, "next", "game", Capability: null, Prompt: null, Events: []);
        var endpoint = new SequenceEndpoint(new EngineResponse(EngineProtocol.Version, "large", new string ('g', SocketFrame.MaximumPayload), Capability: null, Prompt: null, Events: []), normal);
        var server = new SocketEngineServer(endpoint, IPAddress.Loopback, port: 0);
        var failed = await ExchangeOverSocket(server, EngineRequest.CloseGame("first", "game", "capability"));
        var next = await ExchangeOverSocket(server, EngineRequest.CloseGame("second", "game", "capability"));
        Assert.Equal("response_failed", failed.Error?.Code);
        Assert.Equal(EngineJson.Write(normal), EngineJson.Write(next));
    }

    [Fact]
    public async Task SeparateConnectionsCannotResolveOrCloseEachOthersSession()
    {
        var host = new EngineHost(DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root), new SequenceCapabilities("capability-a", "capability-b"));
        var server = new SocketEngineServer(host, IPAddress.Loopback, port: 0);
        var specification = new GameSpecification("rhino", ["spider_man"], ModularSets: [], Seed: 7);
        var first = await ExchangeOverSocket(server, EngineRequest.OpenGame("first", "same-id", specification));
        var second = await ExchangeOverSocket(server, EngineRequest.OpenGame("second", "same-id", specification));
        var guessed = await ExchangeOverSocket(server, EngineRequest.CloseGame("guess", "same-id", "not-the-capability"));
        var closeSecond = await ExchangeOverSocket(server, EngineRequest.CloseGame("close-second", "same-id", second.Capability!));
        var resolveFirst = await ExchangeOverSocket(server, EngineRequest.ResolveGame("resolve-first", "same-id", first.Capability!, TakeOnly(first)));
        Assert.NotEqual(first.Capability, second.Capability);
        Assert.Equal("session_not_found", guessed.Error?.Code);
        Assert.Null(closeSecond.Error);
        Assert.Null(resolveFirst.Error);
    }
}
