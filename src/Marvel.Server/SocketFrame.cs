using System.Buffers.Binary;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

namespace Marvel.Server;

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
