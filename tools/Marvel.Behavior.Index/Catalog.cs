using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Tests;

namespace Marvel.Behavior.Index;

internal sealed record AdjudicationFile(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("sources")] IReadOnlyList<Adjudication> Sources);

internal sealed record Adjudication(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("fingerprint")] string Fingerprint,
    [property: JsonPropertyName("obligations")] IReadOnlyList<Obligation> Obligations);

internal sealed record Obligation(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("disposition")] string Disposition,
    [property: JsonPropertyName("reason")] string? Reason,
    [property: JsonPropertyName("target")] string? Target,
    [property: JsonPropertyName("implementation")] string? Implementation,
    [property: JsonPropertyName("work_item")] string? WorkItem,
    [property: JsonPropertyName("exception")] string? Exception,
    [property: JsonPropertyName("scenarios")] IReadOnlyList<string> Scenarios,
    [property: JsonPropertyName("mutation")] string? Mutation);

internal sealed record CatalogFile(
    [property: JsonPropertyName("version")] int Version,
    [property: JsonPropertyName("contract")] string Contract,
    [property: JsonPropertyName("sources")] IReadOnlyList<CatalogEntry> Sources);

internal sealed record CatalogEntry(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("fingerprint")] string Fingerprint,
    [property: JsonPropertyName("scope")] string Scope,
    [property: JsonPropertyName("disposition")] string Disposition,
    [property: JsonPropertyName("obligations")] IReadOnlyList<CatalogObligation> Obligations);

internal sealed record CatalogObligation(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("disposition")] string Disposition,
    [property: JsonPropertyName("reason")] string? Reason,
    [property: JsonPropertyName("target")] string? Target,
    [property: JsonPropertyName("implementation")] string? Implementation,
    [property: JsonPropertyName("work_item")] string? WorkItem,
    [property: JsonPropertyName("exception")] string? Exception,
    [property: JsonPropertyName("scenarios")] IReadOnlyList<string> Scenarios,
    [property: JsonPropertyName("mutation")] string? Mutation);

/// <summary>Joins canonical sources to reviewed obligation adjudications.</summary>
internal static class Catalog
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = null,
        WriteIndented = true,
    };

    private static readonly HashSet<string> Dispositions = new(StringComparer.Ordinal)
    {
        "executable",
        "narrower",
        "no-independent-behavior",
        "not-representable",
        "outside-core",
        "superseded",
    };

    private static readonly HashSet<string> Implementations = new(StringComparer.Ordinal)
    {
        "unverified",
        "supported",
        "unimplemented",
    };

    public static string AdjudicationsPath { get; } =
        RepositoryPaths.Repository("specs", "behavior", "adjudications.json");

    public static string CatalogPath { get; } =
        RepositoryPaths.Repository("specs", "behavior", "catalog.json");

    public static CatalogFile Build()
    {
        var authorities = AuthoritySources.Read();
        var adjudications = ReadAdjudications();
        Validate(authorities, adjudications);
        var byId = adjudications.Sources.ToDictionary(item => item.Id, StringComparer.Ordinal);
        return new CatalogFile(
            2,
            "docs/behavioral-specification.md",
            [.. authorities.Select(source => Join(source, byId[source.Id]))]);
    }

    public static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, JsonOptions)
            .Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";

    public static void Write()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CatalogPath)!);
        File.WriteAllText(CatalogPath, Serialize(Build()), new System.Text.UTF8Encoding(false));
    }

    public static void Check()
    {
        string expected = Serialize(Build());
        if (!File.Exists(CatalogPath))
        {
            throw new InvalidDataException(
                "specs/behavior/catalog.json is absent; run Marvel.Behavior.Index write");
        }

        string actual = File.ReadAllText(CatalogPath);
        if (!string.Equals(expected, actual, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "specs/behavior/catalog.json is stale; run Marvel.Behavior.Index write");
        }
    }

    /// <summary>Creates an invalid-by-design starting point; review must replace every marker.</summary>
    public static void Scaffold()
    {
        var file = new AdjudicationFile(
            2,
            [.. AuthoritySources.Read().Select(source => new Adjudication(
                source.Id,
                source.Fingerprint,
                [new Obligation(
                    "unreviewed",
                    "This source has not been adjudicated.",
                    "unreviewed",
                    null,
                    null,
                    null,
                    null,
                    null,
                    [],
                    null)]))]);
        Directory.CreateDirectory(Path.GetDirectoryName(AdjudicationsPath)!);
        File.WriteAllText(
            AdjudicationsPath,
            Serialize(file),
            new System.Text.UTF8Encoding(false));
    }

    public static void Skeletons(CatalogFile catalog, TextWriter output)
    {
        output.WriteLine("Feature: Authority-derived behavior obligations");
        foreach (var source in catalog.Sources)
        {
            foreach (var obligation in source.Obligations
                .Where(obligation => obligation.Disposition == "executable"))
            {
                output.WriteLine();
                output.WriteLine($"  @{obligation.Id}");
                output.WriteLine($"  @{source.Id}");
                output.WriteLine($"  Scenario: {obligation.Summary}");
                output.WriteLine("    Given a canonical core game");
                output.WriteLine("    When the obligation is exercised");
                output.WriteLine(obligation.Implementation == "unimplemented"
                    ? $"    Then {obligation.Exception} is raised"
                    : "    Then the published behavior is observed");
            }
        }
    }

    private static AdjudicationFile ReadAdjudications()
    {
        if (!File.Exists(AdjudicationsPath))
        {
            throw new InvalidDataException(
                "specs/behavior/adjudications.json is absent; run Marvel.Behavior.Index scaffold");
        }

        return JsonSerializer.Deserialize<AdjudicationFile>(
            File.ReadAllBytes(AdjudicationsPath), JsonOptions)
            ?? throw new InvalidDataException("adjudications.json is empty");
    }

    private static void Validate(
        IReadOnlyList<AuthoritySource> authorities,
        AdjudicationFile file)
    {
        if (file.Version != 2)
        {
            throw new InvalidDataException(
                $"adjudications.json version is {file.Version}, expected 2");
        }

        var duplicate = file.Sources
            .GroupBy(item => item.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidDataException($"duplicate adjudication {duplicate.Key}");
        }

        var expected = authorities.Select(source => source.Id).ToList();
        var actual = file.Sources.Select(source => source.Id).ToList();
        if (!expected.SequenceEqual(actual, StringComparer.Ordinal))
        {
            string? missing = expected.Except(actual, StringComparer.Ordinal).FirstOrDefault();
            string? extra = actual.Except(expected, StringComparer.Ordinal).FirstOrDefault();
            throw new InvalidDataException(
                "adjudications do not exactly match authority order"
                + (missing is null ? "" : $"; missing {missing}")
                + (extra is null ? "" : $"; extra {extra}"));
        }

        var fingerprints = authorities.ToDictionary(
            source => source.Id,
            source => source.Fingerprint,
            StringComparer.Ordinal);
        foreach (var item in file.Sources)
        {
            EnsureReviewedFingerprint(item.Id, item.Fingerprint, fingerprints[item.Id]);
        }

        var known = file.Sources
            .SelectMany(item => item.Obligations.Select(obligation => new
            {
                Id = $"behavior:{item.Id}:{obligation.Key}",
                obligation.Disposition,
            }))
            .ToDictionary(item => item.Id, item => item.Disposition, StringComparer.Ordinal);
        foreach (var item in file.Sources)
        {
            Validate(item, known);
        }
    }

    internal static void EnsureReviewedFingerprint(
        string id,
        string reviewed,
        string current)
    {
        if (!string.Equals(reviewed, current, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                $"{id} changed from reviewed fingerprint {reviewed} "
                + $"to {current}; re-adjudicate that source");
        }
    }

    private static void Validate(
        Adjudication item,
        IReadOnlyDictionary<string, string> known)
    {
        if (item.Obligations.Count == 0)
        {
            throw new InvalidDataException($"{item.Id} has no obligation dispositions");
        }

        var duplicate = item.Obligations
            .GroupBy(obligation => obligation.Key, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidDataException(
                $"{item.Id} repeats obligation key {duplicate.Key}");
        }

        foreach (var obligation in item.Obligations)
        {
            Validate(item.Id, obligation, known);
        }
    }

    private static void Validate(
        string sourceId,
        Obligation item,
        IReadOnlyDictionary<string, string> known)
    {
        string id = $"behavior:{sourceId}:{item.Key}";
        if (!IsBranchKey(item.Key) || string.IsNullOrWhiteSpace(item.Summary))
        {
            throw new InvalidDataException($"{id} has an invalid key or summary");
        }

        if (!Dispositions.Contains(item.Disposition))
        {
            throw new InvalidDataException($"{id} has invalid disposition {item.Disposition}");
        }

        if (item.Disposition == "executable")
        {
            if (item.Reason is not null || item.Target is not null)
            {
                throw new InvalidDataException(
                    $"{id} is executable and cannot carry reason or target");
            }

            if (item.Implementation is null || !Implementations.Contains(item.Implementation))
            {
                throw new InvalidDataException(
                    $"{id} has invalid implementation {item.Implementation ?? "(none)"}");
            }

            if (item.Implementation == "unimplemented"
                && (string.IsNullOrWhiteSpace(item.WorkItem)
                    || string.IsNullOrWhiteSpace(item.Exception)))
            {
                throw new InvalidDataException(
                    $"{id} is unimplemented without work_item and exception");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(item.Reason))
            {
                throw new InvalidDataException($"{id} disposition requires a reason");
            }

            if (item.Implementation is not null
                || item.WorkItem is not null
                || item.Exception is not null)
            {
                throw new InvalidDataException(
                    $"{id} is non-executable but carries implementation fields");
            }
        }

        if (item.Disposition is "narrower" or "superseded")
        {
            if (item.Target is null
                || !known.TryGetValue(item.Target, out string? targetDisposition)
                || targetDisposition != "executable")
            {
                throw new InvalidDataException(
                    $"{id} names no executable target {item.Target ?? "(none)"}");
            }
        }
        else if (item.Target is not null)
        {
            throw new InvalidDataException($"{id} carries a target but is not narrower");
        }
    }

    private static bool IsBranchKey(string key) => key.Length > 0 && key.All(character =>
        character is >= 'a' and <= 'z'
        || character is >= '0' and <= '9'
        || character == '-');

    private static CatalogEntry Join(AuthoritySource source, Adjudication item) => new(
        source.Id,
        source.Kind,
        source.Title,
        source.Fingerprint,
        source.Scope,
        SourceDisposition(item.Obligations),
        [.. item.Obligations.Select(obligation => new CatalogObligation(
            $"behavior:{source.Id}:{obligation.Key}",
            obligation.Summary,
            obligation.Disposition,
            obligation.Reason,
            obligation.Target,
            obligation.Implementation,
            obligation.WorkItem,
            obligation.Exception,
            obligation.Scenarios,
            obligation.Mutation))]);

    private static string SourceDisposition(IReadOnlyList<Obligation> obligations)
    {
        var dispositions = obligations
            .Select(obligation => obligation.Disposition)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        return dispositions.Count == 1 ? dispositions[0] : "mixed";
    }
}
