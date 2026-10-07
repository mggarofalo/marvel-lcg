using Marvel.Rules.State;

namespace Marvel.View.Tests;

internal sealed class VisibilityFacts : ICardFacts
{
    public CardKind Kind(string faceId) => faceId switch
    {
        "public-villain" => CardKind.EncounterVillain,
        "player-ally" => CardKind.Ally,
        "rule-insert" => CardKind.Insert,
        "underlying-player-card" => CardKind.Support,
        _ => CardKind.Event,
    };

    public string Title(string faceId) => faceId == "underlying-player-card"
        ? "Avengers Mansion"
        : faceId;

    public IReadOnlyList<string> Traits(string faceId) => faceId == "public-villain"
        ? ["BRUTE"]
        : [];

    public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
        faceId switch
        {
            "public-villain" => new Dictionary<string, string>(StringComparer.Ordinal)
                { ["SCH"] = "4", ["Class"] = "Encounter", ["Unique"] = "1" },
            "player-ally" => new Dictionary<string, string>(StringComparer.Ordinal)
                { ["HP"] = "3" },
            "underlying-player-card" => new Dictionary<string, string>(StringComparer.Ordinal)
                { ["HP"] = "7", ["Unique"] = "1" },
            _ => new Dictionary<string, string>(StringComparer.Ordinal),
        };

    public IReadOnlyList<string> Keywords(string faceId) => faceId == "public-villain"
        ? ["Guard"]
        : [];

    public string Text(string faceId) => faceId == "public-villain" ? "Guard." : string.Empty;

    public string FormattedText(string faceId) => faceId == "public-villain"
        ? "<b>Guard</b>."
        : string.Empty;

    public long PrintedValue(
        string faceId, string attribute, int players, long fallback = 0) =>
        Attributes(faceId).TryGetValue(attribute, out string? value)
            && long.TryParse(value, out long parsed) ? parsed : fallback;
}
