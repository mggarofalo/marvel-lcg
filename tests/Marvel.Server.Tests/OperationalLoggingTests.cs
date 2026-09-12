using System.Text.Json;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;
public abstract class OperationalLoggingTestBase
{
    protected sealed class CollectingSink : IOperationalSink
    {
        private readonly ConcurrentQueue<OperationalRecord> records = new();
        public int Count => records.Count;
        public IReadOnlyList<OperationalRecord> Records => [..records];

        public void Write(OperationalRecord record) => records.Enqueue(record);
    }

    protected static void WaitForRecords(CollectingSink sink, int count) => Assert.True(SpinWait.SpinUntil(() => sink.Count >= count, TimeSpan.FromSeconds(2)));
    protected sealed class ThrowingSink : IOperationalSink
    {
        public void Write(OperationalRecord record) => throw new IOException("sink-private-detail");
    }

    protected sealed class BlockingSink : IOperationalSink, IDisposable
    {
        public ManualResetEventSlim Entered { get; } = new(false);
        public ManualResetEventSlim Release { get; } = new(false);

        public void Write(OperationalRecord record)
        {
            Entered.Set();
            Release.Wait();
        }

        public void Dispose()
        {
            Release.Set();
            Entered.Dispose();
            Release.Dispose();
        }
    }

    protected sealed class SequenceCapabilities(params string[] values) : ISessionCapabilityIssuer
    {
        private readonly Queue<string> values = new(values);
        public string Issue() => values.Dequeue();
    }

    protected sealed class ConstantEndpoint(EngineResponse response) : IEngineEndpoint
    {
        public EngineResponse Exchange(EngineRequest request) => response;
    }
}
