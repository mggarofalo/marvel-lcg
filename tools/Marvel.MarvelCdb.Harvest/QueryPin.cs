using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Marvel.MarvelCdb.Harvest;

public sealed record QueryPin(int Version, string Algorithm, int Count, string Hash)
{
    public static QueryPin Create(IEnumerable<string> codes)
    {
        var ordered = codes.Order(StringComparer.Ordinal).ToList();
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[sizeof(long)];
        foreach (string code in ordered)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(code);
            BinaryPrimitives.WriteInt64BigEndian(length, bytes.LongLength);
            digest.AppendData(length);
            digest.AppendData(bytes);
        }

        return new QueryPin(
            1,
            "sha256-length-prefixed-utf8",
            ordered.Count,
            "sha256:" + Convert.ToHexString(digest.GetHashAndReset()).ToLowerInvariant());
    }

    public static QueryPin Read(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        JsonElement root = document.RootElement;
        var pin = new QueryPin(
            root.GetProperty("version").GetInt32(),
            root.GetProperty("algorithm").GetString() ?? string.Empty,
            root.GetProperty("count").GetInt32(),
            root.GetProperty("hash").GetString() ?? string.Empty);
        if (pin.Version != 1
            || !string.Equals(pin.Algorithm, "sha256-length-prefixed-utf8", StringComparison.Ordinal)
            || !pin.Hash.StartsWith("sha256:", StringComparison.Ordinal))
        {
            throw new InvalidDataException("unsupported MarvelCDB query-universe pin");
        }

        return pin;
    }

    public void Verify(IEnumerable<string> codes)
    {
        QueryPin actual = Create(codes);
        if (actual.Count != Count || !string.Equals(actual.Hash, Hash, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"candidate query universe is not pinned: expected {Count} codes at {Hash}, "
                + $"received {actual.Count} at {actual.Hash}");
        }
    }

    public string Json() =>
        $$"""
        {
          "version": {{Version}},
          "algorithm": "{{Algorithm}}",
          "count": {{Count}},
          "hash": "{{Hash}}"
        }
        """.ReplaceLineEndings("\n") + "\n";
}
