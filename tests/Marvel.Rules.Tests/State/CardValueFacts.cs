using Marvel.Rules.State;

namespace Marvel.Rules.Tests.State;

internal sealed class CardValueFacts : ICardFacts
{
    public CardKind Kind(string faceId) => faceId == "hero" ? CardKind.Hero : CardKind.Upgrade;

    public string Title(string faceId) => "Title " + faceId;

    public IReadOnlyList<string> Traits(string faceId) => [];

    public IReadOnlyDictionary<string, string> Attributes(string faceId) => faceId switch
    {
        "hero" => new Dictionary<string, string> { ["ATK"] = "2", ["THW"] = "-", ["HP"] = "10" },
        "attachment" => new Dictionary<string, string> { ["ATK+"] = "1" },
        _ => new Dictionary<string, string>(),
    };

    public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
        Attributes(faceId).TryGetValue(attribute, out string? value) && long.TryParse(value, out long number)
            ? number : fallback;
}
