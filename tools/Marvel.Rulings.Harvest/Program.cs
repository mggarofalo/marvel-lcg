using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Marvel.Rulings.Harvest;
using Marvel.Tests;

var pages = new Page[]
{
    new("official-ffg-rulings", "official-ffg-rulings.html", "hallofheroeslcg.com/official-ffg-rulings/", "pre-1.5", PageShape.Compendium),
    new("post-rrg-1-5", "post-rrg-1-5.html", "hallofheroeslcg.com/latest-ffg-rulings-post-rrg-1-5/", "1.5", PageShape.Chronological),
    new("post-rrg-1-6", "post-rrg-1-6.html", "hallofheroeslcg.com/latest-ffg-rulings-post-rrg-1-6/", "1.6", PageShape.Chronological),
    new("post-rrg-1-7", "post-rrg-1-7.html", "hallofheroeslcg.com/latest-ffg-rulings-post-rrg-1-7/", "1.7-1.8", PageShape.Chronological),
};

string verb = args.Length == 0 ? "check" : args[0].TrimStart('-');
string dataset = RepositoryPaths.Dataset("rulings");

switch (verb)
{
    case "fetch":
        return await Fetch(args.Length > 1 ? args[1] : DefaultCache(), pages);

    case "write":
        {
            string from = args.Length > 1 ? args[1] : Path.Combine(dataset, "pages");
            string into = args.Length > 2 ? args[2] : dataset;
            if (!TryRead(from, pages, out var rulings, out var pageBytes))
            {
                return 1;
            }

            string harvested = args.Length > 3 ? args[3] : Harvested(Path.Combine(dataset, "rulings.json"));
            Directory.CreateDirectory(into);
            File.WriteAllBytes(Path.Combine(into, "rulings.json"), Emit.JsonBytes(rulings, harvested));
            File.WriteAllBytes(Path.Combine(into, "pages.manifest.json"), Emit.ManifestBytes(pageBytes));
            Console.Error.WriteLine($"wrote {rulings.Count} rulings into {into}");
            return 0;
        }

    case "check":
        {
            string from = args.Length > 1 ? args[1] : Path.Combine(dataset, "pages");
            bool pinned = args.Length <= 1;
            if (pinned && !VerifyManifest(dataset, pages))
            {
                return 1;
            }

            if (!TryRead(from, pages, out var rulings, out _))
            {
                // A local acquisition cache is optional. The committed pages
                // are not: CI's offline gate must fail if its input vanished.
                return args.Length > 1 ? 0 : 1;
            }

            return Parity(rulings, Path.Combine(dataset, "rulings.json"));
        }

    default:
        Console.Error.WriteLine(
            """
            Harvests Hall of Heroes rulings into datasets/rulings/.

              fetch [cache]                    acquire four pages into a local cache
              write [pages] [into] [date]      parse cached pages into rulings.json
              check [pages]                    compare cached pages with the pinned snapshot

            `write` and `check` are offline. `--check` is accepted as an alias.
            """);
        return 2;
}

static bool TryRead(
    string directory,
    IReadOnlyList<Page> pages,
    out IReadOnlyList<Ruling> rulings,
    out IReadOnlyDictionary<Page, byte[]> pageBytes)
{
    var missing = pages.Where(page => !File.Exists(Path.Combine(directory, page.FileName))).ToList();
    if (missing.Count > 0)
    {
        Console.Error.WriteLine(
            $"no complete Hall of Heroes harvest at {directory}; rulings are unavailable, not empty");
        rulings = [];
        pageBytes = new Dictionary<Page, byte[]>();
        return false;
    }

    var bytes = pages.ToDictionary(
        page => page,
        page => File.ReadAllBytes(Path.Combine(directory, page.FileName)));
    var utf8 = new UTF8Encoding(false, true);
    rulings = pages.SelectMany(page => Harvest.Read(utf8.GetString(bytes[page]), page)).ToList();
    pageBytes = bytes;
    return true;
}

static int Parity(IReadOnlyList<Ruling> candidate, string committedPath)
{
    byte[] committedBytes = File.ReadAllBytes(committedPath);
    using var committedJson = JsonDocument.Parse(committedBytes);
    var committed = committedJson.RootElement.GetProperty("rulings").EnumerateArray()
        .ToDictionary(
            item => item.GetProperty("id").GetString()!,
            item => item.GetProperty("hash").GetString()!,
            StringComparer.Ordinal);
    var mine = candidate.ToDictionary(item => item.Id, item => item.Hash, StringComparer.Ordinal);

    var added = mine.Keys.Except(committed.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
    var removed = committed.Keys.Except(mine.Keys, StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();
    var revised = mine.Keys.Intersect(committed.Keys, StringComparer.Ordinal)
        .Where(id => !string.Equals(mine[id], committed[id], StringComparison.Ordinal))
        .Order(StringComparer.Ordinal)
        .ToList();

    Console.WriteLine($"harvested {mine.Count} rulings; committed {committed.Count}");
    Console.WriteLine($"  {added.Count} added");
    Console.WriteLine($"  {revised.Count} revised");
    Console.WriteLine($"  {removed.Count} removed");
    foreach (string id in added) Console.WriteLine($"  added    {id}");
    foreach (string id in revised) Console.WriteLine($"  revised  {id}");
    foreach (string id in removed) Console.WriteLine($"  removed  {id}");
    byte[] candidateBytes = Emit.JsonBytes(
        candidate,
        committedJson.RootElement.GetProperty("harvested").GetString()!);
    bool sameIndex = candidateBytes.AsSpan().SequenceEqual(committedBytes);
    if (!sameIndex && added.Count == 0 && revised.Count == 0 && removed.Count == 0)
    {
        Console.WriteLine("  index differs without a ruling content change");
    }

    return sameIndex ? 0 : 1;
}

static string Harvested(string committedPath)
{
    if (!File.Exists(committedPath))
    {
        throw new InvalidOperationException("the first write requires an explicit harvest date");
    }

    using var committed = JsonDocument.Parse(File.ReadAllBytes(committedPath));
    return committed.RootElement.GetProperty("harvested").GetString()!;
}

static bool VerifyManifest(string dataset, IReadOnlyList<Page> pages)
{
    string manifestPath = Path.Combine(dataset, "pages.manifest.json");
    if (!File.Exists(manifestPath))
    {
        Console.Error.WriteLine($"no vendored page manifest at {manifestPath}");
        return false;
    }

    using var manifest = JsonDocument.Parse(File.ReadAllBytes(manifestPath));
    var expected = manifest.RootElement.GetProperty("files").EnumerateArray()
        .ToDictionary(
            file => file.GetProperty("path").GetString()!,
            file => (
                Bytes: file.GetProperty("bytes").GetInt64(),
                Hash: file.GetProperty("hash").GetString()!),
            StringComparer.Ordinal);
    bool valid = expected.Count == pages.Count;
    foreach (Page page in pages)
    {
        string relative = "pages/" + page.FileName;
        string path = Path.Combine(dataset, "pages", page.FileName);
        if (!expected.TryGetValue(relative, out var pin) || !File.Exists(path))
        {
            Console.Error.WriteLine($"  unpinned or missing  {relative}");
            valid = false;
            continue;
        }

        byte[] bytes = File.ReadAllBytes(path);
        string hash = "sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (bytes.LongLength != pin.Bytes || !string.Equals(hash, pin.Hash, StringComparison.Ordinal))
        {
            Console.Error.WriteLine($"  page differs         {relative}");
            valid = false;
        }
    }

    Console.WriteLine(valid
        ? $"verified {pages.Count} vendored page hashes"
        : "vendored page manifest does not match");
    return valid;
}

static async Task<int> Fetch(string into, IReadOnlyList<Page> pages)
{
    using var client = new HttpClient();
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("marvel-lcg-rulings-harvester", "1.0"));
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(+https://github.com/mggarofalo/marvel-lcg)"));
    var fetched = new Dictionary<Page, byte[]>();
    foreach (Page page in pages)
    {
        var uri = new Uri("https://" + page.Via);
        fetched[page] = await client.GetByteArrayAsync(uri);
        Console.Error.WriteLine($"fetched {uri}");
    }

    // A failed request leaves an existing cache intact rather than mixing
    // pages from two observations into one plausible-looking harvest.
    Directory.CreateDirectory(into);
    foreach ((Page page, byte[] html) in fetched)
    {
        await File.WriteAllBytesAsync(Path.Combine(into, page.FileName), html);
    }

    return 0;
}

static string DefaultCache() => Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "marvel-lcg", "hall-of-heroes");
