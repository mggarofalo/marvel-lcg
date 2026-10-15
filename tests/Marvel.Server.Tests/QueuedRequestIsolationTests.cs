using Marvel.Decisions;
using Marvel.Rules.Play;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class QueuedRequestIsolationTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    [Fact]
    public async Task QueuedRequestsCaptureNestedValuesBeforeReturningToTheirCaller()
    {
        // Synthetic protocol request exercises every mutable request family. The
        // gated endpoint establishes that the second request has not dispatched.
        using var endpoint = new CapturingEndpoint();
        await using var transport = new QueuedInProcessTransport(endpoint);
        var first = transport.ExchangeAsync(EngineRequest.ReadSetup("first"),
            TestContext.Current.CancellationToken).AsTask();
        try
        {
            await endpoint.Started.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
            string[] heroes = ["spider_man"];
            string[] modularSets = ["bomb_scare"];
            int[] targets = [1];
            int[] resources = [2];
            int[] order = [0, 1];
            var values = new Dictionary<string, long> { ["X"] = 3 };
            ResourceAllocation[] allocations = [new(2, 0, "E")];
            var request = new EngineRequest(EngineProtocol.Version, "second", "synthetic", "game",
                Game: new GameSpecification("rhino", heroes, modularSets, 7),
                Decision: new EngineDecision(7, targets, resources, values, allocations), Order: order);
            byte[] original = EngineJson.Write(request);

            var second = transport.ExchangeAsync(request, TestContext.Current.CancellationToken).AsTask();
            Assert.False(second.IsCompleted);
            heroes[0] = "changed";
            modularSets[0] = "changed";
            targets[0] = 99;
            resources[0] = 99;
            order[0] = 99;
            values["X"] = 99;
            allocations[0] = new(99, 1, "W");
            endpoint.Release.Set();
            await Task.WhenAll(first, second).WaitAsync(Timeout, TestContext.Current.CancellationToken);

            Assert.Equal(original, endpoint.Received[1]);
        }
        finally { endpoint.Release.Set(); }
    }

    private sealed class CapturingEndpoint : IEngineEndpoint, IDisposable
    {
        internal TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal ManualResetEventSlim Release { get; } = new();
        internal List<byte[]> Received { get; } = [];

        public EngineResponse Exchange(EngineRequest request)
        {
            Received.Add(EngineJson.Write(request));
            if (request.RequestId == "first")
            {
                Started.SetResult();
                Assert.True(Release.Wait(Timeout, TestContext.Current.CancellationToken));
            }
            return new(EngineProtocol.Version, request.RequestId, request.GameId, null, null, []);
        }

        public void Dispose() => Release.Dispose();
    }
}
