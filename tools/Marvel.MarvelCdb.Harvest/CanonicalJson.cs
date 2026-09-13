using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Marvel.MarvelCdb.Harvest;

internal static partial class CanonicalJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string String(string value) => JsonSerializer.Serialize(value, Options);

    public static string Entry(JsonElement entry)
    {
        string[] first = ["code", "html", "text", "updated"];
        var properties = entry.EnumerateObject().ToDictionary(
            property => property.Name,
            property => property.Value,
            StringComparer.Ordinal);
        var names = first.Where(properties.ContainsKey)
            .Concat(properties.Keys.Except(first, StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal));
        return "{" + string.Join(", ", names.Select(name =>
            $"{String(name)}: {Element(properties[name])}")) + "}";
    }

    private static string Element(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => "{" + string.Join(", ", element.EnumerateObject().Select(
            property => $"{String(property.Name)}: {Element(property.Value)}")) + "}",
        JsonValueKind.Array => "[" + string.Join(", ", element.EnumerateArray().Select(Element)) + "]",
        JsonValueKind.String => String(element.GetString() ?? string.Empty),
        JsonValueKind.Number => element.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => "null",
        _ => throw new InvalidDataException($"unsupported JSON value {element.ValueKind}"),
    };
}
