using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Rulings.Harvest;

public static class Emit
{
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(false, true);

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static string Json(IReadOnlyList<Ruling> rulings, string harvested) =>
        (JsonSerializer.Serialize(new Snapshot(
            1,
            harvested,
            "https://hallofheroeslcg.com",
            rulings.OrderBy(ruling => ruling.Id, StringComparer.Ordinal).ToList()), Options) + "\n")
            .ReplaceLineEndings("\n");

    public static byte[] JsonBytes(IReadOnlyList<Ruling> rulings, string harvested) =>
        Utf8WithoutBom.GetBytes(Json(rulings, harvested));

    public static byte[] ManifestBytes(IReadOnlyDictionary<Page, byte[]> pages)
    {
        var files = pages.Select(pair => new ManifestFile(
            "pages/" + pair.Key.FileName,
            pair.Value.LongLength,
            "sha256:" + Convert.ToHexString(SHA256.HashData(pair.Value)).ToLowerInvariant()))
            .OrderBy(file => file.Path, StringComparer.Ordinal)
            .ToList();
        string json = JsonSerializer.Serialize(
            new Manifest(1, "sha256", files),
            Options).ReplaceLineEndings("\n") + "\n";
        return Utf8WithoutBom.GetBytes(json);
    }

    private sealed record Snapshot(
        int Version,
        string Harvested,
        string Source,
        IReadOnlyList<Ruling> Rulings);

    private sealed record Manifest(
        int Version,
        string Algorithm,
        IReadOnlyList<ManifestFile> Files);

    private sealed record ManifestFile(
        string Path,
        long Bytes,
        string Hash);
}
