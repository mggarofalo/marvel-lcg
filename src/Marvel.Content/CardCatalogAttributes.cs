using System.Text.Json;

namespace Marvel.Content;

/// <summary>Reads structured printed attributes and preserves their display spelling.</summary>
internal static class CardCatalogAttributes
{
    public static void ReadTraits(
        JsonElement element, List<string> traits, List<string> labels)
    {
        if (!element.TryGetProperty("traits", out JsonElement values)
            || values.ValueKind != JsonValueKind.Array)
        {
            return;
        }
        foreach (JsonElement value in values.EnumerateArray())
        {
            if (value.GetString() is { Length: > 0 } text)
            {
                labels.Add(text);
                traits.Add(CardCatalog.TraitKey(text));
            }
        }
    }

    public static Dictionary<string, string> ReadAttributes(JsonElement element)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!element.TryGetProperty("attributes", out JsonElement values)
            || values.ValueKind != JsonValueKind.Object)
        {
            return result;
        }
        foreach (JsonProperty attribute in values.EnumerateObject())
        {
            result[attribute.Name] = attribute.Value.GetString() ?? string.Empty;
        }
        return result;
    }

}
