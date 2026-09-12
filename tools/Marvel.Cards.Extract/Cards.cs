using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Marvel.Cards.Extract;

/// <summary>Writing and reading the dataset.</summary>
/// <remarks>
/// <b>The spelling is the contract.</b> A generated dataset is only useful if
/// the same inputs give the same bytes, so what the writer emits is the
/// canonical form: two-space indent, keys in the order below, cards in card-id
/// order, and no escaping beyond what JSON requires. That is our choice, and
/// the only property it has to keep is holding still.
/// </remarks>
internal static class Cards
{
    /// <summary>Serialises the dataset.</summary>
    /// <param name="cards">Every card, in card-id order.</param>
    /// <param name="supplement">What the supplement contributed, for the header.</param>
    public static string Write(IReadOnlyList<Card> cards, Supplement supplement)
    {
        var options = new JsonWriterOptions
        {
            Indented = true,
            // Not the default encoder, which escapes for HTML safety and
            // reaches the apostrophe in `BATROC'S BRIGADE` and every non-ASCII
            // character in a card's text. A dataset is not HTML.
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder
                .UnsafeRelaxedJsonEscaping,
        };

        var buffer = new System.Buffers.ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, options))
        {
            writer.WriteStartObject();
            writer.WriteNumber("dataset_version"u8, 3);
            writer.WriteString("generated_from"u8, "datasets/marvelsdb/");
            writer.WriteString(
                "generated_by"u8,
                "dotnet run --project tools/Marvel.Cards.Extract -- write");
            writer.WriteString(
                "note"u8,
                "Printed card text and stats, derived from the vendored MarvelSDB snapshot "
                + "and from datasets/cards/supplement.json where MarvelSDB records nothing. "
                + "Regenerate rather than edit: `-- check` fails the build on a hand edit.");

            writer.WriteStartObject("counts"u8);
            writer.WriteNumber("cards"u8, cards.Count);
            writer.WriteNumber("supplemented"u8, supplement.Touched);
            writer.WriteNumber("engine_only"u8, supplement.Only.Count);
            writer.WriteEndObject();

            writer.WriteStartArray("cards"u8);
            foreach (var card in cards)
            {
                writer.WriteStartObject();
                writer.WriteString("card_id"u8, card.Id);
                writer.WriteString("name"u8, card.Name);
                writer.WriteString("subname"u8, card.Subname);
                writer.WriteString("type"u8, card.Kind);

                writer.WriteStartArray("traits"u8);
                foreach (string trait in card.Traits)
                {
                    writer.WriteStringValue(trait);
                }

                writer.WriteEndArray();

                writer.WriteStartObject("attributes"u8);
                foreach (var (key, value) in card.Attributes)
                {
                    writer.WriteString(key, value);
                }

                writer.WriteEndObject();

                if (card.LinkedTo.Count > 0)
                {
                    writer.WriteStartArray("linked_to"u8);
                    foreach (string faceId in card.LinkedTo)
                    {
                        writer.WriteStringValue(faceId);
                    }
                    writer.WriteEndArray();
                }

                writer.WriteString("text"u8, card.Text);
                writer.WriteString("text_plain"u8, card.Plain);
                writer.WriteString("pack"u8, card.Pack);
                writer.WriteString("set"u8, card.Set);
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        // **Line feeds, on every platform.** `Utf8JsonWriter` indents with
        // `Environment.NewLine`, so the same inputs give different bytes on
        // Windows and Linux -- and `-- check` compares bytes. `.gitattributes`
        // already normalises the working tree to LF for exactly this reason;
        // this is the other half, because what the generator emits has to
        // match what the repository holds rather than what the machine
        // prefers. `JsonWriterOptions.NewLine` would say it in one line and
        // arrived in .NET 9; this targets `net8.0` (Directory.Build.props).
        return Encoding.UTF8.GetString(buffer.WrittenSpan)
            .Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
    }

    /// <summary>Reads a dataset in either the old shape or the new one.</summary>
    /// <remarks>
    /// The old one nested the engine-facing half under <c>engine</c>. Reading
    /// both is what lets <c>diff</c> hold the generator against the dataset it
    /// replaces; nothing else needs it.
    /// </remarks>
    /// <param name="json">The dataset text.</param>
    public static Dictionary<string, Card> Read(string json)
    {
        var found = new Dictionary<string, Card>(StringComparer.Ordinal);
        using var document = JsonDocument.Parse(json);

        foreach (var element in document.RootElement.GetProperty("cards").EnumerateArray())
        {
            if (ReadCard(element) is { } card) found[card.Id] = card;
        }

        return found;
    }

    private static Card? ReadCard(JsonElement element)
    {
        string id = element.GetProperty("card_id").GetString()!;
        bool nested = element.TryGetProperty("engine", out var engine)
            && engine.ValueKind == JsonValueKind.Object;
        // The old dataset carried an `engine` block only for engine cards.
        if (!nested && element.TryGetProperty("engine", out _)) return null;
        var facts = nested ? engine : element;
        if (!nested && !element.TryGetProperty("attributes", out _)) return null;
        return new Card(
            id, Field(element, "name"), Field(element, "subname"),
            facts.TryGetProperty("type", out var kind) ? kind.GetString() ?? "" : "",
            StringList(facts, "traits"), Attributes(facts),
            StringList(facts, "linked_to"), Field(element, "text"),
            Field(element, "pack"), Field(element, "set"));
    }

    private static SortedDictionary<string, string> Attributes(JsonElement facts)
    {
        var attributes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        if (!facts.TryGetProperty("attributes", out var printed)
            || printed.ValueKind != JsonValueKind.Object) return attributes;
        foreach (var attribute in printed.EnumerateObject())
            attributes[attribute.Name] = attribute.Value.GetString() ?? "";
        return attributes;
    }

    private static List<string> StringList(JsonElement facts, string name)
    {
        if (!facts.TryGetProperty(name, out var values)
            || values.ValueKind != JsonValueKind.Array) return [];
        return [.. values.EnumerateArray().Select(value => value.GetString()!)];
    }

    /// <summary>Prints what changed between two datasets, and how much.</summary>
    /// <param name="was">The committed dataset.</param>
    /// <param name="now">What the generator produces.</param>
    public static int Report(
        IReadOnlyDictionary<string, Card> was, IReadOnlyDictionary<string, Card> now)
    {
        var lines = new List<string>();
        var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);

        void Note(string kind, string line)
        {
            counts[kind] = counts.GetValueOrDefault(kind) + 1;
            lines.Add($"{kind,-22} {line}");
        }

        foreach (string id in was.Keys.Concat(now.Keys)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal))
        {
            if (!now.TryGetValue(id, out var built)) { Note("card dropped", id); continue; }
            if (!was.TryGetValue(id, out var old)) { Note("card added", id); continue; }

            if (!string.Equals(old.Kind, built.Kind, StringComparison.Ordinal)
                && old.Kind.Length > 0)
            {
                Note("type", $"{id} {old.Kind} -> {built.Kind}");
            }

            if (!old.Traits.SequenceEqual(built.Traits, StringComparer.Ordinal))
            {
                Note("traits", $"{id} [{string.Join(' ', old.Traits)}] -> [{string.Join(' ', built.Traits)}]");
            }
            if (!old.LinkedTo.SequenceEqual(built.LinkedTo, StringComparer.Ordinal))
            {
                Note(
                    "linked faces",
                    $"{id} [{string.Join(' ', old.LinkedTo)}] -> [{string.Join(' ', built.LinkedTo)}]");
            }

            foreach (string key in old.Attributes.Keys
                .Concat(built.Attributes.Keys)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(key => key, StringComparer.Ordinal))
            {
                old.Attributes.TryGetValue(key, out string? before);
                built.Attributes.TryGetValue(key, out string? after);
                if (!string.Equals(before, after, StringComparison.Ordinal))
                {
                    Note($"attr {key}", $"{id} {before ?? "-"} -> {after ?? "-"}");
                }
            }
        }

        foreach (string line in lines)
        {
            Console.WriteLine(line);
        }

        Console.Error.WriteLine();
        foreach (var (kind, count) in counts.OrderByDescending(pair => pair.Value))
        {
            Console.Error.WriteLine(
                $"{count,6}  {kind}".ToString(CultureInfo.InvariantCulture));
        }

        Console.Error.WriteLine($"{lines.Count,6}  differences in total");
        return 0;
    }

    private static string Field(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? ""
            : "";
}
