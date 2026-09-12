using System.Buffers;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;

namespace Marvel.Sim;

/// <summary>Opens simulation record containers without changing their JSONL payload.</summary>
internal static class SimulationRecordFiles
{
    private const byte GzipIdOne = 0x1f;
    private const byte GzipIdTwo = 0x8b;

    public static TextWriter CreateWriter(string path)
    {
        string fullPath = Path.GetFullPath(path);
        string? parent = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(parent))
        {
            Directory.CreateDirectory(parent);
        }

        var file = new FileStream(
            fullPath, FileMode.Create, FileAccess.Write, FileShare.Read,
            bufferSize: 4096, FileOptions.SequentialScan);
        try
        {
            Stream output = fullPath.EndsWith(".jsonl.gz", StringComparison.OrdinalIgnoreCase)
                ? new GZipStream(file, CompressionLevel.Optimal)
                : file;
            return new StreamWriter(output, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
        catch
        {
            file.Dispose();
            throw;
        }
    }

    public static IEnumerable<string> ReadLines(string path)
    {
        string fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new SimulationUsageException($"record does not exist: {fullPath}");
        }

        return EnumerateLines(fullPath);
    }

    private static IEnumerable<string> EnumerateLines(string path)
    {
        using var file = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, FileOptions.SequentialScan);
        bool compressed = file.ReadByte() == GzipIdOne && file.ReadByte() == GzipIdTwo;
        file.Position = 0;
        if (compressed)
        {
            ValidateGzip(file, path);
            file.Position = 0;
        }

        using GzipMemberReadStream? gzip = compressed
            ? new GzipMemberReadStream(file)
            : null;
        Stream input = gzip is null ? file : gzip;
        using var reader = new StreamReader(
            input, Encoding.UTF8, detectEncodingFromByteOrderMarks: true,
            bufferSize: 4096, leaveOpen: true);

        while (ReadLine(reader, compressed, path) is { } line)
        {
            yield return line;
        }
    }

    private static void ValidateGzip(FileStream file, string path)
    {
        // Validate every member before exposing JSONL so a container error is
        // never hidden by parsing the corrupted decompressed bytes.
        try
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(81920);
            try
            {
                Span<byte> trailer = stackalloc byte[8];
                while (file.Position < file.Length)
                {
                    ReadHeader(file);
                    var checksum = new Crc32();
                    using (var deflate = new DeflateStream(
                               new SingleByteReadStream(file),
                               CompressionMode.Decompress))
                    {
                        int read;
                        while ((read = deflate.Read(buffer)) > 0)
                        {
                            checksum.Append(buffer.AsSpan(0, read));
                        }
                    }

                    file.ReadExactly(trailer);
                    uint expectedCrc = BinaryPrimitives.ReadUInt32LittleEndian(trailer[..4]);
                    uint expectedSize = BinaryPrimitives.ReadUInt32LittleEndian(trailer[4..]);
                    if (checksum.Value != expectedCrc || checksum.Size != expectedSize)
                    {
                        throw new InvalidDataException(
                            "gzip trailer does not match its member content");
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (Exception error) when (error is InvalidDataException or EndOfStreamException)
        {
            throw InvalidGzip(path, error);
        }
    }

    private static void ReadHeader(Stream input)
    {
        var checksum = new Crc32();
        byte idOne = ReadHeaderByte(input, checksum);
        byte idTwo = ReadHeaderByte(input, checksum);
        byte compressionMethod = ReadHeaderByte(input, checksum);
        byte flags = ReadHeaderByte(input, checksum);
        ValidateHeaderIdentity(idOne, idTwo, compressionMethod, flags);

        for (int index = 0; index < 6; index++)
        {
            _ = ReadHeaderByte(input, checksum);
        }

        ReadOptionalHeaderFields(input, checksum, flags);
    }

    private static void ValidateHeaderIdentity(
        byte idOne, byte idTwo, byte compressionMethod, byte flags)
    {
        if (idOne != GzipIdOne || idTwo != GzipIdTwo || compressionMethod != 8
            || (flags & 0xe0) != 0)
            throw new InvalidDataException("gzip member header is invalid");
    }

    private static void ReadOptionalHeaderFields(Stream input, Crc32 checksum, byte flags)
    {
        if ((flags & 0x04) != 0) ReadExtraField(input, checksum);
        if ((flags & 0x08) != 0) ReadNullTerminated(input, checksum);
        if ((flags & 0x10) != 0) ReadNullTerminated(input, checksum);
        if ((flags & 0x02) != 0) ValidateHeaderChecksum(input, checksum);
    }

    private static void ReadExtraField(Stream input, Crc32 checksum)
    {
        int length = ReadHeaderByte(input, checksum) | ReadHeaderByte(input, checksum) << 8;
        for (int index = 0; index < length; index++) _ = ReadHeaderByte(input, checksum);
    }

    private static void ValidateHeaderChecksum(Stream input, Crc32 checksum)
    {
        int expected = ReadRequiredByte(input) | ReadRequiredByte(input) << 8;
        if ((checksum.Value & 0xffff) != expected)
            throw new InvalidDataException("gzip member header checksum is invalid");
    }

    private static byte ReadHeaderByte(Stream input, Crc32 checksum)
    {
        byte value = ReadRequiredByte(input);
        checksum.Append(value);
        return value;
    }

    private static void ReadNullTerminated(Stream input, Crc32 checksum)
    {
        while (ReadHeaderByte(input, checksum) != 0)
        {
        }
    }

    private static byte ReadRequiredByte(Stream input)
    {
        int value = input.ReadByte();
        return value < 0
            ? throw new EndOfStreamException("gzip member ended unexpectedly")
            : (byte)value;
    }

    private static string? ReadLine(StreamReader reader, bool compressed, string path)
    {
        try
        {
            return reader.ReadLine();
        }
        catch (InvalidDataException error) when (compressed)
        {
            throw InvalidGzip(path, error);
        }
    }

    private static SimulationUsageException InvalidGzip(
        string path, Exception innerException) =>
        new($"record is not valid gzip: {path}", innerException);

    private sealed class SingleByteReadStream(Stream inner) : Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            inner.Read(buffer, offset, Math.Min(count, 1));

        public override int Read(Span<byte> buffer) =>
            inner.Read(buffer[..Math.Min(buffer.Length, 1)]);

        public override int ReadByte() => inner.ReadByte();

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();
    }

    private sealed class GzipMemberReadStream(Stream inner) : Stream
    {
        private readonly byte[] trailer = new byte[8];
        private DeflateStream? member;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count) =>
            Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer)
        {
            if (buffer.IsEmpty)
            {
                return 0;
            }

            while (true)
            {
                if (member is null)
                {
                    if (inner.Position == inner.Length)
                    {
                        return 0;
                    }

                    ReadHeader(inner);
                    member = new DeflateStream(
                        new SingleByteReadStream(inner), CompressionMode.Decompress);
                }

                int read = member.Read(buffer);
                if (read > 0)
                {
                    return read;
                }

                member.Dispose();
                member = null;
                inner.ReadExactly(trailer);
            }
        }

        public override int ReadByte()
        {
            Span<byte> value = stackalloc byte[1];
            return Read(value) == 0 ? -1 : value[0];
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                member?.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class Crc32
    {
        private static readonly uint[] Table = BuildTable();
        private uint value = uint.MaxValue;

        public uint Value => ~value;
        public uint Size { get; private set; }

        public void Append(byte item)
        {
            value = Table[(value ^ item) & 0xff] ^ (value >> 8);
            Size = unchecked(Size + 1);
        }

        public void Append(ReadOnlySpan<byte> bytes)
        {
            foreach (byte item in bytes)
            {
                Append(item);
            }
        }

        private static uint[] BuildTable()
        {
            var table = new uint[256];
            for (uint index = 0; index < table.Length; index++)
            {
                uint entry = index;
                for (int bit = 0; bit < 8; bit++)
                {
                    entry = (entry & 1) == 0
                        ? entry >> 1
                        : 0xedb88320u ^ (entry >> 1);
                }

                table[index] = entry;
            }

            return table;
        }
    }
}
