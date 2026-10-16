using System.Collections.Immutable;
using System.Text.Json;

namespace Marvel.Content.Setup;

/// <summary>
/// Everything a board can be dealt from: scenarios, starter decks, encounter sets.
/// </summary>
/// <remarks>
/// <para>
/// Reads <c>datasets/setup/setup.json</c>. See <c>docs/setup-dataset.md</c>.
/// </para>
/// <para>
/// <b>Names, not paths.</b> A scenario or starter deck is identified by a bare
/// name; the dataset has already resolved every one and recorded any collision,
/// which is why nothing here needs a folder search order.
/// </para>
/// </remarks>
public sealed class SetupCatalog
{
    private readonly IReadOnlyDictionary<string, CampaignSetup> campaigns;
    private readonly IReadOnlyDictionary<string, HeroSetup> heroes;
    private readonly IReadOnlyDictionary<string, EncounterSetSetup> encounterSets;

    private SetupCatalog(
        IReadOnlyDictionary<string, CampaignSetup> campaigns,
        IReadOnlyDictionary<string, HeroSetup> heroes,
        IReadOnlyDictionary<string, EncounterSetSetup> encounterSets)
    {
        this.campaigns = campaigns;
        this.heroes = heroes;
        this.encounterSets = encounterSets;
    }

    /// <summary>The scenarios, by the name the engine resolves them under.</summary>
    public IReadOnlyCollection<string> CampaignNames => (IReadOnlyCollection<string>)campaigns.Keys;

    /// <summary>The starter decks, by hero name.</summary>
    public IReadOnlyCollection<string> HeroNames => (IReadOnlyCollection<string>)heroes.Keys;

    /// <summary>The named encounter sets.</summary>
    public IReadOnlyCollection<string> EncounterSetNames => (IReadOnlyCollection<string>)encounterSets.Keys;

    /// <summary>One scenario.</summary>
    /// <exception cref="KeyNotFoundException">
    /// The dataset has no such name. Deliberately not a null return: a board
    /// dealt one card short is worse than a board not dealt at all.
    /// </exception>
    public CampaignSetup Campaign(string name) => Get(campaigns, name, "campaign");

    /// <summary>One hero's starter deck.</summary>
    /// <exception cref="KeyNotFoundException">The dataset has no such name.</exception>
    public HeroSetup Hero(string name) => Get(heroes, name, "hero");

    /// <summary>One named encounter set's cards, in printed order.</summary>
    /// <exception cref="KeyNotFoundException">The dataset has no such name.</exception>
    public IReadOnlyList<string> EncounterSet(string name) =>
        Get(encounterSets, name, "encounter set").Cards;

    /// <summary>The authored printed name of one encounter set.</summary>
    public string EncounterSetDisplayName(string name) =>
        Get(encounterSets, name, "encounter set").Name;

    /// <summary>The authored classification used by setup selection.</summary>
    public EncounterSetRole EncounterRole(string name) => Get(encounterSets, name, "encounter set").Role;

    private static T Get<T>(IReadOnlyDictionary<string, T> from, string name, string what)
    {
        ArgumentNullException.ThrowIfNull(name);
        return from.TryGetValue(name, out var found)
            ? found
            : throw new KeyNotFoundException($"no {what} named '{name}' in the setup dataset");
    }

    /// <summary>Parses the canonical <c>setup.json</c> text.</summary>
    /// <exception cref="JsonException">The text is not a setup dataset.</exception>
    public static SetupCatalog Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        var campaigns = new Dictionary<string, CampaignSetup>(StringComparer.Ordinal);
        foreach (var entry in Section(root, "campaigns"))
        {
            if (!campaigns.TryAdd(entry.Name, ReadCampaign(entry.Value)))
                throw new JsonException($"duplicate campaign identity '{entry.Name}'");
        }

        var heroes = new Dictionary<string, HeroSetup>(StringComparer.Ordinal);
        foreach (var entry in Section(root, "heroes"))
        {
            if (!heroes.TryAdd(entry.Name, ReadHero(entry.Value)))
                throw new JsonException($"duplicate hero identity '{entry.Name}'");
        }

        var sets = new Dictionary<string, EncounterSetSetup>(StringComparer.Ordinal);
        foreach (var entry in Section(root, "encounter_sets"))
        {
            if (!sets.TryAdd(entry.Name, new EncounterSetSetup(
                    Text(entry.Value, "name"), Strings(entry.Value, "encounters"), Role(entry.Value))))
                throw new JsonException($"duplicate encounter set identity '{entry.Name}'");
        }

        return new SetupCatalog(campaigns, heroes, sets);
    }

    private static JsonElement.ObjectEnumerator Section(JsonElement root, string name) =>
        root.TryGetProperty(name, out var section) && section.ValueKind == JsonValueKind.Object
            ? section.EnumerateObject()
            : throw new JsonException($"the setup dataset has no '{name}' object");

    private static CampaignSetup ReadCampaign(JsonElement element) => new(
        Name: Text(element, "name"),
        Villain: Strings(element, "villain"),
        Expert: Boolean(element, "expert"),
        Challenges: Strings(element, "challenges"),
        Schemes: Strings(element, "schemes"),
        SetAside: Strings(element, "set_aside"),
        Encounters: Strings(element, "encounters"),
        EncounterSets: Strings(element, "encounter_sets"),
        ModularSets: Strings(element, "modular_sets"));

    private static HeroSetup ReadHero(JsonElement element) => new(
        Name: Text(element, "name"),
        Hero: Strings(element, "hero"),
        HeroDeck: Strings(element, "hero_deck"),
        Obligations: Strings(element, "obligations"),
        NemesisSet: Strings(element, "nemesis_set"),
        PlayerDeck: Strings(element, "player_deck"),
        NemesisSetIcon: Text(element, "nemesis_set_icon"));

    private static string Text(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()!
            : throw new JsonException($"expected a string '{name}'");

    private static bool Boolean(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value)
        && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : throw new JsonException($"expected a boolean '{name}'");

    private static EncounterSetRole Role(JsonElement element) => Text(element, "role") switch
    {
        "modular" => EncounterSetRole.Modular,
        "standard" => EncounterSetRole.Standard,
        "expert" => EncounterSetRole.Expert,
        "scenario" => EncounterSetRole.Scenario,
        "nemesis" => EncounterSetRole.Nemesis,
        var role => throw new JsonException($"unknown encounter set role '{role}'"),
    };

    // A missing list is an error rather than an empty one. Every record the
    // emitter writes carries every field its dataclass declares, including the
    // empty ones, so an absent key means the file is not what it claims to be.
    private static ImmutableArray<string> Strings(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException($"expected an array '{name}'");
        }

        var items = new List<string>(value.GetArrayLength());
        foreach (var item in value.EnumerateArray())
        {
            items.Add(item.GetString()
                      ?? throw new JsonException($"'{name}' holds a non-string"));
        }

        // Catalogs are shared across games. Our ingestion contract publishes
        // immutable recipes so callers cannot change another game's setup.
        return items.ToImmutableArray();
    }
}
