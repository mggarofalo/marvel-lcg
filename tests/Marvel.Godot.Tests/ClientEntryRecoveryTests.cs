using Marvel.Client;
using Marvel.Protocol;
using Marvel.Server;
using Marvel.Tests;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class ClientEntryRecoveryTests
{
    [Theory]
    [InlineData("lost")]
    [InlineData("uncertain")]
    [InlineData("mismatched")]
    public async Task LostOpenResponseRetainsAnExactRetryAttemptInsteadOfCreatingAnotherGame(string failure)
    {
        var store = new MemorySessionStore();
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root), store: store);
        var transport = new LoseFirstReply(host, failure);
        using var client = new LocalGameClient(transport);
        ClientEntryResult lost = await client.OpenSessionAsync("recovery",
            new GameSpecification("rhino", ["spider_man"], [], 91), TestContext.Current.CancellationToken);
        Assert.False(lost.Succeeded);
        ClientEntryAttempt attempt = Assert.IsType<ClientEntryAttempt>(lost.PendingEntry);
        ClientEntryResult recovered = await client.RetryEntryAsync(attempt, TestContext.Current.CancellationToken);
        Assert.True(recovered.Succeeded, recovered.Error?.Message);
        Assert.Null(recovered.PendingEntry);
        Assert.Single(store.Load());
        Assert.Equal(transport.FirstClaim, transport.LastClaim);
        Assert.Null(recovered.Response!.Capability);
        Assert.NotNull(recovered.Session!.Capability);
    }

    [Fact]
    public async Task EntryLifecycleRetainsTheEmbeddedHostUntilRecoveryCompletes()
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        var transport = new LoseFirstReply(host);
        using var client = new LocalGameClient(transport);
        var lifecycle = new ClientGameLifecycle();
        long first = lifecycle.BeginEntry();
        ClientEntryResult lost = await client.OpenSessionAsync("lifetime",
            new GameSpecification("rhino", ["spider_man"], [], 91), TestContext.Current.CancellationToken);
        Assert.True(lifecycle.RetainEntry(first, client, lost));
        Assert.True(lifecycle.FinishEntry(first));
        long retry = lifecycle.BeginEntry();
        Assert.Same(client, lifecycle.EntryRecoveryClient);
        Assert.False(transport.Disposed);
        ClientEntryResult recovered = await client.RetryEntryAsync(lifecycle.EntryRecovery!, TestContext.Current.CancellationToken);
        Assert.True(lifecycle.Enter(retry, client, recovered));
        Assert.Null(lifecycle.EntryRecovery);
        Assert.NotNull(lifecycle.CurrentGame);
        lifecycle.Detach();
        Assert.True(transport.Disposed);
    }

    private sealed class LoseFirstReply(IEngineEndpoint host, string failure = "lost") : IEngineTransport, IDisposable
    {
        public bool Disposed { get; private set; }
        public void Dispose() => Disposed = true;
        public EntryClaim? FirstClaim { get; private set; }
        public EntryClaim? LastClaim { get; private set; }
        public ValueTask<EngineResponse> ExchangeAsync(EngineRequest request, CancellationToken cancellationToken = default)
        {
            EngineResponse response = host.Exchange(request);
            LastClaim = request.Entry;
            if (FirstClaim is null)
            {
                FirstClaim = request.Entry;
                if (failure == "uncertain") return ValueTask.FromResult(response with
                {
                    Error = new EngineError("write_failed", "response could not be encoded", EngineFailureDisposition.CommitUncertain),
                });
                if (failure == "mismatched") return ValueTask.FromResult(response with
                {
                    RequestId = "another-request", Error = new EngineError("denied", "unrelated request failed"),
                });
                return ValueTask.FromException<EngineResponse>(new IOException("committed response lost"));
            }
            return ValueTask.FromResult(response);
        }
    }
}
