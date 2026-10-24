using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Tests;

namespace Marvel.Content.Tests.Setup;

/// <summary>Real printed component recipes, isolated from public whole-pack admission.</summary>
internal static class WreckingRecipeFixture
{
    internal static readonly string[][] Decks =
    [
        ["07005", "07006", "07007", "07008", "07009", "07010", "07011", "07012", "07012", "07013", "07014", "07015", "07015", "07016", "07016"],
        ["07020", "07021", "07022", "07023", "07024", "07025", "07026", "07027", "07027", "07028", "07029", "07030", "07030", "07031", "07031"],
        ["07035", "07035", "07036", "07037", "07038", "07039", "07040", "07041", "07042", "07042", "07043", "07043", "07044", "07044", "07045"],
        ["07049", "07050", "07051", "07051", "07052", "07053", "07054", "07055", "07055", "07056", "07057", "07058", "07058", "07059", "07059"],
    ];
    internal static readonly string[][] Stages = [["07002", "07003"], ["07017", "07018"], ["07032", "07033"], ["07046", "07047"]];
    internal static readonly string[] Schemes = ["07004", "07019", "07034", "07048"];

    internal static JsonObject Document(int versions = 0, bool extreme = false)
    {
        var root = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")))!.AsObject();
        var groups = new JsonArray();
        var villains = new List<string>();
        for (int group = 0; group < 4; group++)
        {
            string[] stages = extreme ? Stages[group] : [Stages[group][(versions >> group) & 1]];
            villains.AddRange(stages);
            groups.Add(new JsonObject
            {
                ["stages"] = Array(stages), ["scheme"] = Schemes[group], ["encounters"] = Array(Decks[group]),
            });
        }
        root["campaigns"]!["wrecking_fixture"] = new JsonObject
        {
            ["name"] = "Wrecking Crew component recipe", ["villain"] = Array(villains), ["expert"] = versions == 15,
            ["challenges"] = new JsonArray(), ["schemes"] = Array(["07001a,07001b"]),
            ["set_aside"] = new JsonArray(), ["encounters"] = new JsonArray(),
            ["encounter_sets"] = new JsonArray(), ["modular_sets"] = new JsonArray(),
            ["encounter_groups"] = new JsonObject { ["active_group"] = 0, ["groups"] = groups },
        };
        return root;
    }

    internal static SetupCatalog Catalog(int versions = 0, bool extreme = false) =>
        SetupCatalog.Parse(Document(versions, extreme).ToJsonString());

    private static JsonArray Array(IEnumerable<string> values) => new(values.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray());
}
