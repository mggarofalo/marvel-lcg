using Marvel.Transport;
using Marvel.Protocol;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

/// <summary>Transport scheduling choices, with synthetic endpoint gates rather than gameplay timing assertions.</summary>
public sealed class QueuedInProcessTransportTests : TransportTestBase
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task CallerReturnsWhileHostWorksAndEveryRequestUsesOneOrderedOwner()
    {
        using var endpoint = new GatedEndpoint();
        await using var transport = new QueuedInProcessTransport(endpoint);
        int caller = Environment.CurrentManagedThreadId;
        Task<EngineResponse> first = transport.ExchangeAsync(Request("first"), TestContext.Current.CancellationToken).AsTask();
        try
        {
            await endpoint.Started.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Assert.False(first.IsCompleted);
            Assert.NotEqual(caller, endpoint.OwnerThread);
            Task<EngineResponse> second = transport.ExchangeAsync(Request("second"), TestContext.Current.CancellationToken).AsTask();
            Task<EngineResponse> third = transport.ExchangeAsync(Request("third"), TestContext.Current.CancellationToken).AsTask();
            Assert.False(second.IsCompleted);
            Assert.False(third.IsCompleted);
            endpoint.Release.Set();
            EngineResponse[] responses = await Task.WhenAll(first, second, third).WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Assert.Equal(["first", "second", "third"], responses.Select(response => response.RequestId));
            Assert.Equal(["first", "second", "third"], endpoint.Calls);
            Assert.Equal(1, endpoint.MaximumConcurrent);
            Assert.Single(endpoint.Threads.Distinct());
        }
        finally { endpoint.Release.Set(); }
    }

    [Fact]
    public async Task QueuedCancellationImmediatelyProvesNonDeliveryAndLeavesLaterRequestsOrdered()
    {
        using var endpoint = new GatedEndpoint();
        await using var transport = new QueuedInProcessTransport(endpoint);
        Task<EngineResponse> first = transport.ExchangeAsync(Request("first"), TestContext.Current.CancellationToken).AsTask();
        try
        {
            await endpoint.Started.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            using var cancellation = new CancellationTokenSource();
            Task<EngineResponse> cancelled = transport.ExchangeAsync(Request("cancelled"), cancellation.Token).AsTask();
            Task<EngineResponse> last = transport.ExchangeAsync(Request("last"), TestContext.Current.CancellationToken).AsTask();
            cancellation.Cancel();
            EngineTransportException failure = await Assert.ThrowsAsync<EngineTransportException>(
                () => cancelled.WaitAsync(Timeout, TestContext.Current.CancellationToken));
            Assert.False(failure.RequestMayHaveCommitted);
            Assert.IsType<OperationCanceledException>(failure.InnerException);
            Assert.False(first.IsCompleted);
            endpoint.Release.Set();
            await Task.WhenAll(first, last).WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Assert.Equal(["first", "last"], endpoint.Calls);
        }
        finally { endpoint.Release.Set(); }
    }

    [Fact]
    public async Task CancellationBeforeAdmissionNeverReachesTheEndpoint()
    {
        using var endpoint = new GatedEndpoint();
        await using var transport = new QueuedInProcessTransport(endpoint);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await transport.ExchangeAsync(Request("cancelled"), cancellation.Token));
        Assert.Empty(endpoint.Calls);
    }

    [Fact]
    public async Task DispatchedCancellationConsumesTheAuthoritativeResultExactlyOnce()
    {
        using var endpoint = new GatedEndpoint();
        await using var transport = new QueuedInProcessTransport(endpoint);
        using var cancellation = new CancellationTokenSource();
        Task<EngineResponse> first = transport.ExchangeAsync(Request("first"), cancellation.Token).AsTask();
        try
        {
            await endpoint.Started.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            cancellation.Cancel();
            Assert.False(first.IsCompleted);
            endpoint.Release.Set();
            Assert.Equal("first", (await first.WaitAsync(Timeout, TestContext.Current.CancellationToken)).RequestId);
            Assert.Equal(["first"], endpoint.Calls);
        }
        finally { endpoint.Release.Set(); }
    }

    [Fact]
    public async Task EndpointFailureRemainsUncertainAndDoesNotPreventFollowingSynchronization()
    {
        using var endpoint = new GatedEndpoint(failFirst: true);
        await using var transport = new QueuedInProcessTransport(endpoint);
        Task<EngineResponse> first = transport.ExchangeAsync(Request("first"), TestContext.Current.CancellationToken).AsTask();
        try
        {
            await endpoint.Started.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Task<EngineResponse> sync = transport.ExchangeAsync(EngineRequest.SyncGame("sync", "game", "authority"), TestContext.Current.CancellationToken).AsTask();
            endpoint.Release.Set();
            EngineTransportException failure = await Assert.ThrowsAsync<EngineTransportException>(() => first.WaitAsync(Timeout, TestContext.Current.CancellationToken));
            Assert.True(failure.RequestMayHaveCommitted);
            Assert.IsType<IOException>(failure.InnerException);
            Assert.Equal("sync", (await sync.WaitAsync(Timeout, TestContext.Current.CancellationToken)).RequestId);
            Assert.Equal(["first", "sync"], endpoint.Calls);
            Assert.Single(endpoint.Threads.Distinct());
        }
        finally { endpoint.Release.Set(); }
    }

    [Fact]
    public async Task ShutdownReturnsToCallerAndDrainsAcceptedRequestsBeforeCompleting()
    {
        using var endpoint = new GatedEndpoint();
        var transport = new QueuedInProcessTransport(endpoint);
        Task<EngineResponse> first = transport.ExchangeAsync(Request("first"), TestContext.Current.CancellationToken).AsTask();
        try
        {
            await endpoint.Started.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Task<EngineResponse> last = transport.ExchangeAsync(Request("last"), TestContext.Current.CancellationToken).AsTask();
            transport.Dispose();
            Task shutdown = transport.DisposeAsync().AsTask();
            Assert.False(shutdown.IsCompleted);
            Assert.False(transport.ShutdownCompletion.IsCompleted);
            EngineTransportException refused = await Assert.ThrowsAsync<EngineTransportException>(async () =>
                await transport.ExchangeAsync(Request("refused"), TestContext.Current.CancellationToken));
            Assert.False(refused.RequestMayHaveCommitted);
            endpoint.Release.Set();
            await Task.WhenAll(first, last, shutdown).WaitAsync(Timeout, TestContext.Current.CancellationToken);
            Assert.True(transport.ShutdownCompletion.IsCompletedSuccessfully);
            Assert.Equal(["first", "last"], endpoint.Calls);
            await transport.DisposeAsync();
        }
        finally
        {
            endpoint.Release.Set();
            await transport.DisposeAsync();
        }
    }

    [Fact]
    public async Task RepeatedCommitmentsAtOneRevisionAcceptOnceAndMatchSynchronousReplay()
    {
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var direct = new EngineHost(factory, new SequenceCapabilities("authority"));
        await using var transport = new QueuedInProcessTransport(
            new EngineHost(factory, new SequenceCapabilities("authority")));
        EngineRequest open = EngineRequest.OpenGame("open", "game",
            new GameSpecification("rhino", ["spider_man"], ModularSets: [], Seed: 7));
        EngineResponse opened = await transport.ExchangeAsync(open, TestContext.Current.CancellationToken);
        Assert.Equal(EngineJson.Write(direct.Exchange(open)), EngineJson.Write(opened));
        EngineRequest resolve = EngineRequest.ResolveGame("commit", "game", opened.Capability!, TakeOnly(opened), opened.Revision);
        Task<EngineResponse> accepted = transport.ExchangeAsync(resolve, TestContext.Current.CancellationToken).AsTask();
        Task<EngineResponse> repeated = transport.ExchangeAsync(resolve with { RequestId = "repeat" }, TestContext.Current.CancellationToken).AsTask();
        EngineResponse committed = await accepted;
        Assert.Null(committed.Error);
        Assert.Equal(EngineJson.Write(direct.Exchange(resolve)), EngineJson.Write(committed));
        Assert.Equal("stale_decision", (await repeated).Error?.Code);
        EngineRequest sync = EngineRequest.SyncGame("sync", "game", opened.Capability!);
        EngineResponse current = await transport.ExchangeAsync(sync, TestContext.Current.CancellationToken);
        Assert.Equal(opened.Revision + 1, current.Revision);
        Assert.Equal(EngineJson.Write(direct.Exchange(sync)), EngineJson.Write(current));
    }

    private static EngineRequest Request(string id) => EngineRequest.CloseGame(id, "game", "authority");

    private sealed class GatedEndpoint(bool failFirst = false) : IEngineEndpoint, IDisposable
    {
        internal TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal ManualResetEventSlim Release { get; } = new();
        internal List<string> Calls { get; } = [];
        internal List<int> Threads { get; } = [];
        internal int OwnerThread { get; private set; }
        internal int MaximumConcurrent { get; private set; }
        private int concurrent;

        public EngineResponse Exchange(EngineRequest request)
        {
            int active = Interlocked.Increment(ref concurrent);
            MaximumConcurrent = Math.Max(MaximumConcurrent, active);
            try
            {
                Calls.Add(request.RequestId);
                Threads.Add(Environment.CurrentManagedThreadId);
                if (request.RequestId == "first")
                {
                    OwnerThread = Environment.CurrentManagedThreadId;
                    Started.SetResult();
                    Assert.True(Release.Wait(Timeout, TestContext.Current.CancellationToken), "The test must release the endpoint without blocking the caller.");
                    if (failFirst) throw new IOException("synthetic publication failure");
                }
                return new EngineResponse(EngineProtocol.Version, request.RequestId, request.GameId,
                    Capability: null, Prompt: null, Events: []);
            }
            finally { Interlocked.Decrement(ref concurrent); }
        }

        public void Dispose() => Release.Dispose();
    }
}
