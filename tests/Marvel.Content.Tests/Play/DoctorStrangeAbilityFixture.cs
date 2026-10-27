using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;

namespace Marvel.Content.Tests.Play;

/// <summary>Production programs with explicit synthetic replacements for nonpublic controls.</summary>
internal static class DoctorStrangeAbilityFixture
{
    internal static AbilityRunner WithReplacements(JsonArray replacements)
    {
        var root = JsonNode.Parse(AuthoredCards.Text)!;
        var rows = root["cards"]!.AsArray();
        foreach (var replacement in replacements)
        {
            string face = replacement!["card"]!.GetValue<string>();
            var existing = rows.SingleOrDefault(row => row!["card"]!.GetValue<string>() == face);
            if (existing is not null) rows.Remove(existing);
            rows.Add(replacement.DeepClone());
        }
        return new AbilityRunner(AbilityCatalog.Parse(root.ToJsonString()));
    }
}
