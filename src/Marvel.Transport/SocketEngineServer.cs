using Marvel.Protocol;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Marvel.Transport;

/// <summary>Bounded concurrent socket framing around an authoritative endpoint.</summary>
public sealed class SocketEngineServer(IEngineEndpoint endpoint, IPAddress address, int port)
{
    // Host choices: limit memory held by incomplete frames and bound idle peer lifetime.
    private const int MaximumConnections = 32;
    private static readonly TimeSpan ClientTimeout = TimeSpan.FromSeconds(30);
    private readonly IEngineEndpoint endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
    private readonly IPAddress address = address ?? throw new ArgumentNullException(nameof(address));
    private readonly int port = port is >= 0 and <= ushort.MaxValue
        ? port : throw new ArgumentOutOfRangeException(nameof(port));

    /// <summary>Listens until cancellation and drains admitted requests before returning.</summary>
    public void Run(CancellationToken cancellationToken = default) => Run(null, cancellationToken);

    /// <summary>Reports the bound address before accepting requests.</summary>
    public void Run(Action<IPEndPoint>? onListening, CancellationToken cancellationToken = default) =>
        RunAsync(onListening, cancellationToken).GetAwaiter().GetResult();

    /// <summary>Accepts and frames connections asynchronously; each connection carries one request.</summary>
    public Task RunAsync(CancellationToken cancellationToken = default) => RunAsync(null, cancellationToken);

    /// <summary>Reports the bound address before accepting requests asynchronously.</summary>
    public async Task RunAsync(Action<IPEndPoint>? onListening, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return;
        var listener = new TcpListener(address, port);
        var active = new List<Task>();
        using var stopping = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        listener.Start();
        try
        {
            onListening?.Invoke((IPEndPoint)listener.LocalEndpoint);
            while (!stopping.IsCancellationRequested)
            {
                foreach (Task completed in active.Where(task => task.IsCompleted).ToArray())
                {
                    await completed.ConfigureAwait(false);
                    active.Remove(completed);
                }
                if (active.Count == MaximumConnections)
                {
                    await Task.WhenAny(active).ConfigureAwait(false);
                    continue;
                }
                TcpClient client = await listener.AcceptTcpClientAsync(stopping.Token).ConfigureAwait(false);
                active.Add(ServeConnectionAsync(client, stopping.Token));
            }
        }
        catch (OperationCanceledException) when (stopping.IsCancellationRequested) { }
        finally
        {
            listener.Stop();
            await stopping.CancelAsync().ConfigureAwait(false);
            await Task.WhenAll(active).ConfigureAwait(false);
        }
    }

    private async Task ServeConnectionAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
        {
            timeout.CancelAfter(ClientTimeout);
            try { await ServeAsync(client, timeout.Token).ConfigureAwait(false); }
            catch (Exception failure) when (failure is IOException or SocketException
                or InvalidDataException or JsonException or OperationCanceledException) { }
        }
    }

    internal void Serve(TcpClient client) => ServeAsync(client, CancellationToken.None).GetAwaiter().GetResult();

    private async Task ServeAsync(TcpClient client, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(client);
        using NetworkStream stream = client.GetStream();
        EngineResponse response;
        try
        {
            byte[] bytes = await SocketFrame.ReadAsync(stream, cancellationToken).ConfigureAwait(false)
                ?? throw new EndOfStreamException("the client closed without a request");
            EngineRequest request = EngineJson.ReadRequest(bytes);
            // Only complete frames consume a worker. The endpoint serializes each game independently.
            response = await Task.Run(() => endpoint.Exchange(request), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception failure) when (failure is JsonException or InvalidDataException or EndOfStreamException)
        {
            response = Failure("invalid_frame", failure.Message);
        }
        byte[] payload = EncodeResponse(response);
        await SocketFrame.WriteAsync(stream, payload, cancellationToken).ConfigureAwait(false);
    }

    private static byte[] EncodeResponse(EngineResponse response)
    {
        try
        {
            byte[] payload = EngineJson.Write(response);
            SocketFrame.ValidatePayloadLength(payload.Length);
            return payload;
        }
        catch (Exception failure) when (failure is InvalidDataException or JsonException or NotSupportedException)
        {
            return EngineJson.Write(Failure("response_failed", "the engine response could not be represented on the wire"));
        }
    }

    private static EngineResponse Failure(string code, string message) => new(
        EngineProtocol.Version, RequestId: string.Empty, GameId: string.Empty,
        Capability: null, Prompt: null, Events: [], World: null, Error: new EngineError(code, message, code == "response_failed"
            ? EngineFailureDisposition.CommitUncertain : EngineFailureDisposition.Rejected));
}
