using System.Text.Json;

// `RepositoryPaths` is linked in from `tests/Shared/`. It answers "where is
// this repository" and not "where is this test", and one copy of that answer
// is the reason it lives in one file.
using Marvel.Tests;

namespace Marvel.Rules.Index;

/// <summary>One citable unit of the Rules Reference.</summary>
/// <param name="Id">Its citation id — <c>rr:forced.4</c>.</param>
/// <param name="Title">The entry it belongs to, in the document's own casing.</param>
/// <param name="Fragment">The clause, as the index records it for legibility.</param>
/// <param name="Clauses">
/// How many citable records the entry holds, counting itself. Zero on anything
/// that is not an entry.
/// </param>
internal readonly record struct Record(
    string Id,
    string Title,
    string Fragment,
    string Hash,
    int Clauses,
    string Kind,
    string? BaseId);

/// <summary>One authored edge of the rule reference graph.</summary>
/// <param name="From">The rule that names another.</param>
/// <param name="To">What it names.</param>
/// <param name="Why">Why the edge is there, as the dataset records it.</param>
internal readonly record struct Edge(string From, string To, string Why);

/// <summary>A published ruling layered over one citable Rules Reference record.</summary>
internal readonly record struct Modification(
    string Id,
    string BaseId,
    string SupersedesHash,
    string? AbsorbedIn,
    string Why,
    string Source,
    string Via,
    string Scope,
    string? Observed,
    string Hash);

/// <summary>
/// The vendored Rules Reference index, and the authored graph over it.
/// </summary>
/// <remarks>
/// <para>
/// Two datasets and one reason to read them together.
/// <c>datasets/rules-reference/index.json</c> is harvested and carries no
/// relationships: an entry knows its own text and nothing about which other
/// rule it qualifies. <c>datasets/rules-graph.json</c> is hand-authored and
/// carries only relationships. Neither is useful alone.
/// </para>
/// <para>
/// <b>The graph is one-way by construction.</b> Its own note says so — "an
/// exception names the rule it overrides or extends; a base rule names
/// nothing" — so the interesting query is the reverse one, and the reverse is
/// computed here rather than stored. A stored reverse edge is a second place
/// for the same fact to be wrong.
/// </para>
/// </remarks>
internal sealed class Corpus
{
    private readonly Dictionary<string, Record> records;
    private readonly List<Edge> edges;
    private readonly List<Modification> modifications;

    private Corpus(
        Dictionary<string, Record> records,
        List<Edge> edges,
        List<Modification> modifications)
    {
        this.records = records;
        this.edges = edges;
        this.modifications = modifications;
    }

    /// <summary>The Rules Reference version the index was harvested from.</summary>
    public string Version { get; private set; } = "unknown";

    /// <summary>Every citable record, in the document's order.</summary>
    public IReadOnlyCollection<Record> Records => records.Values;

    /// <summary>Every authored edge, in the order the dataset lists them.</summary>
    public IReadOnlyList<Edge> Edges => edges;

    /// <summary>Every audited ruling-to-base relationship.</summary>
    public IReadOnlyList<Modification> Modifications => modifications;

    /// <summary>Reads the base index, relationship graph, and rulings from the repository.</summary>
    public static Corpus Read() => Read(
        RepositoryPaths.Dataset("rules-reference", "index.json"),
        RepositoryPaths.Dataset("rules-graph.json"),
        RepositoryPaths.Dataset("rulings", "rulings.json"));

    /// <summary>Reads the three corpus inputs from explicit paths.</summary>
    internal static Corpus Read(string indexPath, string graphPath, string rulingsPath)
    {
        var found = new Dictionary<string, Record>(StringComparer.Ordinal);

        using var index = JsonDocument.Parse(File.ReadAllBytes(indexPath));
        var root = index.RootElement;

        // An entry's own id is the prefix every one of its clauses shares, so
        // the clause count is a group-by rather than a field. It is what
        // `citations --sort` orders on: a rough proxy for how much engine
        // surface an entry touches, and the index carries nothing better.
        var clauses = new Dictionary<string, int>(StringComparer.Ordinal);
        var entries = root.GetProperty("entries");
        foreach (var entry in entries.EnumerateArray())
        {
            string id = entry.GetProperty("id").GetString()!;
            string owner = EntryOf(id);
            clauses[owner] = clauses.GetValueOrDefault(owner) + 1;
        }

        foreach (var entry in entries.EnumerateArray())
        {
            string id = entry.GetProperty("id").GetString()!;
            found[id] = new Record(
                id,
                entry.TryGetProperty("title", out var title) ? title.GetString() ?? "" : "",
                entry.TryGetProperty("fragment", out var text) ? text.GetString() ?? "" : "",
                entry.TryGetProperty("hash", out var hash) ? hash.GetString() ?? "" : "",
                id == EntryOf(id) ? clauses[id] : 0,
                "base",
                null);
        }

        var authored = new List<Edge>();
        using var graph = JsonDocument.Parse(File.ReadAllBytes(graphPath));
        if (graph.RootElement.GetProperty("version").GetInt32() != 2)
        {
            throw new InvalidDataException("rules-graph.json is not relationship schema version 2");
        }

        foreach (var from in graph.RootElement.GetProperty("edges").EnumerateObject())
        {
            string why = from.Value.TryGetProperty("why", out var reason)
                ? reason.GetString() ?? ""
                : "";
            foreach (var to in from.Value.GetProperty("references").EnumerateArray())
            {
                authored.Add(new Edge(from.Name, to.GetString()!, why));
            }
        }

        using var rulings = JsonDocument.Parse(File.ReadAllBytes(rulingsPath));
        var published = rulings.RootElement.GetProperty("rulings").EnumerateArray()
            .ToDictionary(
                ruling => ruling.GetProperty("id").GetString()!,
                ruling => ruling.Clone(),
                StringComparer.Ordinal);
        var modifications = new List<Modification>();
        foreach (var mapped in graph.RootElement.GetProperty("modifications").EnumerateObject())
        {
            if (!published.TryGetValue(mapped.Name, out var ruling))
            {
                throw new InvalidDataException($"rules modification {mapped.Name} has no published ruling");
            }

            if (!string.Equals(
                ruling.GetProperty("kind").GetString(),
                "rules",
                StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"rules modification {mapped.Name} is a card ruling, not a rules ruling");
            }

            string baseId = mapped.Value.GetProperty("base").GetString()!;
            if (!found.TryGetValue(baseId, out var baseRecord))
            {
                throw new InvalidDataException($"rules modification {mapped.Name} names no base rule {baseId}");
            }

            string supersedesHash = mapped.Value.GetProperty("supersedes_hash").GetString()!;
            if (!string.Equals(supersedesHash, baseRecord.Hash, StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"rules modification {mapped.Name} pins {supersedesHash}, not {baseRecord.Hash} for {baseId}");
            }

            string why = mapped.Value.GetProperty("why").GetString()!;
            string source = ruling.GetProperty("source").GetString()!;
            string via = ruling.GetProperty("via").GetString()!;
            string scope = ruling.GetProperty("rrg_scope").GetString()!;
            string? absorbedIn = mapped.Value.GetProperty("absorbed_in").GetString();
            if (string.IsNullOrWhiteSpace(why)
                || string.IsNullOrWhiteSpace(source)
                || string.IsNullOrWhiteSpace(via)
                || string.IsNullOrWhiteSpace(scope))
            {
                throw new InvalidDataException(
                    $"rules modification {mapped.Name} has incomplete provenance");
            }

            if (absorbedIn is not null
                && CompareVersions(absorbedIn, EffectiveVersion(scope)) < 0)
            {
                throw new InvalidDataException(
                    $"rules modification {mapped.Name} is absorbed before its RRG scope");
            }

            var modification = new Modification(
                mapped.Name,
                baseId,
                supersedesHash,
                absorbedIn,
                why,
                source,
                via,
                scope,
                ruling.GetProperty("observed").GetString(),
                ruling.GetProperty("hash").GetString()!);
            modifications.Add(modification);
            found.Add(mapped.Name, new Record(
                mapped.Name,
                $"RULING — {baseRecord.Title}",
                ruling.GetProperty("answer").GetString()!,
                modification.Hash,
                0,
                "modification",
                baseId));
        }

        return new Corpus(found, authored, modifications)
        {
            Version = root.TryGetProperty("version", out var version)
                ? version.GetString() ?? "unknown"
                : "unknown",
        };
    }

    /// <summary>The entry a citation belongs to.</summary>
    /// <remarks>
    /// Everything after the first dot is the clause, the step or the sub-step —
    /// <c>rr:ability.step.2.a</c> belongs to <c>rr:ability</c>. The scheme's own
    /// colon is not a separator.
    /// </remarks>
    /// <param name="id">A citation id.</param>
    public static string EntryOf(string id)
    {
        int dot = id.IndexOf('.', StringComparison.Ordinal);
        return dot < 0 ? id : id[..dot];
    }

    /// <summary>Whether an id names something citable.</summary>
    /// <param name="id">A citation id.</param>
    public bool Knows(string id) => records.ContainsKey(id);

    /// <summary>One record, or null.</summary>
    /// <param name="id">A citation id.</param>
    public Record? Find(string id) => records.TryGetValue(id, out var record) ? record : null;

    /// <summary>What a rule names.</summary>
    /// <param name="id">A citation id.</param>
    public IReadOnlyList<Edge> References(string id) =>
    [
        .. edges.Where(edge => string.Equals(edge.From, id, StringComparison.Ordinal)),
        .. modifications
            .Where(modification => string.Equals(modification.Id, id, StringComparison.Ordinal))
            .Select(modification => new Edge(
                modification.Id,
                modification.BaseId,
                modification.Why)),
    ];

    /// <summary>
    /// What names a rule — the query the graph exists for, computed rather than
    /// stored.
    /// </summary>
    /// <remarks>
    /// An id is matched by its entry as well as itself, because an edge points
    /// at the grain the author argued from: three edges name <c>rr:thwart.1</c>
    /// and asking about <c>rr:thwart</c> should find them. The reverse is not
    /// true — asking about the clause does not find edges pointing at the whole
    /// entry, which would answer a narrower question with a broader rule.
    /// </remarks>
    /// <param name="id">A citation id.</param>
    public IReadOnlyList<Edge> ReferencedBy(string id) =>
    [
        .. edges.Where(edge =>
            string.Equals(edge.To, id, StringComparison.Ordinal)
            || string.Equals(EntryOf(edge.To), id, StringComparison.Ordinal)),
        .. modifications
            .Where(modification =>
                string.Equals(modification.BaseId, id, StringComparison.Ordinal)
                || string.Equals(EntryOf(modification.BaseId), id, StringComparison.Ordinal))
            .Select(modification => new Edge(
                modification.Id,
                modification.BaseId,
                modification.Why)),
    ];

    /// <summary>The one text current for a base record in the vendored RR version.</summary>
    public Record Resolve(string id, string version)
    {
        if (!string.Equals(version, Version, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Rules Reference v{Version} is the only vendored base; cannot resolve v{version}");
        }

        if (!records.TryGetValue(id, out var current) || current.Kind != "base")
        {
            throw new KeyNotFoundException($"Rules Reference v{Version} has no base rule '{id}'");
        }

        string? selected = SelectCurrent(
            modifications.Where(modification => string.Equals(
                modification.BaseId,
                id,
                StringComparison.Ordinal)),
            version,
            id);
        return selected is null ? current : records[selected];
    }

    /// <summary>Selects one overlay by RRG order, or the base when it was absorbed.</summary>
    internal static string? SelectCurrent(
        IEnumerable<Modification> candidates,
        string version,
        string baseId)
    {
        var applicable = candidates
            .Where(modification => CompareVersions(EffectiveVersion(modification.Scope), version) <= 0)
            .ToList();
        if (applicable.Count == 0)
        {
            return null;
        }

        string latest = applicable
            .Select(modification => EffectiveVersion(modification.Scope))
            .MaxBy(value => System.Version.Parse(value))!;
        var currentModifications = applicable
            .Where(modification => string.Equals(
                EffectiveVersion(modification.Scope),
                latest,
                StringComparison.Ordinal))
            .ToList();
        if (currentModifications.Count > 1)
        {
            throw new InvalidDataException(
                $"{baseId} has {currentModifications.Count} current modifications from RRG {latest}");
        }

        Modification selected = currentModifications[0];
        return selected.AbsorbedIn is not null
            && CompareVersions(version, selected.AbsorbedIn) >= 0
            ? null
            : selected.Id;
    }

    private static string EffectiveVersion(string scope) => scope switch
    {
        "pre-1.5" => "0.0",
        "1.7-1.8" => "1.7",
        _ => scope,
    };

    private static int CompareVersions(string left, string right) =>
        System.Version.Parse(left).CompareTo(System.Version.Parse(right));
}
