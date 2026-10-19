using System.Text.Json;

namespace Marvel.Rules.Index;

/// <summary>Loads Rules Reference and product rules without conflating their authority tiers.</summary>
internal static class CorpusReader
{
    /// <summary>Reads pinned rule indexes and validates their authored ruling relationships.</summary>
    internal static Corpus Read(string indexPath, string graphPath, string rulingsPath, string packsPath)
    {
        var found = new Dictionary<string, RuleRecord>(StringComparer.Ordinal);
        using var index = JsonDocument.Parse(File.ReadAllBytes(indexPath));
        var root = index.RootElement;
        ReadBase(root, found, "base");
        using var packs = JsonDocument.Parse(File.ReadAllBytes(packsPath));
        ReadBase(packs.RootElement, found, "pack");
        using var graph = JsonDocument.Parse(File.ReadAllBytes(graphPath));
        if (graph.RootElement.GetProperty("version").GetInt32() != 2)
            throw new InvalidDataException("rules-graph.json is not relationship schema version 2");
        var authored = ReadEdges(graph.RootElement);
        using var rulings = JsonDocument.Parse(File.ReadAllBytes(rulingsPath));
        var published = ReadPublished(rulings.RootElement);
        var modifications = ReadModifications(
            graph.RootElement, published, found);
        return new Corpus(found, authored, modifications)
        {
            Version = root.TryGetProperty("version", out var version)
                ? version.GetString() ?? "unknown" : "unknown",
        };
    }

    private static void ReadBase(
        JsonElement root, Dictionary<string, RuleRecord> found, string kind)
    {
        var entries = root.GetProperty("entries");
        var clauses = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var entry in entries.EnumerateArray())
        {
            string owner = Corpus.EntryOf(entry.GetProperty("id").GetString()!);
            clauses[owner] = clauses.GetValueOrDefault(owner) + 1;
        }
        foreach (var entry in entries.EnumerateArray())
        {
            string id = entry.GetProperty("id").GetString()!;
            found[id] = new RuleRecord(
                id, Optional(entry, "title"), Optional(entry, "fragment"),
                Optional(entry, "hash"), id == Corpus.EntryOf(id) ? clauses[id] : 0,
                kind, null);
        }
    }

    private static string Optional(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) ? value.GetString() ?? "" : "";

    private static List<Edge> ReadEdges(JsonElement graph)
    {
        var result = new List<Edge>();
        foreach (var from in graph.GetProperty("edges").EnumerateObject())
        {
            string why = Optional(from.Value, "why");
            foreach (var to in from.Value.GetProperty("references").EnumerateArray())
                result.Add(new Edge(from.Name, to.GetString()!, why));
        }
        return result;
    }

    private static Dictionary<string, JsonElement> ReadPublished(JsonElement root)
    {
        var published = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var ruling in root.GetProperty("rulings").EnumerateArray())
        {
            string rulingId = ruling.GetProperty("id").GetString()!;
            if (!published.TryAdd(rulingId, ruling.Clone()))
                throw new InvalidDataException($"rulings.json contains duplicate id {rulingId}");
        }
        return published;
    }

    private static List<Modification> ReadModifications(
        JsonElement graph, Dictionary<string, JsonElement> published,
        Dictionary<string, RuleRecord> found)
    {
        var modifications = new List<Modification>();
        foreach (var mapped in graph.GetProperty("modifications").EnumerateObject())
        {
            var modification = ReadModification(mapped, published, found);
            modifications.Add(modification);
            var ruling = published[mapped.Name];
            var baseRecord = found[modification.BaseId];
            found.Add(mapped.Name, new RuleRecord(
                mapped.Name,
                $"RULING — {baseRecord.Title}",
                ruling.GetProperty("answer").GetString()!,
                modification.Hash,
                0,
                "modification",
                modification.BaseId));
        }
        return modifications;
    }

    private static Modification ReadModification(
        JsonProperty mapped,
        Dictionary<string, JsonElement> published,
        Dictionary<string, RuleRecord> found)
    {
        if (!published.TryGetValue(mapped.Name, out var ruling))
            throw new InvalidDataException(
                $"rules modification {mapped.Name} has no published ruling");
        RequireRulesRuling(mapped.Name, ruling);
        string baseId = mapped.Value.GetProperty("base").GetString()!;
        if (!found.TryGetValue(baseId, out var baseRecord))
            throw new InvalidDataException(
                $"rules modification {mapped.Name} names no base rule {baseId}");
        string supersedes = mapped.Value.GetProperty("supersedes_hash").GetString()!;
        RequireHash(mapped.Name, baseId, supersedes, baseRecord.Hash);
        string rulingHash = ruling.GetProperty("hash").GetString()!;
        string expected = mapped.Value.GetProperty("ruling_hash").GetString()!;
        RequireRulingHash(mapped.Name, expected, rulingHash);
        string why = mapped.Value.GetProperty("why").GetString()!;
        string source = ruling.GetProperty("source").GetString()!;
        string via = ruling.GetProperty("via").GetString()!;
        string scope = ruling.GetProperty("rrg_scope").GetString()!;
        RequireProvenance(mapped.Name, why, source, via, scope);
        string? absorbed = mapped.Value.GetProperty("absorbed_in").GetString();
        RequireAbsorption(mapped.Name, absorbed, scope);
        return new Modification(
            mapped.Name, baseId, supersedes, absorbed, why, source, via, scope,
            ruling.GetProperty("observed").GetString(), rulingHash);
    }

    private static void RequireRulesRuling(string id, JsonElement ruling)
    {
        if (!string.Equals(
            ruling.GetProperty("kind").GetString(), "rules", StringComparison.Ordinal))
            throw new InvalidDataException(
                $"rules modification {id} is a card ruling, not a rules ruling");
    }

    private static void RequireHash(
        string id, string baseId, string pinned, string actual)
    {
        if (!string.Equals(pinned, actual, StringComparison.Ordinal))
            throw new InvalidDataException(
                $"rules modification {id} pins {pinned}, not {actual} for {baseId}");
    }

    private static void RequireRulingHash(
        string id, string pinned, string actual)
    {
        if (!string.Equals(pinned, actual, StringComparison.Ordinal))
            throw new InvalidDataException(
                $"rules modification {id} pins ruling {pinned}, not {actual}");
    }

    private static void RequireProvenance(
        string id, string why, string source, string via, string scope)
    {
        if (string.IsNullOrWhiteSpace(why) || string.IsNullOrWhiteSpace(source)
            || string.IsNullOrWhiteSpace(via) || string.IsNullOrWhiteSpace(scope))
            throw new InvalidDataException(
                $"rules modification {id} has incomplete provenance");
    }

    private static void RequireAbsorption(
        string id, string? absorbed, string scope)
    {
        if (absorbed is not null
            && Corpus.CompareVersions(absorbed, Corpus.EffectiveVersion(scope)) < 0)
            throw new InvalidDataException(
                $"rules modification {id} is absorbed before its RRG scope");
    }

}
