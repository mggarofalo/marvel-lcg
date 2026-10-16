using Marvel.Protocol;
namespace Marvel.Transport;

/// <summary>Arbitrates cancellation against the dispatch boundary of one embedded request.</summary>
internal sealed class QueuedEngineExchange
{
    private readonly EngineRequest request;
    private readonly CancellationToken cancellationToken;
    private readonly CancellationTokenRegistration cancellation;
    private readonly TaskCompletionSource<EngineResponse> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int dispatchedOrCancelled;

    internal QueuedEngineExchange(EngineRequest request, CancellationToken cancellationToken)
    {
        // Capture before queue admission, while the caller still owns the input.
        this.request = EngineJson.Snapshot(request);
        this.cancellationToken = cancellationToken;
        cancellation = cancellationToken.Register(Cancel);
    }

    internal Task<EngineResponse> Response => completion.Task;

    private void Cancel()
    {
        // Dispatch and cancellation compete for one boundary. A cancellation receipt
        // proves non-delivery; it cannot roll back an endpoint that already started.
        if (Interlocked.CompareExchange(ref dispatchedOrCancelled, 1, 0) == 0)
            completion.SetException(new EngineTransportException(requestMayHaveCommitted: false,
                new OperationCanceledException(cancellationToken)));
    }

    internal void Execute(IEngineEndpoint endpoint)
    {
        try
        {
            if (Interlocked.CompareExchange(ref dispatchedOrCancelled, 1, 0) != 0) return;
            completion.SetResult(EngineJson.Snapshot(endpoint.Exchange(request)));
        }
        catch (Exception failure)
        {
            // An endpoint failure cannot certify whether publication occurred. Keep
            // that uncertainty and allow a subsequent authoritative sync on this owner.
            completion.SetException(new EngineTransportException(requestMayHaveCommitted: true, failure));
        }
        finally { cancellation.Dispose(); }
    }
}
