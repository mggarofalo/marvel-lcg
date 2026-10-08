using System.Text.Json;
using Marvel.Rules.State;

namespace Marvel.Content;

/// <summary>Combines engine attributes with source facts that their numeric notation cannot express.</summary>
internal static class CardCatalogPrintedStats
{
    internal static IReadOnlyDictionary<string, PrintedStatValue> Read(
        JsonElement card, CardKind kind, IReadOnlyDictionary<string, string> attributes)
    {
        var values = new Dictionary<string, PrintedStatValue>(
            PrintedStatFacts.From(kind, attributes), StringComparer.Ordinal);
        if (!card.TryGetProperty("stat_annotations", out JsonElement annotations)) return values;
        foreach (JsonProperty annotation in annotations.EnumerateObject())
        {
            JsonElement source = annotation.Value;
            PrintedStatValue original = values.GetValueOrDefault(annotation.Name)
                ?? new PrintedStatValue("", false, false, 0);
            string value = source.TryGetProperty("value", out JsonElement printed)
                ? printed.GetString() ?? "" : original.Value;
            if (value.Length == 0)
                throw new FormatException($"Printed stat '{annotation.Name}' has no value.");
            values[annotation.Name] = original with
            {
                Value = value,
                SpecialStar = source.TryGetProperty("special_star", out JsonElement star)
                    && star.GetBoolean(),
                PerPlayer = source.TryGetProperty("per_player", out JsonElement perPlayer)
                    ? perPlayer.GetBoolean() : original.PerPlayer,
            };
        }
        return values;
    }
}
