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
internal readonly record struct Record(string Id, string Title, string Fragment, int Clauses);

/// <summary>One authored edge of the rule reference graph.</summary>
/// <param name="From">The rule that names another.</param>
/// <param name="To">What it names.</param>
/// <param name="Why">Why the edge is there, as the dataset records it.</param>
internal readonly record struct Edge(string From, string To, string Why);

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

    private Corpus(Dictionary<string, Record> records, List<Edge> edges)
    {
        this.records = records;
        this.edges = edges;
    }

    /// <summary>The Rules Reference version the index was harvested from.</summary>
    public string Version { get; private set; } = "unknown";

    /// <summary>Every citable record, in the document's order.</summary>
    public IReadOnlyCollection<Record> Records => records.Values;

    /// <summary>Every authored edge, in the order the dataset lists them.</summary>
    public IReadOnlyList<Edge> Edges => edges;

    /// <summary>Reads both datasets from the repository.</summary>
    public static Corpus Read()
    {
        var found = new Dictionary<string, Record>(StringComparer.Ordinal);

        using var index = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Dataset("rules-reference", "index.json")));
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
                id == EntryOf(id) ? clauses[id] : 0);
        }

        var authored = new List<Edge>();
        using var graph = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Dataset("rules-graph.json")));
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

        return new Corpus(found, authored)
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
        [.. edges.Where(edge => string.Equals(edge.From, id, StringComparison.Ordinal))];

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
    ];
}
