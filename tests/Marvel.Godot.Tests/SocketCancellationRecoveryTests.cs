using System.Net;
using System.Net.Sockets;
using Marvel.Client;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Server;
using Marvel.Tests;
using Marvel.Transport;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class SocketCancellationRecoveryTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CancelledCommittedResponseRecoversWithoutRepeatingTheOperation(bool opening)
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        using var stopping = new CancellationTokenSource();
        using var caller = new CancellationTokenSource();
        var committed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var requests = new List<EngineRequest>();
        Task serving = ServeAsync(listener, host, opening ? EngineProtocol.Open : EngineProtocol.Resolve,
            committed, requests, stopping.Token);
        using var client = new LocalGameClient(new SocketTransport(IPAddress.Loopback.ToString(),
            ((IPEndPoint)listener.LocalEndpoint).Port));
        try
        {
            if (opening)
            {
                Task<ClientEntryResult> entry = client.OpenSessionAsync("table",
                    new GameSpecification("rhino", ["spider_man"], [], 91), caller.Token).AsTask();
                await committed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                await caller.CancelAsync();
                ClientEntryResult uncertain = await entry.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                ClientEntryAttempt attempt = Assert.IsType<ClientEntryAttempt>(uncertain.PendingEntry);
                ClientEntryResult recovered = await client.RetryEntryAsync(attempt, TestContext.Current.CancellationToken);
                Assert.True(recovered.Succeeded, recovered.Error?.Message);
                Assert.Equal(requests[0].Entry, requests[1].Entry);
                Assert.Equal(0, recovered.Response!.Revision);
            }
            else
            {
                ClientEntryResult entry = await client.OpenSessionAsync("table",
                    new GameSpecification("rhino", ["spider_man"], [], 91), caller.Token);
                Assert.True(entry.Succeeded, entry.Error?.Message);
                var answer = new Decision(Assert.Single(entry.Response!.Prompt!.Affordances).Id, []);
                Task<ClientResolutionResult> resolving = client.ResolveAsync(entry.Session!, answer, caller.Token).AsTask();
                await committed.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                await caller.CancelAsync();
                ClientResolutionResult recovered = await resolving.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                Assert.True(recovered.HasAuthoritativeView);
                Assert.Equal(ClientMutationDisposition.Uncertain, recovered.MutationDisposition);
                Assert.Equal(1, recovered.Response!.Revision);
                Assert.Equal([EngineProtocol.Open, EngineProtocol.Resolve, EngineProtocol.Sync],
                    requests.Select(request => request.Operation));
            }
        }
        finally
        {
            await stopping.CancelAsync();
            await serving.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task ExchangeDeadlineBoundsAnUnresponsivePeerWithoutCallerCancellation()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        using var stopping = new CancellationTokenSource();
        var transport = new SocketTransport(IPAddress.Loopback.ToString(), ((IPEndPoint)listener.LocalEndpoint).Port)
        {
            ExchangeTimeout = TimeSpan.FromSeconds(1),
        };
        Task<EngineResponse> exchange = transport.ExchangeAsync(EngineRequest.ReadSetup("timeout"), TestContext.Current.CancellationToken).AsTask();
        using TcpClient peer = await listener.AcceptTcpClientAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(await SocketFrame.ReadAsync(peer.GetStream(), TestContext.Current.CancellationToken));
        EngineTransportException failure = await Assert.ThrowsAsync<EngineTransportException>(
            () => exchange.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.True(failure.RequestMayHaveCommitted);
        Assert.IsAssignableFrom<OperationCanceledException>(failure.InnerException);
    }

    private static async Task ServeAsync(TcpListener listener, EngineHost host, string stalledOperation,
        TaskCompletionSource committed, List<EngineRequest> requests, CancellationToken stopping)
    {
        bool stalled = false;
        try
        {
            while (!stopping.IsCancellationRequested)
            {
                using TcpClient peer = await listener.AcceptTcpClientAsync(stopping);
                NetworkStream stream = peer.GetStream();
                byte[] payload = (await SocketFrame.ReadAsync(stream, stopping))!;
                EngineRequest request = EngineJson.ReadRequest(payload);
                requests.Add(request);
                EngineResponse response = host.Exchange(request);
                Assert.Null(response.Error);
                if (!stalled && request.Operation == stalledOperation)
                {
                    stalled = true;
                    committed.SetResult();
                    Assert.Equal(0, await stream.ReadAsync(new byte[1], stopping));
                }
                else await SocketFrame.WriteAsync(stream, EngineJson.Write(response), stopping);
            }
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
    }
}
