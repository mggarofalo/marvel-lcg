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
        if (target.ReplayContract is not ("engine-replay-v22" or "engine-replay-v23" or "engine-replay-v24" or "engine-replay-v25" or "engine-replay-v26") || source is null
            || actual with { Application = target.Application } == target) return null;
        var route = (actual.ReplayContract, target.ReplayContract) switch
        {
            ("engine-replay-v19", "engine-replay-v22") => SessionReplayUpgrade.CoreRorsV19ToV22,
            ("engine-replay-v20", "engine-replay-v22") => SessionReplayUpgrade.CoreRorsV20ToV22,
            ("engine-replay-v21", "engine-replay-v22") => SessionReplayUpgrade.CoreRorsV21ToV22,
            ("engine-replay-v22", "engine-replay-v22") => SessionReplayUpgrade.Unchanged,
            ("engine-replay-v19", "engine-replay-v23") => SessionReplayUpgrade.CoreRorsV19ToV23,
            ("engine-replay-v20", "engine-replay-v23") => SessionReplayUpgrade.CoreRorsV20ToV23,
            ("engine-replay-v21", "engine-replay-v23") => SessionReplayUpgrade.CoreRorsV21ToV23,
            ("engine-replay-v23", "engine-replay-v23") => SessionReplayUpgrade.Unchanged,
            ("engine-replay-v19", "engine-replay-v24") => SessionReplayUpgrade.CoreRorsV19ToV24,
            ("engine-replay-v20", "engine-replay-v24") => SessionReplayUpgrade.CoreRorsV20ToV24,
            ("engine-replay-v21", "engine-replay-v24") => SessionReplayUpgrade.CoreRorsV21ToV24,
            ("engine-replay-v24", "engine-replay-v24") => SessionReplayUpgrade.Unchanged,
            ("engine-replay-v19", "engine-replay-v25") => SessionReplayUpgrade.CoreRorsV19ToV25,
            ("engine-replay-v20", "engine-replay-v25") => SessionReplayUpgrade.CoreRorsV20ToV25,
            ("engine-replay-v21", "engine-replay-v25") => SessionReplayUpgrade.CoreRorsV21ToV25,
            ("engine-replay-v25", "engine-replay-v25") => SessionReplayUpgrade.Unchanged,
            ("engine-replay-v19", "engine-replay-v26") => SessionReplayUpgrade.CoreRorsV19ToV26,
            ("engine-replay-v20", "engine-replay-v26") => SessionReplayUpgrade.CoreRorsV20ToV26,
            ("engine-replay-v21", "engine-replay-v26") => SessionReplayUpgrade.CoreRorsV21ToV26,
            ("engine-replay-v26", "engine-replay-v26") => SessionReplayUpgrade.Unchanged,
            _ => (SessionReplayUpgrade?)null,
        };
        if (route is null) return null;
        // Each source names the frozen content triple; every original gameplay fact
        // must still verify under this delivery before provenance can change.
        var admitted = source with { ReplayContract = actual.ReplayContract };
        if (actual with { Application = admitted.Application } != admitted) return null;
        return new SessionContentUpgrade(admitted, target, route.Value);
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
