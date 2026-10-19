using System.Globalization;
using System.Text.Json;
using Marvel.Tests;

namespace Marvel.Cards.Extract;

/// <summary>Generates acquisition metadata for search without changing gameplay dataset hashes.</summary>
internal static class SearchMetadata
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    internal static string Path => RepositoryPaths.Dataset("cards", "search.json");

    internal static string Build(string canonicalCards)
    {
        var all = SdbCard.ReadAll();
        using var packs = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("marvelsdb", "packs.json")));
        var dates = packs.RootElement.EnumerateArray().ToDictionary(pack => pack.GetProperty("code").GetString()!,
            pack => pack.GetProperty("date_release").GetString() ?? "", StringComparer.Ordinal);
        var names = packs.RootElement.EnumerateArray().ToDictionary(pack => pack.GetProperty("code").GetString()!,
            pack => pack.GetProperty("name").GetString() ?? "", StringComparer.Ordinal);
        var positions = packs.RootElement.EnumerateArray().ToDictionary(pack => pack.GetProperty("code").GetString()!,
            pack => pack.GetProperty("position").GetInt32().ToString(CultureInfo.InvariantCulture), StringComparer.Ordinal);
        var result = new SortedDictionary<string, SortedDictionary<string, string>>(StringComparer.Ordinal);
        foreach (var (code, source) in all)
        {
            var card = source.Resolved(all);
            var fields = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, field) in new (string, string)[] {
                ("v", "flavor"), ("l", "illustrator"), ("b", "subtype_code"), ("e", "pack_code"),
                ("m", "set_code"), ("t", "type_code"), ("f", "faction_code") })
                if (card.Text(field) is { } value) fields[key] = value;
            foreach (var (key, field) in new (string, string)[] {
                ("qt", "quantity"), ("at", "attack"), ("th", "thwart"), ("df", "defense"), ("rc", "recover"),
                ("h", "health"), ("o", "cost"), ("rm", "resource_mental"), ("rp", "resource_physical"),
                ("re", "resource_energy"), ("rw", "resource_wild") })
                if (card.Number(field) is { } value) fields[key] = value.ToString(CultureInfo.InvariantCulture);
            foreach (string resource in new[] { "rm", "rp", "re", "rw" }) fields.TryAdd(resource, "0");
            fields["u"] = card.Flag("is_unique") ? "1" : "0";
            fields["en"] = names.GetValueOrDefault(card.Text("pack_code") ?? "", "");
            fields["ep"] = positions.GetValueOrDefault(card.Text("pack_code") ?? "", "0");
            fields["r"] = dates.GetValueOrDefault(card.Text("pack_code") ?? "", "");
            result[code] = fields;
        }
        ApplyCanonicalFacts(result, canonicalCards);
        return JsonSerializer.Serialize(result, Options)
            .Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
    }
    private static void ApplyCanonicalFacts(SortedDictionary<string, SortedDictionary<string, string>> metadata, string json)
    {
        using var document = JsonDocument.Parse(json);
        foreach (var card in document.RootElement.GetProperty("cards").EnumerateArray())
        {
            string id = card.GetProperty("card_id").GetString()!;
            if (!metadata.TryGetValue(id, out var fields)) metadata[id] = fields = new(StringComparer.Ordinal);
            var attributes = card.GetProperty("attributes");
            foreach (var (key, attribute) in new (string, string)[] {
                ("at", "ATK"), ("th", "THW"), ("df", "DEF"), ("rc", "REC"), ("h", "HP"), ("o", "Cost") })
            {
                fields.Remove(key);
                if (attributes.TryGetProperty(attribute, out var value)) fields[key] = value.GetString()!;
            }
            fields["u"] = attributes.TryGetProperty("Unique", out var unique) ? unique.GetString()! : "0";
            string resources = attributes.TryGetProperty("RES", out var printed) ? printed.GetString()! : "";
            foreach (var (key, symbol) in new (string, char)[] { ("rp", 'R'), ("re", 'Y'), ("rm", 'B'), ("rw", 'G') })
                fields[key] = resources.Count(character => character == symbol).ToString(CultureInfo.InvariantCulture);
            fields["e"] = card.GetProperty("pack").GetString()!;
            fields["m"] = card.GetProperty("set").GetString()!;
        }
    }

}
