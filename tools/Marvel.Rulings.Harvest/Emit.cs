using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Rulings.Harvest;

public static class Emit
{
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

    private sealed record Snapshot(
        int Version,
        string Harvested,
        string Source,
        IReadOnlyList<Ruling> Rulings);
}
