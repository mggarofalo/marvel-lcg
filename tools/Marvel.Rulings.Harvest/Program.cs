using System.Net.Http.Headers;
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
            if (!TryRead(from, pages, out var rulings))
            {
                return 0;
            }

            string harvested = args.Length > 3 ? args[3] : Harvested(Path.Combine(dataset, "rulings.json"));
            Directory.CreateDirectory(into);
            File.WriteAllText(Path.Combine(into, "rulings.json"), Emit.Json(rulings, harvested));
            Console.Error.WriteLine($"wrote {rulings.Count} rulings into {into}");
            return 0;
        }

    case "check":
        {
            string from = args.Length > 1 ? args[1] : Path.Combine(dataset, "pages");
            if (!TryRead(from, pages, out var rulings))
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

static bool TryRead(string directory, IReadOnlyList<Page> pages, out IReadOnlyList<Ruling> rulings)
{
    var missing = pages.Where(page => !File.Exists(Path.Combine(directory, page.FileName))).ToList();
    if (missing.Count > 0)
    {
        Console.Error.WriteLine(
            $"no complete Hall of Heroes harvest at {directory}; rulings are unavailable, not empty");
        rulings = [];
        return false;
    }

    rulings = pages.SelectMany(page =>
        Harvest.Read(File.ReadAllText(Path.Combine(directory, page.FileName)), page)).ToList();
    return true;
}

static int Parity(IReadOnlyList<Ruling> candidate, string committedPath)
{
    string committedText = File.ReadAllText(committedPath);
    using var committedJson = JsonDocument.Parse(committedText);
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
    bool sameIndex = string.Equals(
        Emit.Json(candidate, committedJson.RootElement.GetProperty("harvested").GetString()!),
        committedText,
        StringComparison.Ordinal);
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

    using var committed = JsonDocument.Parse(File.ReadAllText(committedPath));
    return committed.RootElement.GetProperty("harvested").GetString()!;
}

static async Task<int> Fetch(string into, IReadOnlyList<Page> pages)
{
    using var client = new HttpClient();
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("marvel-lcg-rulings-harvester", "1.0"));
    client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(+https://github.com/mggarofalo/marvel-lcg)"));
    var fetched = new Dictionary<Page, string>();
    foreach (Page page in pages)
    {
        var uri = new Uri("https://" + page.Via);
        fetched[page] = await client.GetStringAsync(uri);
        Console.Error.WriteLine($"fetched {uri}");
    }

    // A failed request leaves an existing cache intact rather than mixing
    // pages from two observations into one plausible-looking harvest.
    Directory.CreateDirectory(into);
    foreach ((Page page, string html) in fetched)
    {
        await File.WriteAllTextAsync(Path.Combine(into, page.FileName), html);
    }

    return 0;
}

static string DefaultCache() => Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "marvel-lcg", "hall-of-heroes");
