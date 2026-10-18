using System.Text.Json;

namespace Marvel.Client;

/// <summary>Bounded MarvelCDB interchange; construction legality remains with the host.</summary>
public static class MarvelCdbDeckJson
{
    /// <summary>The largest imported document, including unused metadata.</summary>
    public const int MaximumBytes = 1024 * 1024;
    /// <summary>The largest editable draft accepted by this product.</summary>
    public const int MaximumCards = 500;

    /// <summary>Reads exact codes and integer quantities without title matching or dropping cards.</summary>
    public static DeckDraft Read(ReadOnlyMemory<byte> json)
    {
        if (json.Length > MaximumBytes) throw new InvalidDataException("The deck file exceeds 1 MiB.");
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 16 });
        JsonElement root = document.RootElement;
        RequireObject(root);
        string hero = RequiredText(root, "hero_code", 80);
        string name = root.TryGetProperty("name", out _) ? RequiredText(root, "name", 256) : "Imported deck";
        if (!root.TryGetProperty("slots", out var slots)) throw new InvalidDataException("The deck has no slots object.");
        RequireObject(slots);
        RejectSpecialSlots(root);
        return new(name, hero, ReadSlots(slots));
    }

    private static List<string> ReadSlots(JsonElement slots)
    {
        var cards = new List<string>();
        // Ordering is our import contract: equivalent object property orders
        // create the same captured recipe and therefore the same seeded game.
        foreach (var slot in slots.EnumerateObject().OrderBy(item => item.Name, StringComparer.Ordinal))
        {
            if (string.IsNullOrWhiteSpace(slot.Name) || slot.Name.Length > 80 || slot.Name.Contains(',', StringComparison.Ordinal))
                throw new InvalidDataException("Each slot must name one exact printed face.");
            if (slot.Value.ValueKind != JsonValueKind.Number || !slot.Value.TryGetInt32(out int quantity)
                || quantity < 1 || quantity > MaximumCards - cards.Count)
                throw new InvalidDataException("Slot quantities must be positive whole numbers totaling at most 500 cards.");
            cards.AddRange(Enumerable.Repeat(slot.Name, quantity));
        }
        return cards;
    }

    /// <summary>Exports the exact draft for JSON interchange, including invalid construction.</summary>
    public static byte[] Write(DeckDraft draft)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
        {
            writer.WriteStartObject();
            writer.WriteString("name", draft.Name);
            writer.WriteString("hero_code", draft.HeroCode);
            writer.WriteStartObject("slots");
            foreach (var group in draft.Cards.GroupBy(face => face, StringComparer.Ordinal).OrderBy(group => group.Key, StringComparer.Ordinal))
                writer.WriteNumber(group.Key, group.Count());
            writer.WriteEndObject();
            writer.WriteEndObject();
        }
        return stream.ToArray();
    }

    private static void RequireObject(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Expected a deck JSON object.");
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
            if (!names.Add(property.Name)) throw new InvalidDataException($"Duplicate JSON property '{property.Name}'.");
    }

    private static string RequiredText(JsonElement root, string property, int maximum)
    {
        if (!root.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String
            || value.GetString() is not { } text || string.IsNullOrWhiteSpace(text) || text.Length > maximum)
            throw new InvalidDataException($"The deck requires a nonempty '{property}' of at most {maximum} characters.");
        return text;
    }

    private static void RejectSpecialSlots(JsonElement root)
    {
        if (!root.TryGetProperty("ignoreDeckLimitSlots", out var special) || special.ValueKind == JsonValueKind.Null) return;
        if (special.ValueKind == JsonValueKind.Object && !special.EnumerateObject().Any()) return;
        throw new InvalidDataException("This deck uses additional deck-limit slots that the Core importer does not support.");
    }
}
