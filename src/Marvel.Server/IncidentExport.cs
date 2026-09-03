using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Server;

/// <summary>One read-only operator evidence manifest.</summary>
public sealed record IncidentManifest(
    string Format,
    int Schema,
    DateTimeOffset CreatedUtc,
    RuntimeIdentity Runtime,
    string Health,
    IReadOnlyList<IncidentDiagnosticFile> Diagnostics,
    IReadOnlyList<IncidentSaveGeneration> SaveGenerations);

/// <summary>A retained log file and its selected already-redacted records.</summary>
public sealed record IncidentDiagnosticFile(
    string Name,
    long Bytes,
    string Sha256,
    int InvalidRecords,
    IReadOnlyList<OperationalRecord> Records);

/// <summary>Hashes identifying one save generation without exporting its contents.</summary>
public sealed record IncidentSaveGeneration(
    string StorageId,
    string Generation,
    bool Selected,
    string? SessionSha256,
    string? AuthoritySha256,
    string? ErrorCode);

/// <summary>Builds evidence without loading, replaying, migrating, or repairing a session.</summary>
public static class IncidentExporter
{
    private const int MaximumDiagnosticLineCharacters = 4096;
    private const int MaximumManifestBytes = 64;
    private const int MaximumSelectedRecords = 2000;
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    /// <summary>Collects one manifest without changing either evidence root.</summary>
    public static IncidentManifest Build(
        string dataRoot,
        string saveRoot,
        string diagnosticsRoot,
        Func<DateTimeOffset>? clock = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(saveRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(diagnosticsRoot);
        RuntimeIdentity runtime = DatasetGameFactory.Load(dataRoot).DiscoverSetup().Runtime;
        IReadOnlyList<IncidentDiagnosticFile> diagnostics = ReadDiagnostics(diagnosticsRoot);
        IReadOnlyList<IncidentSaveGeneration> saves = ReadSaves(saveRoot);
        string health = saves.Any(save => save.Selected && save.ErrorCode is not null)
            ? "offline_evidence_has_invalid_selected_generation"
            : "offline_evidence_only";
        return new IncidentManifest(
            "marvel-incident",
            1,
            (clock ?? (() => DateTimeOffset.UtcNow))(),
            runtime,
            health,
            diagnostics,
            saves);
    }

    /// <summary>Serializes the stable manifest shape for stdout export.</summary>
    public static string Serialize(IncidentManifest manifest) =>
        JsonSerializer.Serialize(manifest, Options) + "\n";

    private static List<IncidentDiagnosticFile> ReadDiagnostics(string root)
    {
        if (!Directory.Exists(root))
        {
            return [];
        }

        var result = new List<IncidentDiagnosticFile>();
        foreach (string path in Directory.GetFiles(root, "operational*.jsonl")
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            var records = new List<OperationalRecord>();
            int invalid = 0;
            foreach (string? line in ReadBoundedLines(path))
            {
                try
                {
                    if (line is null)
                    {
                        throw new JsonException("operational record exceeds its bound");
                    }
                    records.Add(OperationalJson.ReadVerified(line));
                }
                catch (JsonException)
                {
                    invalid++;
                }
            }

            result.Add(new IncidentDiagnosticFile(
                Path.GetFileName(path),
                new FileInfo(path).Length,
                Hash(path),
                invalid,
                records.TakeLast(MaximumSelectedRecords).ToArray()));
        }

        return result;
    }

    private static List<IncidentSaveGeneration> ReadSaves(string root)
    {
        if (!Directory.Exists(root))
        {
            return [];
        }

        var result = new List<IncidentSaveGeneration>();
        foreach (string directory in Directory.GetDirectories(root)
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            string storageId = Path.GetFileName(directory);
            if (!ValidToken(storageId))
            {
                continue;
            }

            string manifest = Path.Combine(directory, "current");
            string? selected = ReadManifest(manifest);
            bool validSelection = selected is not null && ValidToken(selected);
            string[] generations = Directory.GetFiles(directory, "*.session.json")
                .Select(path => Path.GetFileName(path)[..^".session.json".Length])
                .Concat(Directory.GetFiles(directory, "*.authority.json")
                    .Select(path => Path.GetFileName(path)[..^".authority.json".Length]))
                .Where(ValidToken)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            foreach (string generation in generations)
            {
                string session = Path.Combine(directory, generation + ".session.json");
                string authority = Path.Combine(directory, generation + ".authority.json");
                bool complete = File.Exists(session) && File.Exists(authority);
                result.Add(new IncidentSaveGeneration(
                    storageId,
                    generation,
                    string.Equals(generation, selected, StringComparison.Ordinal),
                    File.Exists(session) ? Hash(session) : null,
                    File.Exists(authority) ? Hash(authority) : null,
                    !complete ? "incomplete_generation" : null));
            }

            if (!validSelection || !generations.Contains(selected!, StringComparer.Ordinal))
            {
                result.Add(new IncidentSaveGeneration(
                    storageId,
                    validSelection ? selected! : "unknown",
                    Selected: true,
                    SessionSha256: null,
                    AuthoritySha256: null,
                    validSelection ? "incomplete_generation" : "invalid_manifest"));
            }
        }

        return result;
    }

    private static bool ValidToken(string value) =>
        value.Length == 32
        && value.All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static string? ReadManifest(string path)
    {
        if (!File.Exists(path) || new FileInfo(path).Length > MaximumManifestBytes)
        {
            return null;
        }

        return File.ReadAllText(path).Trim();
    }

    private static IEnumerable<string?> ReadBoundedLines(string path)
    {
        using var reader = new StreamReader(path, detectEncodingFromByteOrderMarks: true);
        var line = new System.Text.StringBuilder();
        bool exceeded = false;
        int character;
        while ((character = reader.Read()) >= 0)
        {
            if (character == '\n')
            {
                if (!exceeded && line.Length > 0 && line[^1] == '\r')
                {
                    line.Length--;
                }
                yield return exceeded ? null : line.ToString();
                line.Clear();
                exceeded = false;
            }
            else if (!exceeded)
            {
                if (line.Length < MaximumDiagnosticLineCharacters)
                {
                    line.Append((char)character);
                }
                else
                {
                    exceeded = true;
                    line.Clear();
                }
            }
        }

        if (exceeded || line.Length > 0)
        {
            yield return exceeded ? null : line.ToString();
        }
    }

    private static string Hash(string path)
    {
        using var stream = new FileStream(
            path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
