using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Marvel.Server;

/// <summary>A socket exchange failed before or after request transmission could begin.</summary>
public sealed class EngineTransportException : IOException
{
    /// <summary>Creates a transport failure without exposing its diagnostic as the message.</summary>
    public EngineTransportException(bool requestMayHaveCommitted, Exception innerException)
        : base("the engine transport exchange failed", innerException)
    {
        ArgumentNullException.ThrowIfNull(innerException);
        RequestMayHaveCommitted = requestMayHaveCommitted;
    }

    /// <summary>
    /// Whether request transmission began before the failure occurred.
    /// </summary>
    public bool RequestMayHaveCommitted { get; }
}

/// <summary>A TCP transport for the same request/response contract used in-process.</summary>
/// <remarks>
/// Each exchange is one connection and one length-prefixed UTF-8 JSON frame in
/// each direction. That lets the standalone process serve clients in sequence
/// without a connection monopolising its single game-state thread. Framing and
/// the 4 MiB limit are our protocol choices, not game rules.
/// </remarks>
public sealed class SocketTransport(
    string host,
    int port,
    OperationalLog? operationalLog = null) : IEngineTransport
{
    private readonly Action? onRequestWriteStarting;
    private readonly Action? onRequestCommitted;
    private readonly OperationalLog log = operationalLog ?? OperationalLog.None;
    private readonly string host =
        !string.IsNullOrWhiteSpace(host)
            ? host
            : throw new ArgumentException("a socket host is required", nameof(host));
    private readonly int port = port is > 0 and <= ushort.MaxValue
        ? port
        : throw new ArgumentOutOfRangeException(nameof(port));

    internal SocketTransport(
        string host, int port, Action onRequestCommitted)
        : this(host, port, operationalLog: null) =>
        this.onRequestCommitted = onRequestCommitted
            ?? throw new ArgumentNullException(nameof(onRequestCommitted));

    internal SocketTransport(
        string host,
        int port,
        Action onRequestWriteStarting,
        Action onRequestCommitted)
        : this(host, port, operationalLog: null)
    {
        this.onRequestWriteStarting = onRequestWriteStarting
            ?? throw new ArgumentNullException(nameof(onRequestWriteStarting));
        this.onRequestCommitted = onRequestCommitted
            ?? throw new ArgumentNullException(nameof(onRequestCommitted));
    }

    /// <inheritdoc />
    public async ValueTask<EngineResponse> ExchangeAsync(
        EngineRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        bool requestMayHaveCommitted = false;
        var elapsed = Stopwatch.StartNew();

        try
        {
            byte[] requestFrame = EngineJson.Write(request);
            SocketFrame.ValidatePayloadLength(requestFrame.Length);
            using var client = new TcpClient();
            await client.ConnectAsync(host, port, cancellationToken).ConfigureAwait(false);
            using NetworkStream stream = client.GetStream();
            // A frame is written in more than one socket operation. Once the
            // first operation can begin, a later client-side failure cannot
            // prove that the server did not receive and apply the whole frame.
            requestMayHaveCommitted = true;
            onRequestWriteStarting?.Invoke();
            await SocketFrame.WriteAsync(
                stream, requestFrame, cancellationToken).ConfigureAwait(false);
            onRequestCommitted?.Invoke();
            // The server may already have mutated game state, so cancellation
            // after transmission begins cannot discard the only authoritative
            // prompt and event list.
            byte[] response = await SocketFrame.ReadAsync(stream, CancellationToken.None)
                .ConfigureAwait(false)
                ?? throw new EndOfStreamException("the engine host closed without a response");
            int responseVersion = EngineJson.ReadResponseVersion(response);
            if (responseVersion != EngineProtocol.Version)
            {
                // A future response schema cannot be decoded strictly. The
                // transport preserves only its version mismatch and uses the
                // request's correlation labels so the client can report that
                // incompatibility instead of mistaking it for an outage. This is
                // our wire-compatibility choice, not a game rule.
                EngineResponse mismatch = new(
                    responseVersion,
                    request.RequestId,
                    request.GameId,
                    Capability: null,
                    Prompt: null,
                    Events: []);
                Observe(request, elapsed, "rejected", "unsupported_version");
                return mismatch;
            }

            EngineResponse parsed = EngineJson.ReadResponse(response);
            string disposition = parsed.Error is null
                ? "accepted"
                : parsed.Error.Code.StartsWith("stale_", StringComparison.Ordinal)
                    ? "stale"
                    : "rejected";
            Observe(
                request,
                elapsed,
                disposition,
                parsed.Error?.Code,
                parsed.Error is null
                    && request.Operation is not (EngineProtocol.Setup or EngineProtocol.Close)
                        ? parsed.Revision
                        : null);
            return parsed;
        }
        catch (OperationCanceledException) when (
            !requestMayHaveCommitted && cancellationToken.IsCancellationRequested)
        {
            Observe(request, elapsed, "cancelled", "transport_cancelled");
            throw;
        }
        catch (OperationCanceledException failure)
        {
            // Cancellation after transmission begins cannot prove that the
            // server did not apply the request. Preserve that uncertainty so
            // mutation clients synchronize instead of retrying the decision.
            Observe(request, elapsed, "uncertain", "transport_failed");
            throw new EngineTransportException(requestMayHaveCommitted, failure);
        }
        catch (Exception failure) when (failure is IOException
                                             or SocketException
                                             or InvalidDataException
                                             or JsonException
                                             or NotSupportedException)
        {
            Observe(
                request,
                elapsed,
                requestMayHaveCommitted ? "uncertain" : "rejected",
                "transport_failed");
            throw new EngineTransportException(requestMayHaveCommitted, failure);
        }
    }

    private void Observe(
        EngineRequest request,
        Stopwatch elapsed,
        string disposition,
        string? errorCode,
        long? revision = null)
    {
        elapsed.Stop();
        log.Write(
            OperationalEventIds.TransportCompleted,
            disposition,
            elapsed.ElapsedMilliseconds,
            request.RequestId,
            request.GameId,
            request.Operation,
            revision,
            errorCode: errorCode);
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
            while (!cancellationToken.IsCancellationRequested)
            {
                using TcpClient client = listener.AcceptTcpClient();
                using CancellationTokenRegistration disconnecting =
                    cancellationToken.Register(client.Close);
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
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
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

internal static class SocketFrame
{
    internal const int MaximumPayload = 4 * 1024 * 1024;

    internal static void ValidatePayloadLength(int payloadLength)
    {
        if (payloadLength > MaximumPayload)
        {
            throw new InvalidDataException(
                $"socket payload is {payloadLength} bytes; maximum is {MaximumPayload}");
        }
    }

    public static void Write(Stream stream, ReadOnlySpan<byte> payload)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ValidatePayloadLength(payload.Length);

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
        ValidatePayloadLength(payload.Length);

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
