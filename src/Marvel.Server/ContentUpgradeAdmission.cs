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

    internal SessionContentUpgrade? For(SessionCompatibility actual)
    {
        if (target.ReplayContract != "engine-replay-v20" || source is null
            || actual with { Application = target.Application } == target) return null;
        var admitted = source;
        // This release's v20 runtime also writes the frozen Core/RoRS content triple.
        // It is a distinct named source, not a claim that newly recorded games are historical v19 saves.
        if (target.ReplayContract == "engine-replay-v20" && actual.ReplayContract == "engine-replay-v20")
            admitted = source with { ReplayContract = "engine-replay-v20" };
        if (actual with { Application = admitted.Application } != admitted) return null;
        return new SessionContentUpgrade(admitted, target,
            admitted.ReplayContract == "engine-replay-v19" && target.ReplayContract == "engine-replay-v20"
                ? SessionReplayUpgrade.CoreRorsV19ToV20 : SessionReplayUpgrade.Unchanged);
    }

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
