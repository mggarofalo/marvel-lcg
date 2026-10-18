using System.Text.Json;

namespace Marvel.Client;

/// <summary>Atomic local draft files, independent of engine persistence and replay.</summary>
public sealed class LocalDeckStore(string directory)
{
    private readonly string root = Path.GetFullPath(directory);

    /// <summary>Reads saved drafts; a corrupt file is reported rather than overwritten.</summary>
    public IReadOnlyList<SavedDeck> List()
    {
        if (!Directory.Exists(root)) return [];
        return Directory.EnumerateFiles(root, "*.deck.json").Order(StringComparer.Ordinal)
            .Select(path => Read(Path.GetFileName(path)[..^10])).ToArray();
    }

    /// <summary>Reads one bounded snapshot, preserving exact ordered copies.</summary>
    public SavedDeck Read(string id)
    {
        string path = PathFor(id);
        if (new FileInfo(path).Length > MarvelCdbDeckJson.MaximumBytes)
            throw new InvalidDataException("The saved deck exceeds 1 MiB.");
        using var document = JsonDocument.Parse(File.ReadAllBytes(path), new JsonDocumentOptions { MaxDepth = 16 });
        var item = document.RootElement;
        if (item.GetProperty("schema").GetInt32() != 1) throw new InvalidDataException("Unsupported deck library format.");
        int revision = item.GetProperty("revision").GetInt32();
        if (revision < 1) throw new InvalidDataException("Invalid deck revision.");
        string[] cards = item.GetProperty("cards").EnumerateArray().Select(value => value.GetString()
            ?? throw new InvalidDataException("Missing printed card code.")).ToArray();
        var draft = new DeckDraft(item.GetProperty("name").GetString()!, item.GetProperty("hero_code").GetString()!, cards);
        Validate(draft);
        return new(id, revision, draft);
    }

    /// <summary>Saves a draft, including invalid construction, only if its loaded revision is current.</summary>
    public SavedDeck Save(DeckDraft draft, string? id = null, int expectedRevision = 0)
    {
        Validate(draft);
        id ??= Guid.NewGuid().ToString("N");
        string path = PathFor(id);
        Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, ".library.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        int current = File.Exists(path) ? Read(id).Revision : 0;
        if (current != expectedRevision) throw new IOException("This deck changed on disk. Reload it or save a new copy.");
        int revision = checked(current + 1);
        string temporary = Path.Combine(root, Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("schema", 1);
                    writer.WriteNumber("revision", revision);
                    writer.WriteString("name", draft.Name);
                    writer.WriteString("hero_code", draft.HeroCode);
                    writer.WriteStartArray("cards");
                    foreach (string card in draft.Cards) writer.WriteStringValue(card);
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }
                stream.Flush(flushToDisk: true);
            }
            File.Move(temporary, path, overwrite: true);
            return new(id, revision, draft);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    /// <summary>Deletes only the revision the caller has inspected.</summary>
    public void Delete(string id, int expectedRevision)
    {
        string path = PathFor(id);
        using var lease = new FileStream(Path.Combine(root, ".library.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        if (Read(id).Revision != expectedRevision) throw new IOException("This deck changed on disk. Reload before deleting.");
        File.Delete(path);
    }

    private string PathFor(string id)
    {
        if (!Guid.TryParseExact(id, "N", out _)) throw new ArgumentException("Invalid library deck id.", nameof(id));
        return Path.Combine(root, id + ".deck.json");
    }

    private static void Validate(DeckDraft draft)
    {
        // These are storage/input bounds, not construction legality.
        if (string.IsNullOrWhiteSpace(draft.Name) || draft.Name.Length > 256
            || string.IsNullOrWhiteSpace(draft.HeroCode) || draft.HeroCode.Length > 80)
            throw new InvalidDataException("Give the deck a name and an identity code within the supported bounds.");
        if (draft.Cards.Length > MarvelCdbDeckJson.MaximumCards
            || draft.Cards.Any(face => string.IsNullOrWhiteSpace(face) || face.Length > 80))
            throw new InvalidDataException("The draft exceeds the supported storage bounds.");
    }
}
