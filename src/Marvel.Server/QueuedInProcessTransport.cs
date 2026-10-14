namespace Marvel.Server;

/// <summary>Runs embedded protocol requests on one dedicated, ordered host owner.</summary>
/// <remarks>
/// The caller transfers exclusive exchange ownership of the endpoint to this transport.
/// Queue order and shutdown draining are our transport choices, not game rules.
/// Gameplay remains synchronous: no two exchanges run concurrently, and every exchange
/// finishes replay, persistence and publication before the next begins.
/// </remarks>
public sealed class QueuedInProcessTransport : IEngineTransport, IDisposable, IAsyncDisposable
{
    private readonly object gate = new();
    private readonly Queue<QueuedEngineExchange> pending = new();
    private readonly IEngineEndpoint endpoint;
    private readonly TaskCompletionSource stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private bool closing;

    /// <summary>Starts the sole worker for an endpoint that no other caller may exchange with.</summary>
    public QueuedInProcessTransport(IEngineEndpoint endpoint)
    {
        this.endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
        var owner = new Thread(Serve) { IsBackground = true, Name = "Marvel embedded host" };
        owner.Start();
    }

    /// <summary>Completes when shutdown has drained all admitted work and the owner has exited.</summary>
    public Task ShutdownCompletion => stopped.Task;

    /// <summary>
    /// Enqueues without executing the host on the caller. Cancellation prevents queued
    /// dispatch; after dispatch, the authoritative response is consumed despite cancellation.
    /// </summary>
    public ValueTask<EngineResponse> ExchangeAsync(
        EngineRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        lock (gate)
        {
            if (closing)
                return ValueTask.FromException<EngineResponse>(new EngineTransportException(
                    requestMayHaveCommitted: false, new ObjectDisposedException(nameof(QueuedInProcessTransport))));
            var exchange = new QueuedEngineExchange(request, cancellationToken);
            pending.Enqueue(exchange);
            Monitor.Pulse(gate);
            return new(exchange.Response);
        }
    }

    /// <summary>Stops admission immediately and lets all accepted exchanges finish without blocking the caller.</summary>
    public void Dispose()
    {
        lock (gate)
        {
            closing = true;
            Monitor.Pulse(gate);
        }
    }

    /// <summary>Stops admission and asynchronously awaits the complete accepted queue and worker shutdown.</summary>
    public ValueTask DisposeAsync()
    {
        Dispose();
        return new(stopped.Task);
    }

    private void Serve()
    {
        while (true)
        {
            QueuedEngineExchange exchange;
            lock (gate)
            {
                while (pending.Count == 0 && !closing) Monitor.Wait(gate);
                if (pending.Count == 0)
                {
                    stopped.SetResult();
                    return;
                }
                exchange = pending.Dequeue();
            }
            exchange.Execute(endpoint);
        }
    }
}
