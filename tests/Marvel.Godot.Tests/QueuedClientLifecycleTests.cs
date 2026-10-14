using System.Collections.Concurrent;
using Marvel.Decisions;
using Marvel.Server;
using Xunit;

namespace Marvel.Godot.Tests;

/// <summary>Real Core host and client lifecycle on the queued boundary; these are managed, not native frame checks.</summary>
public sealed class QueuedClientLifecycleTests : LocalGameClientTestBase
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task QueuedCancellationRetainsDraftAndAllowsOneExplicitCommitment()
    {
        using var endpoint = new BlockingEndpoint(Host());
        await using var transport = new QueuedInProcessTransport(endpoint);
        using var client = new LocalGameClient(transport);
        ClientGameLifecycle lifecycle = await Enter(client);
        EngineResponse original = lifecycle.CurrentGame!;
        Task<EngineResponse> hold = transport.ExchangeAsync(EngineRequest.ReadSetup("hold"),
            TestContext.Current.CancellationToken).AsTask();
        try
        {
            await endpoint.Started.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            using var cancellation = new CancellationTokenSource();
            Task<ClientLifecycleUpdate?> pending = lifecycle.ResolveAsync(original.Revision,
                VisibleDecision(original.Prompt!), cancellation.Token);
            Assert.False(pending.IsCompleted);
            cancellation.Cancel();
            ClientLifecycleUpdate notSent = Assert.IsType<ClientLifecycleUpdate>(
                await pending.WaitAsync(Timeout, TestContext.Current.CancellationToken));
            Assert.Equal(ClientMutationDisposition.NotSent, notSent.MutationDisposition);
            Assert.Equal(ClientDraftDisposition.Retry, notSent.Draft);
            Assert.Equal(GameProgressKind.DecisionNotSent, notSent.Progress.Kind);
            Assert.Same(original, lifecycle.CurrentGame);
            Assert.True(lifecycle.CanResolve);
            Assert.DoesNotContain(EngineProtocol.Resolve, endpoint.Operations);
            endpoint.Release.Set();
            await hold;
            ClientLifecycleUpdate committed = Assert.IsType<ClientLifecycleUpdate>(await lifecycle.ResolveAsync(
                original.Revision, VisibleDecision(original.Prompt!), TestContext.Current.CancellationToken));
            Assert.True(committed.MutationAccepted);
            Assert.Equal(original.Revision + 1, lifecycle.CurrentGame!.Revision);
            Assert.Single(endpoint.Operations, operation => operation == EngineProtocol.Resolve);
        }
        finally { endpoint.Release.Set(); }
    }

    [Fact]
    public async Task SendingStateAndRepeatedInputRemainSafeDuringRealHostWorkAndCancellation()
    {
        using var endpoint = new BlockingEndpoint(Host(), blockResolve: true);
        await using var transport = new QueuedInProcessTransport(endpoint);
        using var client = new LocalGameClient(transport);
        ClientGameLifecycle lifecycle = await Enter(client);
        EngineResponse original = lifecycle.CurrentGame!;
        using var cancellation = new CancellationTokenSource();
        Task<ClientLifecycleUpdate?> pending = lifecycle.ResolveAsync(original.Revision,
            VisibleDecision(original.Prompt!), cancellation.Token);
        try
        {
            await endpoint.Started.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Assert.False(pending.IsCompleted);
            Assert.Equal(GameProgressKind.Resolving, lifecycle.Progress!.Kind);
            Assert.True(lifecycle.Progress.LocksDecisions);
            Assert.Null(await lifecycle.ResolveAsync(original.Revision, VisibleDecision(original.Prompt!),
                TestContext.Current.CancellationToken));
            Assert.Null(await lifecycle.SynchronizeAsync(cancellationToken: TestContext.Current.CancellationToken));
            cancellation.Cancel();
            Assert.False(pending.IsCompleted);
            endpoint.Release.Set();
            ClientLifecycleUpdate committed = Assert.IsType<ClientLifecycleUpdate>(await pending);
            Assert.True(committed.MutationAccepted);
            Assert.Equal(original.Revision + 1, lifecycle.CurrentGame!.Revision);
            Assert.Single(endpoint.Operations, operation => operation == EngineProtocol.Resolve);
        }
        finally { endpoint.Release.Set(); }
    }

    [Fact]
    public async Task DetachedClientDrainsHostWorkAndDiscardsTheLateResult()
    {
        using var endpoint = new BlockingEndpoint(Host(), blockResolve: true);
        await using var transport = new QueuedInProcessTransport(endpoint);
        using var client = new LocalGameClient(transport);
        ClientGameLifecycle lifecycle = await Enter(client);
        Task<ClientLifecycleUpdate?> pending = lifecycle.ResolveAsync(lifecycle.CurrentGame!.Revision,
            VisibleDecision(lifecycle.CurrentGame.Prompt!), TestContext.Current.CancellationToken);
        try
        {
            await endpoint.Started.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            lifecycle.Detach();
            Task shutdown = client.DisposeAsync().AsTask();
            Assert.False(shutdown.IsCompleted);
            Assert.False(lifecycle.CanResolve);
            Assert.Null(lifecycle.CurrentGame);
            EngineTransportException refused = await Assert.ThrowsAsync<EngineTransportException>(async () =>
                await transport.ExchangeAsync(EngineRequest.ReadSetup("after-detach"), TestContext.Current.CancellationToken));
            Assert.False(refused.RequestMayHaveCommitted);
            endpoint.Release.Set();
            Assert.Null(await pending);
            await shutdown.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Assert.Null(lifecycle.CurrentGame);
            Assert.Single(endpoint.Operations, operation => operation == EngineProtocol.Resolve);
        }
        finally { endpoint.Release.Set(); }
    }

    [Fact]
    public async Task LostCommittedResponseSynchronizesOnTheOwnerWithoutRepeatingTheDecision()
    {
        using var endpoint = new BlockingEndpoint(Host(), loseResolveResponse: true);
        await using var transport = new QueuedInProcessTransport(endpoint);
        using var client = new LocalGameClient(transport);
        ClientGameLifecycle lifecycle = await Enter(client);
        EngineResponse original = lifecycle.CurrentGame!;
        ClientLifecycleUpdate recovered = Assert.IsType<ClientLifecycleUpdate>(await lifecycle.ResolveAsync(
            original.Revision, VisibleDecision(original.Prompt!), TestContext.Current.CancellationToken));
        Assert.Equal(ClientMutationDisposition.Uncertain, recovered.MutationDisposition);
        Assert.Equal(ClientDraftDisposition.Replace, recovered.Draft);
        Assert.Equal(original.Revision + 1, lifecycle.CurrentGame!.Revision);
        Assert.Empty(lifecycle.CurrentGame.Events);
        Assert.Single(endpoint.Operations, operation => operation == EngineProtocol.Resolve);
        Assert.Single(endpoint.Operations, operation => operation == EngineProtocol.Sync);
        Assert.Null(await lifecycle.ResolveAsync(original.Revision, VisibleDecision(original.Prompt!),
            TestContext.Current.CancellationToken));
    }

    private static async Task<ClientGameLifecycle> Enter(LocalGameClient client)
    {
        ClientEntryResult entry = await client.OpenSessionAsync("queued-table", Specification(),
            TestContext.Current.CancellationToken);
        var lifecycle = new ClientGameLifecycle();
        Assert.True(lifecycle.Enter(lifecycle.BeginEntry(), client, entry));
        return lifecycle;
    }

    private sealed class BlockingEndpoint(IEngineEndpoint host, bool blockResolve = false,
        bool loseResolveResponse = false) : IEngineEndpoint, IDisposable
    {
        internal TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal ManualResetEventSlim Release { get; } = new();
        internal ConcurrentQueue<string> Operations { get; } = new();

        public EngineResponse Exchange(EngineRequest request)
        {
            Operations.Enqueue(request.Operation);
            if (request.RequestId == "hold" || blockResolve && request.Operation == EngineProtocol.Resolve)
            {
                Started.TrySetResult();
                Assert.True(Release.Wait(Timeout, TestContext.Current.CancellationToken),
                    "The caller must remain free to release the synthetic host gate.");
            }
            EngineResponse response = host.Exchange(request);
            if (loseResolveResponse && request.Operation == EngineProtocol.Resolve)
                throw new IOException("synthetic response loss after publication");
            return response;
        }

        public void Dispose() => Release.Dispose();
    }
}
