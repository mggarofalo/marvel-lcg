using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Marvel.Server;

/// <summary>A TCP transport for the same request/response contract used in-process.</summary>
/// <remarks>
/// Each exchange is one connection and one length-prefixed UTF-8 JSON frame in
/// each direction. That lets the standalone process serve clients in sequence
/// without a connection monopolising its single game-state thread. Framing and
/// the 4 MiB limit are our protocol choices, not game rules.
/// </remarks>
public sealed class SocketTransport(string host, int port) : IEngineTransport
{
    private readonly Action? onRequestCommitted;
    private readonly string host =
        !string.IsNullOrWhiteSpace(host)
            ? host
            : throw new ArgumentException("a socket host is required", nameof(host));
    private readonly int port = port is > 0 and <= ushort.MaxValue
        ? port
        : throw new ArgumentOutOfRangeException(nameof(port));

    internal SocketTransport(
        string host, int port, Action onRequestCommitted)
        : this(host, port) =>
        this.onRequestCommitted = onRequestCommitted
            ?? throw new ArgumentNullException(nameof(onRequestCommitted));

    /// <inheritdoc />
    public async ValueTask<EngineResponse> ExchangeAsync(
        EngineRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var client = new TcpClient();
        await client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
        using NetworkStream stream = client.GetStream();
        await SocketFrame.WriteAsync(
            stream, EngineJson.Write(request), cancellationToken).ConfigureAwait(false);
        onRequestCommitted?.Invoke();
        // The completed write is the transport's commit boundary. The server
        // may already have mutated game state, so cancellation after this
        // point cannot discard the only authoritative prompt and event list.
        byte[] response = await SocketFrame.ReadAsync(stream, CancellationToken.None)
            .ConfigureAwait(false)
            ?? throw new EndOfStreamException("the engine host closed without a response");
        return EngineJson.ReadResponse(response);
    }
}

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
    public void Run(CancellationToken cancellationToken = default)
    {
        var listener = new TcpListener(address, port);
        listener.Start();
        using CancellationTokenRegistration stopping =
            cancellationToken.Register(listener.Stop);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                using TcpClient client = listener.AcceptTcpClient();
                client.ReceiveTimeout = ClientTimeoutMilliseconds;
                client.SendTimeout = ClientTimeoutMilliseconds;
                try
                {
                    Serve(client);
                }
                catch (IOException)
                {
                    // A client that disappears or never finishes its frame
                    // cannot take down the listener or hold its one engine
                    // thread indefinitely. No game-state work happens after a
                    // failed read; a failed response is simply disconnected.
                }
                catch (SocketException)
                {
                }
                catch (InvalidDataException)
                {
                }
                catch (JsonException)
                {
                }
            }
        }
        catch (SocketException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            listener.Stop();
        }
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
                Error: new EngineError(
                    "response_failed",
                    "the engine response could not be represented on the wire"));
            SocketFrame.Write(stream, EngineJson.Write(fallback));
        }
    }
}

internal static class SocketFrame
{
    internal const int MaximumPayload = 4 * 1024 * 1024;

    public static void Write(Stream stream, ReadOnlySpan<byte> payload)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (payload.Length > MaximumPayload)
        {
            throw new InvalidDataException(
                $"socket payload is {payload.Length} bytes; maximum is {MaximumPayload}");
        }

        Span<byte> header = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(header, payload.Length);
        stream.Write(header);
        stream.Write(payload);
        stream.Flush();
    }

    public static byte[]? Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        Span<byte> header = stackalloc byte[sizeof(int)];
        int first = stream.ReadByte();
        if (first < 0)
        {
            return null;
        }

        header[0] = (byte)first;
        ReadExactly(stream, header[1..]);
        int length = BinaryPrimitives.ReadInt32BigEndian(header);
        if (length < 0 || length > MaximumPayload)
        {
            throw new InvalidDataException(
                $"socket frame length {length} is outside 0..{MaximumPayload}");
        }

        var payload = new byte[length];
        ReadExactly(stream, payload);
        return payload;
    }

    public static async ValueTask WriteAsync(
        Stream stream,
        ReadOnlyMemory<byte> payload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (payload.Length > MaximumPayload)
        {
            throw new InvalidDataException(
                $"socket payload is {payload.Length} bytes; maximum is {MaximumPayload}");
        }

        var header = new byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(header, payload.Length);
        await stream.WriteAsync(header, cancellationToken).ConfigureAwait(false);
        await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
    }

    public static async ValueTask<byte[]?> ReadAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var header = new byte[sizeof(int)];
        int first = await stream.ReadAsync(header.AsMemory(0, 1), cancellationToken)
            .ConfigureAwait(false);
        if (first == 0)
        {
            return null;
        }

        await stream.ReadExactlyAsync(header.AsMemory(1), cancellationToken)
            .ConfigureAwait(false);
        int length = BinaryPrimitives.ReadInt32BigEndian(header);
        if (length < 0 || length > MaximumPayload)
        {
            throw new InvalidDataException(
                $"socket frame length {length} is outside 0..{MaximumPayload}");
        }

        var payload = new byte[length];
        await stream.ReadExactlyAsync(payload, cancellationToken).ConfigureAwait(false);
        return payload;
    }

    private static void ReadExactly(Stream stream, Span<byte> buffer)
    {
        while (!buffer.IsEmpty)
        {
            int read = stream.Read(buffer);
            if (read == 0)
            {
                throw new EndOfStreamException("socket frame ended before its declared length");
            }

            buffer = buffer[read..];
        }
    }
}
