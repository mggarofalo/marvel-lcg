// `RepositoryPaths` is linked in from `tests/Shared/`. It answers "where is
// this repository" and not "where is this test", and one copy of that answer
// is the reason it lives in one file.
using Marvel.Tests;

namespace Marvel.Rules.Index;

/// <summary>
/// The vendored Rules Reference and product indexes, and their authored relationships.
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
    private readonly Dictionary<string, RuleRecord> records;
    private readonly List<Edge> edges;
    private readonly List<Modification> modifications;

    internal Corpus(
        Dictionary<string, RuleRecord> records,
        List<Edge> edges,
        List<Modification> modifications)
    {
        this.records = records;
        this.edges = edges;
        this.modifications = modifications;
    }

    /// <summary>The Rules Reference version the index was harvested from.</summary>
    public string Version { get; internal init; } = "unknown";

    /// <summary>Every citable record, in the document's order.</summary>
    public IReadOnlyCollection<RuleRecord> Records => records.Values;

    /// <summary>Every authored edge, in the order the dataset lists them.</summary>
    public IReadOnlyList<Edge> Edges => edges;

    /// <summary>Every audited ruling-to-base relationship.</summary>
    public IReadOnlyList<Modification> Modifications => modifications;

    /// <summary>Reads the base index, relationship graph, and rulings from the repository.</summary>
    public static Corpus Read() => Read(
        RepositoryPaths.Dataset("rules-reference", "index.json"),
        RepositoryPaths.Dataset("rules-graph.json"),
        RepositoryPaths.Dataset("rulings", "rulings.json"));

    /// <summary>Reads explicit RR, graph and ruling inputs alongside the pinned product-rule index.</summary>
    internal static Corpus Read(string indexPath, string graphPath, string rulingsPath) =>
        CorpusReader.Read(indexPath, graphPath, rulingsPath,
            RepositoryPaths.Dataset("rules-packs", "index.json"));

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
    public RuleRecord? Find(string id) => records.TryGetValue(id, out var record) ? record : null;

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
    public RuleRecord Resolve(string id, string version)
    {
        if (!string.Equals(version, Version, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Rules Reference v{Version} is the only vendored base; cannot resolve v{version}");
        }

        if (!records.TryGetValue(id, out var current) || current.Kind is not ("base" or "pack"))
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
        var unabsorbed = currentModifications
            .Where(modification => modification.AbsorbedIn is null
                || CompareVersions(version, modification.AbsorbedIn) < 0)
            .ToList();
        if (unabsorbed.Count > 1)
        {
            throw new InvalidDataException(
                $"{baseId} has {unabsorbed.Count} current modifications from RRG {latest}");
        }

        return unabsorbed.Count == 0 ? null : unabsorbed[0].Id;
    }

    internal static string EffectiveVersion(string scope) => scope switch
    {
        "pre-1.5" => "0.0",
        "1.7-1.8" => "1.7",
        _ => scope,
    };

    internal static int CompareVersions(string left, string right) =>
        System.Version.Parse(left).CompareTo(System.Version.Parse(right));
}
