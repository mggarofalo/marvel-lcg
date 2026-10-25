using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Tests;

namespace Marvel.Content.Tests.Play;

internal static class AvengersSupportCards
{
    internal static AbilityRunner Runner() => new(AbilityCatalog.Parse(Json));
    internal static readonly string Json = Read();

    private static string Read()
    {
        var book = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var cards = new JsonArray();
        foreach (var row in book["cards"]!.AsArray())
            if (row!["card"]!.GetValue<string>() is "04021" or "04022" or "03024")
                cards.Add(row.DeepClone());
        return new JsonObject { ["cards"] = cards }.ToJsonString();
    }
}
