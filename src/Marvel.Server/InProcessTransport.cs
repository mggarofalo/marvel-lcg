namespace Marvel.Server;

/// <summary>The bundled transport: the protocol contract without a socket.</summary>
public sealed class InProcessTransport(IEngineEndpoint endpoint) : IEngineTransport
{
    private readonly IEngineEndpoint endpoint =
        endpoint ?? throw new ArgumentNullException(nameof(endpoint));

    /// <inheritdoc />
    public ValueTask<EngineResponse> ExchangeAsync(
        EngineRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        bool dispatched = false;
        try
        {
            var admitted = EngineJson.Snapshot(request);
            cancellationToken.ThrowIfCancellationRequested();
            dispatched = true;
            return ValueTask.FromResult(EngineJson.Snapshot(endpoint.Exchange(admitted)));
        }
        catch (OperationCanceledException) when (!dispatched && cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception failure)
        {
            throw new EngineTransportException(dispatched, failure);
        }
    }
}
