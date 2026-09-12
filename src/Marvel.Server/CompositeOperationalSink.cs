using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Server;

/// <summary>Delivers one record independently to each configured observer.</summary>
public sealed class CompositeOperationalSink(params IOperationalSink[] sinks)
    : IOperationalSink, IOperationalFlushable
{
    private readonly IReadOnlyList<IsolatedSink> sinks =
        (sinks ?? throw new ArgumentNullException(nameof(sinks)))
        .Select(sink => new IsolatedSink(sink))
        .ToArray();

    /// <inheritdoc />
    public void Write(OperationalRecord record)
    {
        foreach (IsolatedSink sink in sinks)
        {
            sink.Enqueue(record);
        }
    }

    void IOperationalFlushable.Flush(TimeSpan timeout)
    {
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        foreach (IsolatedSink sink in sinks)
        {
            TimeSpan remaining = timeout - elapsed.Elapsed;
            if (remaining <= TimeSpan.Zero)
            {
                return;
            }

            sink.Flush(remaining);
        }
    }

    private sealed class IsolatedSink
    {
        private const int MaximumPendingRecords = 1024;
        private readonly IOperationalSink sink;
        private readonly ConcurrentQueue<OperationalRecord> pending = new();
        private readonly object signal = new();
        private int pendingCount;

        public IsolatedSink(IOperationalSink sink)
        {
            this.sink = sink ?? throw new ArgumentNullException(nameof(sink));
            var worker = new Thread(Drain)
            {
                IsBackground = true,
                Name = "Marvel operational observer",
            };
            worker.Start();
        }

        public void Enqueue(OperationalRecord record)
        {
            if (Interlocked.Increment(ref pendingCount) > MaximumPendingRecords)
            {
                Interlocked.Decrement(ref pendingCount);
                return;
            }

            pending.Enqueue(record);
            lock (signal)
            {
                Monitor.Pulse(signal);
            }
        }

        public void Flush(TimeSpan timeout)
        {
            var elapsed = System.Diagnostics.Stopwatch.StartNew();
            _ = SpinWait.SpinUntil(
                () => Volatile.Read(ref pendingCount) == 0,
                timeout);
            TimeSpan remaining = timeout - elapsed.Elapsed;
            if (Volatile.Read(ref pendingCount) == 0
                && remaining > TimeSpan.Zero
                && sink is IOperationalFlushable flushable)
            {
                flushable.Flush(remaining);
            }
        }

        private void Drain()
        {
            while (true)
            {
                lock (signal)
                {
                    while (pending.IsEmpty)
                    {
                        Monitor.Wait(signal);
                    }
                }
                while (pending.TryDequeue(out OperationalRecord? record))
                {
                    try
                    {
                        sink.Write(record);
                    }
                    catch (Exception)
                    {
                    }
                    finally
                    {
                        Interlocked.Decrement(ref pendingCount);
                    }
                }
            }
        }
    }
}
