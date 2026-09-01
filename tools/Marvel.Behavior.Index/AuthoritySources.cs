using System.Text.Json;
using Marvel.Rules.Index;
using Marvel.Tests;

namespace Marvel.Behavior.Index;

/// <summary>One canonical authority unit before behavioral adjudication.</summary>
internal sealed record AuthoritySource(
    string Id,
    string Kind,
    string Title,
    string Fingerprint,
    string Scope,
    string Text);

/// <summary>Enumerates the closed authority universes in contract order.</summary>
internal static class AuthoritySources
{
    private static readonly Dictionary<string, int> KindOrder =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["rule"] = 0,
            ["card"] = 1,
            ["faq"] = 2,
            ["ruling"] = 3,
            ["setup"] = 4,
        };

    /// <summary>Every source unit, in the only order generated artifacts use.</summary>
    public static IReadOnlyList<AuthoritySource> Read()
    {
        var sources = new List<AuthoritySource>();
        AddRules(sources);
        AddCards(sources);
        AddFaq(sources);
        AddRulings(sources);
        AddSetup(sources);

        var duplicate = sources
            .GroupBy(source => source.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidDataException(
                $"authority source id {duplicate.Key} occurs {duplicate.Count()} times");
        }

        return
        [
            .. sources
                .OrderBy(source => KindOrder[source.Kind])
                .ThenBy(source => source.Id, StringComparer.Ordinal),
        ];
    }

    private static void AddRules(List<AuthoritySource> sources)
    {
        var corpus = Corpus.Read();
        foreach (var record in corpus.Records.Where(record => record.Kind == "base"))
        {
            var effective = corpus.Resolve(record.Id, corpus.Version);
            sources.Add(new AuthoritySource(
                record.Id,
                "rule",
                record.Title,
                effective.Hash,
                $"Rules Reference v{corpus.Version}",
                effective.Fragment));
        }
    }

    private static void AddCards(List<AuthoritySource> sources)
    {
        using var document = ReadDataset("cards", "cards.json");
        AddCards(sources, document.RootElement.GetProperty("cards"));
    }

    internal static void AddCards(
        List<AuthoritySource> sources, JsonElement cards)
    {
        foreach (var card in cards.EnumerateArray())
        {
            if (!string.Equals(card.GetProperty("pack").GetString(), "core", StringComparison.Ordinal))
            {
                continue;
            }

            string id = card.GetProperty("card_id").GetString()!;
            string name = card.GetProperty("name").GetString()!;
            sources.Add(new AuthoritySource(
                $"card:{id}",
                "card",
                name,
                CanonicalJson.Hash(card),
                "Core Set",
                card.GetProperty("text_plain").GetString() ?? ""));
        }
    }

    private static void AddFaq(List<AuthoritySource> sources)
    {
        using var document = ReadDataset("marvelcdb-faq", "faq.json");
        var entries = document.RootElement.GetProperty("entries").EnumerateArray()
            .Select(entry => entry.Clone())
            .ToList();
        var counts = entries
            .GroupBy(entry => entry.GetProperty("code").GetString()!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var ordinals = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            string code = entry.GetProperty("code").GetString()!;
            int ordinal = ordinals.GetValueOrDefault(code) + 1;
            ordinals[code] = ordinal;
            string id = counts[code] == 1 ? $"faq:{code}" : $"faq:{code}:{ordinal}";
            sources.Add(new AuthoritySource(
                id,
                "faq",
                code,
                CanonicalJson.Hash(entry),
                "MarvelCDB FAQ",
                entry.GetProperty("text").GetString() ?? ""));
        }
    }

    private static void AddRulings(List<AuthoritySource> sources)
    {
        using var document = ReadDataset("rulings", "rulings.json");
        foreach (var ruling in document.RootElement.GetProperty("rulings").EnumerateArray())
        {
            string id = ruling.GetProperty("id").GetString()!;
            string question = ruling.GetProperty("question").GetString()!;
            string fingerprint = ruling.GetProperty("hash").GetString()!;
            string section = ruling.GetProperty("section").GetString()!;
            sources.Add(new AuthoritySource(
                id,
                "ruling",
                question,
                fingerprint,
                section,
                question + "\n\n" + ruling.GetProperty("answer").GetString()!));
        }
    }

    private static void AddSetup(List<AuthoritySource> sources)
    {
        using var document = ReadDataset("setup", "setup.json");
        foreach (string collection in new[] { "campaigns", "heroes", "encounter_sets" })
        {
            string singular = collection switch
            {
                "campaigns" => "campaign",
                "heroes" => "hero",
                "encounter_sets" => "encounter-set",
                _ => throw new InvalidOperationException(),
            };
            foreach (var record in document.RootElement.GetProperty(collection).EnumerateObject())
            {
                string title = record.Value.TryGetProperty("name", out var name)
                    ? name.GetString() ?? record.Name
                    : record.Name;
                sources.Add(new AuthoritySource(
                    $"setup:{singular}:{record.Name}",
                    "setup",
                    title,
                    CanonicalJson.Hash(record.Value),
                    singular,
                    CanonicalJson.Serialize(record.Value)));
            }
        }
    }

    private static JsonDocument ReadDataset(params string[] parts) =>
        JsonDocument.Parse(File.ReadAllBytes(RepositoryPaths.Dataset(parts)));
}
