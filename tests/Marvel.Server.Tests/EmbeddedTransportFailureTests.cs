using Marvel.Transport;
using Marvel.Protocol;
using Marvel.Rules.Play;
using System.Collections;
using System.Text.Json;
using Marvel.Decisions;
using Marvel.Rules.Events;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class EmbeddedTransportFailureTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RequestSerializationFailureProvesNonDelivery(bool queued)
    {
        var endpoint = new FailingEndpoint();
        await using var owner = queued ? new QueuedInProcessTransport(endpoint) : null;
        IEngineTransport transport = owner ?? (IEngineTransport)new InProcessTransport(endpoint);
        var request = EngineRequest.ResolveGame("request", "game", "authority",
            new Decision(7, new UnserializableList<int>()));

        var failure = await Assert.ThrowsAsync<EngineTransportException>(async () =>
            await transport.ExchangeAsync(request, TestContext.Current.CancellationToken));

        Assert.False(failure.RequestMayHaveCommitted);
        Assert.IsType<JsonException>(failure.InnerException);
        Assert.Equal(0, endpoint.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResponseSerializationFailureKeepsCommitUncertaintyAndAllowsSynchronization(bool queued)
    {
        var endpoint = new FailingEndpoint();
        await using var owner = queued ? new QueuedInProcessTransport(endpoint) : null;
        IEngineTransport transport = owner ?? (IEngineTransport)new InProcessTransport(endpoint);

        var failure = await Assert.ThrowsAsync<EngineTransportException>(async () =>
            await transport.ExchangeAsync(EngineRequest.ReadSetup("unserializable"),
                TestContext.Current.CancellationToken));

        Assert.True(failure.RequestMayHaveCommitted);
        Assert.IsType<JsonException>(failure.InnerException);
        var synchronized = await transport.ExchangeAsync(EngineRequest.ReadSetup("sync"),
            TestContext.Current.CancellationToken);
        Assert.Equal("sync", synchronized.RequestId);
        Assert.Equal(2, endpoint.Calls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EndpointFailureAfterDispatchRemainsUncertain(bool queued)
    {
        var endpoint = new FailingEndpoint();
        await using var owner = queued ? new QueuedInProcessTransport(endpoint) : null;
        IEngineTransport transport = owner ?? (IEngineTransport)new InProcessTransport(endpoint);

        var failure = await Assert.ThrowsAsync<EngineTransportException>(async () =>
            await transport.ExchangeAsync(EngineRequest.ReadSetup("throw"),
                TestContext.Current.CancellationToken));

        Assert.True(failure.RequestMayHaveCommitted);
        Assert.IsType<IOException>(failure.InnerException);
        Assert.Equal(1, endpoint.Calls);
    }

    private sealed class FailingEndpoint : IEngineEndpoint
    {
        internal int Calls { get; private set; }

        public EngineResponse Exchange(EngineRequest request)
        {
            Calls++;
            if (request.RequestId == "throw") throw new IOException("synthetic endpoint failure");
            IReadOnlyList<GameEvent> events = request.RequestId == "unserializable"
                ? new UnserializableList<GameEvent>() : Array.Empty<GameEvent>();
            return new(EngineProtocol.Version, request.RequestId, request.GameId, null, null,
                events);
        }
    }

    private sealed class UnserializableList<T> : IReadOnlyList<T>
    {
        public int Count => 1;
        public T this[int index] => throw new JsonException("synthetic serialization failure");
        public IEnumerator<T> GetEnumerator() => throw new JsonException("synthetic serialization failure");
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
