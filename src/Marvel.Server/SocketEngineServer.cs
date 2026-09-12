using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Marvel.Server;

/// <summary>The standalone, sequential socket entry point for an engine endpoint.</summary>
public sealed class SocketEngineServer(IEngineEndpoint endpoint, IPAddress address, int port)
{
    private const int ClientTimeoutMilliseconds = 30_000;
    private readonly IEngineEndpoint endpoint =
        endpoint ?? throw new ArgumentNullException(nameof(endpoint));
    private readonly IPAddress address =
        address ?? throw new ArgumentNullException(nameof(address));
    private readonly int port = port is >= 0 and <= ushort.MaxValue
        ? port
        : throw new ArgumentOutOfRangeException(nameof(port));

    /// <summary>Listens until cancellation, handling one request per connection.</summary>
    public void Run(CancellationToken cancellationToken = default) =>
        Run(onListening: null, cancellationToken);

    internal void Run(
        Action<IPEndPoint>? onListening,
        CancellationToken cancellationToken = default)
    {
        var listener = new TcpListener(address, port);

        try
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            listener.Start();
            using CancellationTokenRegistration stopping =
                cancellationToken.Register(listener.Stop);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            onListening?.Invoke((IPEndPoint)listener.LocalEndpoint);
            ServeUntilCancelled(listener, cancellationToken);
        }
        catch (SocketException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            listener.Stop();
        }
    }

    private void ServeUntilCancelled(TcpListener listener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            using TcpClient client = listener.AcceptTcpClient();
            client.ReceiveTimeout = ClientTimeoutMilliseconds;
            client.SendTimeout = ClientTimeoutMilliseconds;
            using CancellationTokenRegistration disconnecting =
                cancellationToken.Register(client.Close);
            TryServe(client);
        }
    }

    private void TryServe(TcpClient client)
    {
        try { Serve(client); }
        catch (IOException) { }
        catch (SocketException) { }
        catch (InvalidDataException) { }
        catch (JsonException) { }
    }

    internal void Serve(TcpClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        using NetworkStream stream = client.GetStream();
        EngineResponse response;

        try
        {
            byte[] requestBytes = SocketFrame.Read(stream)
                ?? throw new EndOfStreamException("the client closed without a request");
            EngineRequest request = EngineJson.ReadRequest(requestBytes);
            response = endpoint.Exchange(request);
        }
        catch (Exception failure) when (failure is JsonException
                                             or InvalidDataException
                                             or EndOfStreamException)
        {
            response = new EngineResponse(
                EngineProtocol.Version,
                RequestId: string.Empty,
                GameId: string.Empty,
                Capability: null,
                Prompt: null,
                Events: [],
                World: null,
                Error: new EngineError("invalid_frame", failure.Message));
        }

        WriteResponse(stream, response);
    }

    private static void WriteResponse(Stream stream, EngineResponse response)
    {
        try
        {
            SocketFrame.Write(stream, EngineJson.Write(response));
        }
        catch (Exception failure) when (failure is InvalidDataException
                                             or JsonException
                                             or NotSupportedException)
        {
            // Serialization finishes and the size is checked before a frame
            // byte is written, so this compact fallback cannot be appended to
            // a partial protocol response. It is deliberately independent of
            // the failed response's client-controlled ids and diagnostic.
            var fallback = new EngineResponse(
                EngineProtocol.Version,
                RequestId: string.Empty,
                GameId: string.Empty,
                Capability: null,
                Prompt: null,
                Events: [],
                World: null,
                Error: new EngineError(
                    "response_failed",
                    "the engine response could not be represented on the wire"));
            SocketFrame.Write(stream, EngineJson.Write(fallback));
        }
    }
}
