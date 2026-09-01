using System.Text;
using System.Text.Json;
using Marvel.Tests;

namespace Marvel.Behavior.Index;

/// <summary>Generates the mechanical printed-fact transcript for every Core face.</summary>
internal static class CardFaceSpecifications
{
    public static string Path { get; } = RepositoryPaths.Repository(
        "specs", "behavior", "core", "card-faces.feature");

    public static void Write()
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        File.WriteAllText(Path, Build(), new UTF8Encoding(false));
    }

    public static void Check()
    {
        if (!File.Exists(Path))
        {
            throw new InvalidDataException(
                "specs/behavior/core/card-faces.feature is absent; run Marvel.Behavior.Index write");
        }

        if (!string.Equals(File.ReadAllText(Path), Build(), StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "specs/behavior/core/card-faces.feature is stale; run Marvel.Behavior.Index write");
        }
    }

    internal static string Build()
    {
        CatalogFile catalog = Catalog.Build();
        var sources = catalog.Sources
            .Where(source => source.Kind == "card")
            .ToDictionary(source => source.Id["card:".Length..], StringComparer.Ordinal);
        using JsonDocument cards = ReadDataset("cards", "cards.json");
        using JsonDocument setup = ReadDataset("setup", "setup.json");
        var output = new StringBuilder();
        output.AppendLine("@core");
        output.AppendLine("Feature: Canonical Core printed card faces");
        output.AppendLine("  Each scenario places the named physical face in a legal Core deal and");
        output.AppendLine("  checks the structured facts generated from that same printed authority.");

        int ordinal = 0;
        foreach (JsonElement card in cards.RootElement.GetProperty("cards").EnumerateArray()
                     .Where(card => card.GetProperty("pack").GetString() == "core"))
        {
            string id = card.GetProperty("card_id").GetString()!;
            CatalogEntry source = sources[id];
            var fields = PrintedFields(card);
            var obligations = fields
                .Select(field => source.Obligations.Single(obligation =>
                    string.Equals(obligation.Id, $"behavior:card:{id}:{field.Key}",
                        StringComparison.Ordinal)))
                .ToList();
            Deal deal = FindDeal(id, setup.RootElement);

            output.AppendLine();
            output.AppendLine($"  @{obligations[0].Id}");
            foreach (CatalogObligation obligation in obligations.Skip(1))
            {
                output.AppendLine($"  @covers:{obligation.Id}");
            }

            output.AppendLine($"  @card:{id}");
            output.AppendLine($"  Scenario: Card {id} exposes its printed face");
            output.AppendLine("    # The table is generated from this face's canonical cards.json record;");
            output.AppendLine("    # the engine must expose each fact without reinterpretation.");
            output.AppendLine("    Given a canonical Core scene is dealt");
            if (deal.ModularSet is null)
            {
                output.AppendLine("      | campaign | heroes | seed |");
                output.AppendLine(
                    $"      | {deal.Campaign} | {deal.Hero} | {500 + ordinal} |");
            }
            else
            {
                output.AppendLine("      | campaign | heroes | modular sets | seed |");
                output.AppendLine(
                    $"      | {deal.Campaign} | {deal.Hero} | {deal.ModularSet} | {500 + ordinal} |");
            }

            output.AppendLine(
                $"    When the printed characteristics of card {id} copy 0 are requested");
            output.AppendLine($"    Then card {id} copy 0 exposes these printed characteristics");
            output.AppendLine("      | field | value |");
            foreach (PrintedField field in fields)
            {
                output.AppendLine($"      | {field.Field} | {field.Value} |");
            }

            ordinal++;
        }

        // The generated feature is a repository artifact, so its line endings
        // are part of that artifact rather than a choice made by the host OS.
        return output.ToString().Replace(Environment.NewLine, "\n", StringComparison.Ordinal);
    }

    private static List<PrintedField> PrintedFields(JsonElement card)
    {
        var fields = new List<PrintedField>
        {
            new("printed-name", "name", card.GetProperty("name").GetString()!),
            new("printed-type", "type", card.GetProperty("type").GetString()!),
        };
        if (card.TryGetProperty("subname", out JsonElement subtitle)
            && subtitle.GetString() is { Length: > 0 } subtitleText)
        {
            fields.Insert(1, new PrintedField("printed-subtitle", "subtitle", subtitleText));
        }

        if (card.TryGetProperty("traits", out JsonElement traits)
            && traits.ValueKind == JsonValueKind.Array
            && traits.GetArrayLength() > 0)
        {
            fields.Add(new PrintedField(
                "printed-traits",
                "traits",
                string.Join('/', traits.EnumerateArray().Select(value => value.GetString()))));
        }

        foreach (JsonProperty attribute in card.GetProperty("attributes").EnumerateObject())
        {
            string suffix = new(attribute.Name
                .Where(character => char.IsAsciiLetterOrDigit(character))
                .Select(char.ToLowerInvariant)
                .ToArray());
            fields.Add(new PrintedField(
                $"printed-{suffix}", $"attribute:{attribute.Name}", attribute.Value.GetString()!));
        }

        return fields;
    }

    private static Deal FindDeal(string id, JsonElement root)
    {
        foreach (JsonProperty hero in root.GetProperty("heroes").EnumerateObject())
        {
            if (ContainsCard(hero.Value, id))
            {
                return new Deal("rhino", hero.Name, null);
            }
        }

        foreach (JsonProperty campaign in root.GetProperty("campaigns").EnumerateObject())
        {
            if (ContainsCard(campaign.Value, id))
            {
                return new Deal(campaign.Name, "spider_man", null);
            }
        }

        foreach (JsonProperty encounterSet in root.GetProperty("encounter_sets").EnumerateObject())
        {
            if (!ContainsCard(encounterSet.Value, id))
            {
                continue;
            }

            foreach (JsonProperty campaign in root.GetProperty("campaigns").EnumerateObject())
            {
                if (NamesSet(campaign.Value, "encounter_sets", encounterSet.Name)
                    || NamesSet(campaign.Value, "modular_sets", encounterSet.Name))
                {
                    return new Deal(campaign.Name, "spider_man", null);
                }
            }

            return new Deal("rhino", "spider_man", encounterSet.Name);
        }

        throw new InvalidDataException(
            $"Core card {id} is not present in any legal Core setup component");
    }

    private static bool ContainsCard(JsonElement record, string id) =>
        record.EnumerateObject()
            .Where(property => property.Value.ValueKind == JsonValueKind.Array)
            .SelectMany(property => property.Value.EnumerateArray())
            .Where(value => value.ValueKind == JsonValueKind.String)
            .SelectMany(value => value.GetString()!.Split(','))
            .Contains(id, StringComparer.Ordinal);

    private static bool NamesSet(JsonElement record, string property, string name) =>
        record.TryGetProperty(property, out JsonElement sets)
        && sets.EnumerateArray().Any(value => value.GetString() == name);

    private static JsonDocument ReadDataset(params string[] parts) =>
        JsonDocument.Parse(File.ReadAllBytes(RepositoryPaths.Dataset(parts)));

    private sealed record PrintedField(string Key, string Field, string Value);

    private sealed record Deal(string Campaign, string Hero, string? ModularSet);
}
