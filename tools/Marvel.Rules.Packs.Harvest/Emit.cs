using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Marvel.Rules.Packs.Harvest;

public static partial class Emit
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static IReadOnlyDictionary<string, string> Build(
        IReadOnlyList<PackDocument> documents)
    {
        var records = new List<IndexRecord>();
        var tree = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (PackDocument document in documents)
        {
            foreach (Section section in document.Sections)
            {
                string key = Harvest.Slug(section.Heading);
                if (key.Length == 0)
                {
                    throw new InvalidDataException(
                        $"section heading in {document.Path} has no safe destination slug");
                }

                string identifier = $"pack:{document.Code}:{key}";
                if (!seen.Add(identifier))
                {
                    int suffix = 2;
                    while (!seen.Add($"{identifier}-{suffix}"))
                    {
                        suffix += 1;
                    }

                    identifier = $"{identifier}-{suffix}";
                    key = $"{key}-{suffix}";
                }

                records.Add(new IndexRecord(
                    identifier,
                    section.Heading,
                    document.Code,
                    document.Kind,
                    document.Path,
                    section.Page,
                    Sentence(section.Paragraphs.Count > 0
                        ? section.Paragraphs[0]
                        : section.Rules[0].Heading),
                    Hash(section.Text),
                    section.Rules.Count));

                records.AddRange(section.Rules.Select(rule => new IndexRecord(
                    $"{identifier}.{Harvest.Slug(rule.Heading)}",
                    rule.Heading,
                    document.Code,
                    document.Kind,
                    document.Path,
                    section.Page,
                    Sentence(rule.Text),
                    Hash(rule.Text),
                    null)));

                var fields = new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["id"] = identifier,
                    ["title"] = section.Heading,
                    ["pack"] = document.Code,
                    ["kind"] = document.Kind,
                    ["source"] = document.Path,
                    ["page"] = section.Page,
                    ["hash"] = Hash(section.Text),
                    ["rules"] = section.Rules.Select(rule => Harvest.Slug(rule.Heading)).ToList(),
                };
                var body = new List<string> { FrontMatter(fields), string.Empty, $"# {section.Heading}", string.Empty };
                body.AddRange(section.Paragraphs.Select(paragraph => paragraph + "\n"));
                foreach (NamedRule rule in section.Rules)
                {
                    string anchor = Harvest.Slug(rule.Heading);
                    body.Add($"<a id=\"{anchor}\"></a>");
                    body.Add($"## {rule.Heading}");
                    body.Add(string.Empty);
                    body.AddRange(rule.Paragraphs.Select(paragraph => paragraph + "\n"));
                }

                tree[$"{document.Code}/{key}.md"] = string.Join('\n', body);
            }
        }

        var index = new Index(
            "pack",
            documents.Count,
            records.Count,
            documents.Select(document => document.Code).Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal).ToList(),
            records);
        tree["index.json"] = JsonSerializer.Serialize(index, JsonOptions)
            .ReplaceLineEndings("\n") + "\n";
        return tree;
    }

    public static IReadOnlyDictionary<string, string> ReadTree(string root)
    {
        if (!Directory.Exists(root))
        {
            return new Dictionary<string, string>();
        }

        return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => !string.Equals(Path.GetFileName(path), "UPSTREAM.md", StringComparison.Ordinal))
            .Where(path => string.Equals(Path.GetExtension(path), ".json", StringComparison.Ordinal)
                || string.Equals(Path.GetExtension(path), ".md", StringComparison.Ordinal))
            .ToDictionary(
                path => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'),
                File.ReadAllText,
                StringComparer.Ordinal);
    }

    public static IReadOnlyDictionary<string, byte[]> ReadTreeBytes(string root)
    {
        if (!Directory.Exists(root))
        {
            return new Dictionary<string, byte[]>();
        }

        return Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .Where(path => !string.Equals(Path.GetFileName(path), "UPSTREAM.md", StringComparison.Ordinal))
            .Where(path => string.Equals(Path.GetExtension(path), ".json", StringComparison.Ordinal)
                || string.Equals(Path.GetExtension(path), ".md", StringComparison.Ordinal))
            .ToDictionary(
                path => Path.GetRelativePath(root, path).Replace(Path.DirectorySeparatorChar, '/'),
                File.ReadAllBytes,
                StringComparer.Ordinal);
    }

    public static string Manifest(
        IReadOnlyList<string> sources,
        IReadOnlyDictionary<string, string> snapshot)
        => Manifest(
            sources,
            snapshot.ToDictionary(
                pair => pair.Key,
                pair => Encoding.UTF8.GetBytes(pair.Value),
                StringComparer.Ordinal));

    public static string Manifest(
        IReadOnlyList<string> sources,
        IReadOnlyDictionary<string, byte[]> snapshot)
    {
        var files = sources.Select(path =>
        {
            byte[] bytes = File.ReadAllBytes(path);
            return new ManifestFile(
                Path.GetFileName(path),
                bytes.LongLength,
                Sha256(bytes));
        }).OrderBy(file => file.Path, StringComparer.Ordinal).ToList();
        var pin = SnapshotHash(snapshot);
        var manifest = new SourceManifest(
            1,
            "sha256",
            files,
            new SnapshotPin("sha256-length-prefixed-path-and-bytes", pin.Files, pin.Hash));
        return JsonSerializer.Serialize(manifest, JsonOptions).ReplaceLineEndings("\n") + "\n";
    }

    public static (int Files, string Hash) SnapshotHash(
        IReadOnlyDictionary<string, byte[]> snapshot)
    {
        var content = snapshot
            .Where(pair => !string.Equals(pair.Key, "sources.manifest.json", StringComparison.Ordinal))
            .OrderBy(pair => pair.Key, StringComparer.Ordinal).ToList();
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[sizeof(long)];
        foreach ((string path, byte[] bytes) in content)
        {
            byte[] pathBytes = Encoding.UTF8.GetBytes(path);
            BinaryPrimitives.WriteInt64BigEndian(length, pathBytes.LongLength);
            digest.AppendData(length);
            digest.AppendData(pathBytes);
            BinaryPrimitives.WriteInt64BigEndian(length, bytes.LongLength);
            digest.AppendData(length);
            digest.AppendData(bytes);
        }

        return (
            content.Count,
            "sha256:" + Convert.ToHexString(digest.GetHashAndReset()).ToLowerInvariant());
    }

    public static bool VerifyManifest(IReadOnlyList<string> sources, string root)
    {
        string manifestPath = Path.Combine(root, "sources.manifest.json");
        if (!File.Exists(manifestPath))
        {
            Console.Error.WriteLine($"no source manifest at {manifestPath}");
            return false;
        }

        IReadOnlyDictionary<string, byte[]> tree = ReadTreeBytes(root);
        using var document = JsonDocument.Parse(File.ReadAllBytes(manifestPath));
        var expected = document.RootElement.GetProperty("files").EnumerateArray()
            .ToDictionary(
                file => file.GetProperty("path").GetString()!,
                file => (
                    Bytes: file.GetProperty("bytes").GetInt64(),
                    Hash: file.GetProperty("hash").GetString()!),
                StringComparer.Ordinal);
        bool valid = expected.Count == sources.Count;
        foreach (string source in sources)
        {
            string name = Path.GetFileName(source);
            byte[] bytes = File.ReadAllBytes(source);
            if (!expected.TryGetValue(name, out var pin)
                || pin.Bytes != bytes.LongLength
                || !string.Equals(pin.Hash, Sha256(bytes), StringComparison.Ordinal))
            {
                Console.Error.WriteLine($"  source differs  {name}");
                valid = false;
            }
        }

        string expectedSnapshot = document.RootElement.GetProperty("snapshot").GetProperty("hash").GetString()!;
        int expectedFiles = document.RootElement.GetProperty("snapshot").GetProperty("files").GetInt32();
        string expectedSnapshotAlgorithm = document.RootElement.GetProperty("snapshot")
            .GetProperty("algorithm").GetString()!;
        if (!string.Equals(
            expectedSnapshotAlgorithm,
            "sha256-length-prefixed-path-and-bytes",
            StringComparison.Ordinal))
        {
            Console.Error.WriteLine("  unsupported snapshot-hash algorithm");
            valid = false;
        }
        string actualManifest = Manifest(sources, tree);
        using var actual = JsonDocument.Parse(actualManifest);
        string actualSnapshot = actual.RootElement.GetProperty("snapshot").GetProperty("hash").GetString()!;
        int actualFiles = actual.RootElement.GetProperty("snapshot").GetProperty("files").GetInt32();
        if (expectedFiles != actualFiles || !string.Equals(expectedSnapshot, actualSnapshot, StringComparison.Ordinal))
        {
            Console.Error.WriteLine("  committed snapshot differs from its manifest");
            valid = false;
        }

        Console.Error.WriteLine(valid
            ? $"verified {sources.Count} local PDF hashes and {actualFiles} vendored snapshot files"
            : "rules-pack source manifest does not match");
        return valid;
    }

    public static void Write(IReadOnlyDictionary<string, string> tree, string root)
    {
        var destinations = tree.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => (Path: Destination(root, pair.Key), Contents: pair.Value))
            .ToList();
        Directory.CreateDirectory(Path.GetFullPath(root));
        foreach (string path in Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories)
            .Where(path => !string.Equals(Path.GetFileName(path), "UPSTREAM.md", StringComparison.Ordinal)))
        {
            File.Delete(path);
        }

        foreach ((string path, string contents) in destinations)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, contents, new UTF8Encoding(false));
        }
    }

    public static string Destination(string root, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || Path.IsPathRooted(relative))
        {
            throw new InvalidDataException($"snapshot destination is not relative: {relative}");
        }

        string fullRoot = Path.GetFullPath(root);
        string prefix = Path.TrimEndingDirectorySeparator(fullRoot) + Path.DirectorySeparatorChar;
        string candidate = Path.GetFullPath(Path.Combine(
            fullRoot,
            relative.Replace('/', Path.DirectorySeparatorChar)));
        StringComparison comparison = OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;
        if (!candidate.StartsWith(prefix, comparison))
        {
            throw new InvalidDataException(
                $"snapshot destination escapes the output root: {relative}");
        }

        return candidate;
    }

    public static string Hash(string text)
    {
        string plain = Whitespace().Replace(text, " ").Trim();
        return "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plain)))
            .ToLowerInvariant();
    }

    public static string Sentence(string text)
    {
        string plain = text.Replace("*", string.Empty, StringComparison.Ordinal).Trim();
        Match match = FirstSentence().Match(plain);
        return match.Success ? match.Groups[1].Value.Trim() : plain;
    }

    private static string FrontMatter(IReadOnlyDictionary<string, object?> fields)
    {
        var lines = new List<string> { "---" };
        foreach ((string key, object? value) in fields)
        {
            lines.Add(value switch
            {
                int number => $"{key}: {number}",
                IReadOnlyList<string> list => list.Count == 0
                    ? $"{key}: []"
                    : $"{key}: [{string.Join(", ", list.Select(JsonString))}]",
                string text => $"{key}: {JsonString(text)}",
                _ => throw new InvalidDataException($"unsupported front-matter field {key}"),
            });
        }

        lines.Add("---");
        return string.Join('\n', lines);
    }

    private static string JsonString(string value) => JsonSerializer.Serialize(value, JsonOptions);

    private sealed record Index(
        string Tier,
        int Documents,
        int RecordCount,
        IReadOnlyList<string> Packs,
        IReadOnlyList<IndexRecord> Entries);

    private sealed record IndexRecord(
        string Id,
        string Title,
        string Pack,
        string Kind,
        string Source,
        int Page,
        string Fragment,
        string Hash,
        int? Rules);

    private sealed record SourceManifest(
        int Version,
        string Algorithm,
        IReadOnlyList<ManifestFile> Files,
        SnapshotPin Snapshot);

    private sealed record ManifestFile(string Path, long Bytes, string Hash);

    private sealed record SnapshotPin(string Algorithm, int Files, string Hash);

    private static string Sha256(byte[] bytes) =>
        "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace();

    [GeneratedRegex("^(.+?[.!?])(?:\\s|$)", RegexOptions.CultureInvariant)]
    private static partial Regex FirstSentence();
}
