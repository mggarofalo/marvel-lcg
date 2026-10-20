using System.Text.Json.Nodes;
using Marvel.Tests;

namespace Marvel.Content.Tests.Play;

/// <summary>Canonical Absorbing Man abilities isolated for the synthetic encounter fixtures.</summary>
internal static class AbsorbingManFragment
{
    internal static readonly string Json = Read();

    private static string Read()
    {
        var book = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var cards = new JsonArray();
        foreach (var row in book["cards"]!.AsArray())
        {
            string face = row!["card"]!.GetValue<string>();
            if (string.CompareOrdinal(face, "04076") >= 0 && string.CompareOrdinal(face, "04092") <= 0)
                cards.Add(row.DeepClone());
        }
        return new JsonObject { ["cards"] = cards }.ToJsonString();
    }
}
