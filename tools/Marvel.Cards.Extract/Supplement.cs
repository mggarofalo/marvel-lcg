using System.Text.Json;
using Marvel.Tests;

namespace Marvel.Cards.Extract;

/// <summary>
/// Printed facts the vendored snapshot does not record, authored here.
/// </summary>
/// <remarks>
/// <para>
/// MarvelSDB transcribes what a card says and most of what its boxes hold, and
/// there are two places it stops. <b>Cards it does not have at all</b> — the
/// status cards the engine makes mid-game are the clearest case, since they
/// are not printed cards. And <b>printed modifiers on attachments and
/// upgrades</b>, the small <c>ATK +1</c> in a stat box, which upstream records
/// for some and not others.
/// </para>
/// <para>
/// Every entry says why it is here. That is the whole discipline: a supplement
/// nobody can audit is a place for a made-up number to live, and the reason is
/// what tells a reader whether the entry is a transcription or a guess.
/// </para>
/// </remarks>
internal sealed class Supplement
{
    private readonly Dictionary<string, Entry> entries;

    private Supplement(Dictionary<string, Entry> entries, List<Card> only)
    {
        this.entries = entries;
        Only = only;
    }

    /// <summary>Cards the snapshot does not have.</summary>
    public IReadOnlyList<Card> Only { get; }

    /// <summary>Cards the snapshot has that this dataset does not.</summary>
    public HashSet<string> Dropped { get; private init; } = [];

    /// <summary>How many snapshot cards the supplement changed.</summary>
    public int Touched { get; private set; }

    /// <summary>Reads <c>datasets/cards/supplement.json</c>.</summary>
    public static Supplement Read()
    {
        string path = RepositoryPaths.Dataset("cards", "supplement.json");
        if (!File.Exists(path))
        {
            return new Supplement(new Dictionary<string, Entry>(StringComparer.Ordinal), []);
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var found = new Dictionary<string, Entry>(StringComparer.Ordinal);
        var only = new List<Card>();

        // Grouped by reason rather than listed flat: the reason is what tells a
        // reader whether an entry is a transcription or a guess, and repeating
        // it per card would make it something nobody reads.
        foreach (var element in document.RootElement.GetProperty("groups")
            .EnumerateArray()
            .SelectMany(group => group.GetProperty("cards").EnumerateArray()))
        {
            string id = element.GetProperty("card_id").GetString()!;
            var attributes = new SortedDictionary<string, string>(StringComparer.Ordinal);
            if (element.TryGetProperty("attributes", out var printed))
            {
                foreach (var attribute in printed.EnumerateObject())
                {
                    attributes[attribute.Name] = attribute.Value.GetString() ?? "";
                }
            }

            var traits = new List<string>();
            if (element.TryGetProperty("traits", out var written))
            {
                traits.AddRange(written.EnumerateArray().Select(trait => trait.GetString()!));
            }

            if (element.TryGetProperty("type", out var kind))
            {
                // A whole card rather than a correction: the snapshot has no
                // record to correct.
                only.Add(new Card(
                    id,
                    Field(element, "name"),
                    Field(element, "subname"),
                    kind.GetString() ?? "",
                    traits,
                    attributes,
                    Field(element, "text"),
                    Field(element, "pack"),
                    Field(element, "set")));
                continue;
            }

            // Merged rather than replaced: a card can want an entry in two
            // groups -- 12028 Size Increase is missing a trait *and* an
            // abbreviated `Uses`, which are two different reasons -- and each
            // group is written to say one thing about it.
            var already = found.GetValueOrDefault(id);
            var merged = new SortedDictionary<string, string>(
                already?.Attributes.ToDictionary(
                    pair => pair.Key, pair => pair.Value, StringComparer.Ordinal)
                    ?? new Dictionary<string, string>(StringComparer.Ordinal),
                StringComparer.Ordinal);

            foreach (var (key, value) in attributes)
            {
                merged[key] = value;
            }

            found[id] = new Entry(
                merged,
                element.TryGetProperty("traits", out _) ? traits : already?.Traits);
        }

        var dropped = new HashSet<string>(StringComparer.Ordinal);
        if (document.RootElement.TryGetProperty("dropped", out var leave))
        {
            foreach (var card in leave.GetProperty("cards").EnumerateArray())
            {
                dropped.Add(card.GetString()!);
            }
        }

        return new Supplement(found, only) { Dropped = dropped };
    }

    /// <summary>Applies whatever the supplement says about one card.</summary>
    /// <param name="card">The card as the snapshot gives it.</param>
    public Card Apply(Card card)
    {
        if (!entries.TryGetValue(card.Id, out var entry))
        {
            return card;
        }

        Touched += 1;
        var attributes = new SortedDictionary<string, string>(
            card.Attributes.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            StringComparer.Ordinal);

        foreach (var (key, value) in entry.Attributes)
        {
            // An empty value removes the attribute, which is how the
            // supplement says "the snapshot records this and the card does not
            // print it".
            if (value.Length == 0)
            {
                attributes.Remove(key);
            }
            else
            {
                attributes[key] = value;
            }
        }

        return card with
        {
            Attributes = attributes,
            Traits = entry.Traits ?? card.Traits,
        };
    }

    /// <summary>
    /// A starting supplement: what the dataset being replaced knew and the
    /// snapshot does not record.
    /// </summary>
    /// <remarks>
    /// Only where the snapshot is <b>silent</b>. Where it records a value and
    /// the old dataset disagreed, the snapshot wins and nothing is proposed:
    /// it is a transcription of the printed card and the other was an engine's
    /// reading of one.
    /// </remarks>
    /// <param name="was">The dataset being replaced.</param>
    /// <param name="now">What the generator produces without a supplement.</param>
    public static string Propose(
        IReadOnlyDictionary<string, Card> was, IReadOnlyDictionary<string, Card> now)
    {
        var lines = new List<string>();
        foreach (string id in was.Keys.OrderBy(id => id, StringComparer.Ordinal))
        {
            var old = was[id];
            if (!now.TryGetValue(id, out var built))
            {
                lines.Add(
                    $$"""    {"card_id": "{{id}}", "name": {{Quote(old.Name)}}, "subname": {{Quote(old.Subname)}}, "type": "{{old.Kind}}", "traits": [{{string.Join(", ", old.Traits.Select(Quote))}}], "attributes": {{{string.Join(", ", old.Attributes.Select(pair => $"{Quote(pair.Key)}: {Quote(pair.Value)}"))}}}, "text": {{Quote(old.Text)}}, "pack": {{Quote(old.Pack)}}, "set": {{Quote(old.Set)}}},""");
                continue;
            }

            var missing = old.Attributes
                .Where(pair => !built.Attributes.ContainsKey(pair.Key))
                .ToList();
            bool traits = old.Traits.Count > 0 && built.Traits.Count == 0;
            if (missing.Count == 0 && !traits)
            {
                continue;
            }

            string written = string.Join(
                ", ", missing.Select(pair => $"{Quote(pair.Key)}: {Quote(pair.Value)}"));
            string carried = traits
                ? $", \"traits\": [{string.Join(", ", old.Traits.Select(Quote))}]"
                : "";
            lines.Add($$"""    {"card_id": "{{id}}", "attributes": {{{written}}}{{carried}}},""");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static string Quote(string text) => JsonSerializer.Serialize(text);

    private static string Field(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";

    private sealed record Entry(
        IReadOnlyDictionary<string, string> Attributes, IReadOnlyList<string>? Traits);
}
