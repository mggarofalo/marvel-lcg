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
    private readonly string host =
        !string.IsNullOrWhiteSpace(host)
            ? host
            : throw new ArgumentException("a socket host is required", nameof(host));
    private readonly int port = port is > 0 and <= ushort.MaxValue
        ? port
        : throw new ArgumentOutOfRangeException(nameof(port));

    /// <inheritdoc />
    public EngineResponse Exchange(EngineRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        using var client = new TcpClient();
        client.Connect(host, port);
        using NetworkStream stream = client.GetStream();
        SocketFrame.Write(stream, EngineJson.Write(request));
        byte[] response = SocketFrame.Read(stream)
            ?? throw new EndOfStreamException("the engine host closed without a response");
        return EngineJson.ReadResponse(response);
    }
}

/// <summary>The standalone, sequential socket entry point for an engine endpoint.</summary>
public sealed class SocketEngineServer(IEngineEndpoint endpoint, IPAddress address, int port)
{
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
                Serve(client);
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
                Prompt: null,
                Events: [],
                Error: new EngineError("invalid_frame", failure.Message));
        }

        SocketFrame.Write(stream, EngineJson.Write(response));
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
