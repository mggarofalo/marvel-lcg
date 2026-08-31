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
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(endpoint.Exchange(request));
    }
}
