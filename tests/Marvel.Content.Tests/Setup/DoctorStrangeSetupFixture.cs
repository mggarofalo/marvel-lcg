using System.Text.Json.Nodes;
using Marvel.Content.Setup;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;

namespace Marvel.Content.Tests.Setup;

/// <summary>Independent mc08 divider recipe for typed setup and malformed-binding probes.</summary>
internal static class DoctorStrangeSetupFixture
{
    internal static readonly string[] Invocations = ["09032", "09033", "09034", "09035", "09036"];
    internal static readonly string[] Signature =
    [
        "09002", "09003", "09003", "09004", "09004", "09005", "09005", "09006",
        "09007", "09007", "09008", "09009", "09010", "09010", "09011",
    ];
    internal static readonly string[] Extras =
    [
        "09012", "09013", "09014", "09015", "09015", "09015", "09016", "09016", "09016",
        "09017", "09017", "09018", "09018", "09019", "09020", "09020", "09020",
        "09021", "09021", "09021", "09022", "09023", "09024", "09025", "09026",
    ];
    internal static readonly CardCatalog PrintedCards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    internal static JsonObject Root()
    {
        var root = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")))!.AsObject();
        root["heroes"]!["doctor_strange"] = new JsonObject
        {
            ["name"] = "Doctor Strange", ["hero"] = Array(["09001a,09001b"]),
            ["hero_deck"] = Array(Signature), ["player_deck"] = Array(Extras),
            ["obligations"] = Array(["09027"]),
            ["nemesis_set"] = Array(["09028", "09029", "09030", "09030", "09031"]),
            ["nemesis_set_icon"] = "doctor_strange_nemesis", ["invocation_deck"] = Array(Invocations),
        };
        return root;
    }

    internal static JsonArray Array(IEnumerable<string> cards) =>
        new(cards.Select(card => (JsonNode?)JsonValue.Create(card)).ToArray());

    private static readonly string[] OtherHeroes = ["spider_man", "captain_marvel", "she_hulk"];

    internal static string[] Heroes(int count) =>
        OtherHeroes.Take(count - 1).Append("doctor_strange").ToArray();

    internal static IReadOnlyList<CardBlueprint> Blueprints(int count) =>
        Marvel.Rules.Setup.Blueprints.From(Dealer.DealOrder(SetupCatalog.Parse(Root().ToJsonString()),
            "rhino", Heroes(count), facts: PrintedCards), PrintedCards);
}
