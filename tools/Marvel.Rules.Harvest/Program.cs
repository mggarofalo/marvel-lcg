using System.Text.Json;
using Marvel.Rules.Harvest;
using Marvel.Tests;
using UglyToad.PdfPig;

var icons = new Dictionary<string, string>(StringComparer.Ordinal);
using (var legend = JsonDocument.Parse(
    File.ReadAllText(RepositoryPaths.Dataset("rules-reference", "icons.json"))))
{
    foreach (var icon in legend.RootElement.EnumerateObject())
    {
        icons[icon.Name] = icon.Value.GetString()!;
    }
}

Pages.Icons = icons;

string verb = args.Length > 0 ? args[0] : "check";
string pdf = args.Length > 1
    ? args[1]
    : Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Documents", "Marvel Champions LCG", "mc_rulesreference_v18_compressed.pdf");

if (!File.Exists(pdf))
{
    Console.Error.WriteLine(
        $"no Rules Reference at {pdf}. The document is copyrighted and is not in this "
        + "repository; point this at your own copy.");
    return 2;
}

using var document = PdfDocument.Open(pdf);
var entries = Harvest.Read(document);

switch (verb)
{
    case "write":
        string into = args.Length > 2 ? args[2] : RepositoryPaths.Dataset("rules-reference");
        Emit.Write(entries, into, "1.8", icons);
        Console.Error.WriteLine($"wrote {entries.Count} entries into {into}");
        return 0;

    case "check":
        return Parity(entries);

    default:
        Console.Error.WriteLine(
            """
            Reads the Rules Reference PDF into datasets/rules-reference/.

              write [pdf] [into]   harvest and write the dataset
              check [pdf]          harvest and report how it differs from the committed one
            """);
        return 2;
}

static int Parity(IReadOnlyList<Entry> entries)
{
    using var index = JsonDocument.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("rules-reference", "index.json")));
    var committed = index.RootElement.GetProperty("entries").EnumerateArray()
        .ToDictionary(
            entry => entry.GetProperty("id").GetString()!,
            entry => entry.TryGetProperty("fragment", out var f) ? f.GetString() ?? "" : "",
            StringComparer.Ordinal);

    var mine = entries.SelectMany(entry => entry.Records())
        .ToDictionary(record => record.Id, record => record, StringComparer.Ordinal);

    var missing = committed.Keys.Where(id => !mine.ContainsKey(id))
        .OrderBy(id => id, StringComparer.Ordinal).ToList();
    var extra = mine.Keys.Where(id => !committed.ContainsKey(id))
        .OrderBy(id => id, StringComparer.Ordinal).ToList();

    int same = committed.Count(pair =>
        mine.TryGetValue(pair.Key, out var record)
        && string.Equals(record.Fragment, pair.Value, StringComparison.Ordinal));

    Console.WriteLine($"harvested {entries.Count} entries, {mine.Count} records");
    Console.WriteLine($"committed {committed.Count} records");
    Console.WriteLine($"  {same} fragments identical");
    Console.WriteLine($"  {missing.Count} records the committed dataset has and this does not");
    Console.WriteLine($"  {extra.Count} records this has and the committed dataset does not");

    foreach (string id in missing)
    {
        Console.WriteLine($"  missing  {id}");
    }

    foreach (string id in extra)
    {
        Console.WriteLine($"  extra    {id}");
    }

    return missing.Count == 0 && extra.Count == 0 ? 0 : 1;
}
