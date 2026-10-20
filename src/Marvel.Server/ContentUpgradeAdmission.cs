using System.Text.Json;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>Admits only known predecessor content whose executable entries remain unchanged.</summary>
internal sealed class ContentUpgradeAdmission
{
    private static readonly string[] SetupSections = ["campaigns", "encounter_sets", "heroes"];
    private readonly SessionCompatibility target;
    private readonly SessionCompatibility? source;

    internal ContentUpgradeAdmission(
        SessionCompatibility target, byte[] cards, byte[] setup, byte[] abilities)
    {
        this.target = target;
        using var resource = typeof(ContentUpgradeAdmission).Assembly.GetManifestResourceStream(
            "Marvel.Server.ContentCompatibility.core-rors-91098022.json")
            ?? throw new InvalidOperationException("the supported content baseline is missing");
        var baseline = JsonSerializer.Deserialize<ContentCompatibilityBaseline>(resource)
            ?? throw new InvalidOperationException("the supported content baseline is malformed");
        using var cardData = JsonDocument.Parse(cards);
        using var setupData = JsonDocument.Parse(setup);
        using var abilityData = JsonDocument.Parse(abilities);
        var cardEntries = cardData.RootElement.GetProperty("cards").EnumerateArray()
            .ToDictionary(row => row.GetProperty("card_id").GetString()!, StringComparer.Ordinal);
        var abilityEntries = abilityData.RootElement.GetProperty("cards").EnumerateArray()
            .ToDictionary(row => row.GetProperty("card").GetString()!, StringComparer.Ordinal);
        var setupEntries = SetupEntries(setupData.RootElement);
        if (Matches(baseline.Cards, cardEntries) && Matches(baseline.Setup, setupEntries)
            && Matches(baseline.Abilities, abilityEntries))
            source = baseline.Source;
    }

    internal SessionContentUpgrade? For(SessionCompatibility actual) =>
        source is not null && actual with { Application = source.Application } == source
            && actual with { Application = target.Application } != target
            ? new SessionContentUpgrade(source, target) : null;

    private static bool Matches(
        Dictionary<string, string> expected, Dictionary<string, JsonElement> actual) =>
        expected.All(entry => actual.TryGetValue(entry.Key, out var value)
            && ContentEntryFingerprint.Of(value) == entry.Value);

    internal static Dictionary<string, JsonElement> SetupEntries(JsonElement root) =>
        SetupSections
            .SelectMany(section => root.GetProperty(section).EnumerateObject()
                .Select(entry => KeyValuePair.Create(section + "/" + entry.Name, entry.Value)))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
}
