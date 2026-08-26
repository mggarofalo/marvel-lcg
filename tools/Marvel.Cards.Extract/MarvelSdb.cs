using System.Text.Json;

// `RepositoryPaths` is linked in from `tests/Shared/`. It answers "where is
// this repository" and not "where is this test", and one copy of that answer
// is the reason it lives in one file.
using Marvel.Tests;

namespace Marvel.Cards.Extract;

/// <summary>
/// One card as the vendored MarvelSDB snapshot records it.
/// </summary>
/// <remarks>
/// <para>
/// A thin reader over the JSON rather than a typed record per field: upstream
/// has 70-odd keys, this repository derives from about thirty of them, and a
/// property for each of the rest would be thirty places to keep in step with a
/// dataset nobody here controls.
/// </para>
/// <para>
/// <b>Absent and null are different.</b> Upstream writes <c>"attack": null</c>
/// on a character whose ATK is printed as a special value, and omits the key
/// entirely on one that has no ATK box at all. Both read as "no number" and
/// only one of them means the card has no such attribute, so the distinction
/// is kept rather than flattened — see <see cref="Has"/>.
/// </para>
/// </remarks>
internal sealed class SdbCard
{
    private readonly Dictionary<string, JsonElement> fields;

    private SdbCard(Dictionary<string, JsonElement> fields) => this.fields = fields;

    /// <summary>The card's upstream code, which is this repository's face id.</summary>
    public string Code => Text("code") ?? "";

    /// <summary>Reads one pack file.</summary>
    /// <param name="path">A file under <c>datasets/marvelsdb/pack/</c>.</param>
    public static IEnumerable<SdbCard> ReadPack(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        foreach (var element in document.RootElement.EnumerateArray())
        {
            var fields = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                fields[property.Name] = property.Value.Clone();
            }

            yield return new SdbCard(fields);
        }
    }

    /// <summary>Every card in the snapshot, by code.</summary>
    /// <remarks>
    /// Pack files are read in name order so that the result does not depend on
    /// the filesystem's, which is <c>AGENTS.md</c> non-negotiable 5 applied to
    /// a generator: the same inputs must produce the same bytes.
    /// </remarks>
    public static Dictionary<string, SdbCard> ReadAll()
    {
        var all = new Dictionary<string, SdbCard>(StringComparer.Ordinal);
        foreach (string path in Directory
            .EnumerateFiles(RepositoryPaths.Dataset("marvelsdb", "pack"), "*.json")
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            foreach (var card in ReadPack(path))
            {
                all[card.Code] = card;
            }
        }

        return all;
    }

    /// <summary>Whether the snapshot carries this key at all, null or not.</summary>
    public bool Has(string key) => fields.ContainsKey(key);

    /// <summary>A string field, or null.</summary>
    public string? Text(string key) =>
        fields.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>An integer field, or null.</summary>
    public int? Number(string key) =>
        fields.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : null;

    /// <summary>A boolean field, false when absent or null.</summary>
    public bool Flag(string key) =>
        fields.TryGetValue(key, out var value) && value.ValueKind == JsonValueKind.True;

    /// <summary>
    /// This card with a reprint's original merged underneath it.
    /// </summary>
    /// <remarks>
    /// Upstream records a reprint as a near-empty record with
    /// <c>duplicate_of</c> pointing at the original: 342 of the 4,298 cards
    /// carry nothing but a code, a position and that pointer. The printed card
    /// is the original's, so the original's fields are the answer, and the
    /// reprint's own keys win where it has them.
    /// </remarks>
    /// <param name="all">Every card, by code.</param>
    public SdbCard Resolved(IReadOnlyDictionary<string, SdbCard> all)
    {
        if (Text("duplicate_of") is not { } original
            || !all.TryGetValue(original, out var source))
        {
            return this;
        }

        var merged = new Dictionary<string, JsonElement>(source.fields, StringComparer.Ordinal);
        foreach (var (key, value) in fields)
        {
            if (value.ValueKind != JsonValueKind.Null)
            {
                merged[key] = value;
            }
        }

        return new SdbCard(merged);
    }
}
