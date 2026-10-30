using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;

namespace Marvel.Content.Tests.Play;

// Selected production rows on synthetic boards; this does not admit the pack.
internal static class AntManF1Fixture
{
    internal static AbilityRunner Runner(string[] faces, string? synthetic = null)
    {
        var source = JsonNode.Parse(AuthoredCards.Text)!["cards"]!.AsArray();
        var selected = new JsonArray();
        foreach (var row in source)
            if (faces.Contains(row!["card"]!.GetValue<string>(), StringComparer.Ordinal))
                selected.Add(row.DeepClone());
        if (synthetic is not null) selected.Add(JsonNode.Parse(synthetic));
        return new AbilityRunner(AbilityCatalog.Parse(new JsonObject { ["cards"] = selected }.ToJsonString()));
    }
}
