using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;

namespace Marvel.Content.Tests.Play;

internal static class DoctorStrangeMagicBlastFixture
{
    internal static AbilityRunner Runner(bool sequentialControl = false)
    {
        if (!sequentialControl) return AuthoredCards.Runner();
        // Explicit synthetic comparison: replace AND with uninterrupted sequencing.
        var root = JsonNode.Parse(AuthoredCards.Text)!;
        var row = root["cards"]!.AsArray().Single(r => (string?)r!["card"] == "09004")!;
        var body = row["abilities"]![0]!["effect"]!["chooseCard"]!["effect"]!["attack"]!["effect"]!["seq"]![0]!.AsObject();
        var children = body["and"]!.DeepClone();
        body.Remove("and");
        body["seq"] = children;
        return new(AbilityCatalog.Parse(root.ToJsonString()));
    }
}
