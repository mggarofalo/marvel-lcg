using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Marvel.MarvelCdb.Harvest;

public sealed record AcquisitionPin(
    int Version,
    string Format,
    string Algorithm,
    long Bytes,
    string Hash,
    QueryPin Query)
{
    public static AcquisitionPin Create(byte[] bytes, Snapshot candidate)
    {
        candidate.VerifyPublishable();
        return new AcquisitionPin(
            1,
            "marvelcdb-faq-candidate-v1",
            "sha256",
            bytes.LongLength,
            "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
            QueryPin.Create(candidate.Queried));
    }

    public static AcquisitionPin Read(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        JsonElement root = document.RootElement;
        JsonElement query = root.GetProperty("query");
        var pin = new AcquisitionPin(
            root.GetProperty("version").GetInt32(),
            root.GetProperty("format").GetString() ?? string.Empty,
            root.GetProperty("algorithm").GetString() ?? string.Empty,
            root.GetProperty("bytes").GetInt64(),
            root.GetProperty("hash").GetString() ?? string.Empty,
            new QueryPin(
                query.GetProperty("version").GetInt32(),
                query.GetProperty("algorithm").GetString() ?? string.Empty,
                query.GetProperty("count").GetInt32(),
                query.GetProperty("hash").GetString() ?? string.Empty));
        if (pin.Version != 1
            || !string.Equals(pin.Format, "marvelcdb-faq-candidate-v1", StringComparison.Ordinal)
            || !string.Equals(pin.Algorithm, "sha256", StringComparison.Ordinal)
            || !pin.Hash.StartsWith("sha256:", StringComparison.Ordinal)
            || pin.Query.Version != 1
            || !string.Equals(
                pin.Query.Algorithm,
                "sha256-length-prefixed-utf8",
                StringComparison.Ordinal)
            || !pin.Query.Hash.StartsWith("sha256:", StringComparison.Ordinal))
        {
            throw new InvalidDataException("unsupported MarvelCDB acquisition pin");
        }

        return pin;
    }

    public void Verify(byte[] bytes, Snapshot candidate)
    {
        string hash = "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (bytes.LongLength != Bytes || !string.Equals(hash, Hash, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"candidate bytes are not the reviewed acquisition: expected {Bytes} bytes at {Hash}, "
                + $"received {bytes.LongLength} at {hash}");
        }

        Query.Verify(candidate.Queried);
    }

    public string Json() =>
        $$"""
        {
          "version": {{Version}},
          "format": "{{Format}}",
          "algorithm": "{{Algorithm}}",
          "bytes": {{Bytes}},
          "hash": "{{Hash}}",
          "query": {
            "version": {{Query.Version}},
            "algorithm": "{{Query.Algorithm}}",
            "count": {{Query.Count}},
            "hash": "{{Query.Hash}}"
          }
        }
        """.ReplaceLineEndings("\n") + "\n";
}
