using System.Text.Json;

namespace Marvel.Cards.Extract;

/// <summary>Preserves structured source symbols and stars without interpreting card text.</summary>
internal static class StatAnnotations
{
    private static readonly (string Field, string Attribute)[] Fields =
    [
        ("attack", "ATK"), ("thwart", "THW"), ("defense", "DEF"),
        ("scheme", "SCH"), ("recover", "REC"), ("hand_size", "HS"),
        ("health", "HP"), ("cost", "Cost"), ("boost", "Boost"),
        ("base_threat", "StartingThreat"), ("threat", "TargetThreat"),
        ("escalation_threat", "EscalationThreat"),
    ];

    internal static IReadOnlyDictionary<string, StatAnnotation> From(
        SdbCard source, string kind, IReadOnlyDictionary<string, string> attributes)
    {
        var result = new SortedDictionary<string, StatAnnotation>(StringComparer.Ordinal);
        foreach ((string field, string name) in Fields)
        {
            string attribute = kind == "Attachment" && name is "ATK" or "SCH" or "THW"
                ? name + "+" : name;
            StatAnnotation? annotation = ReadSource(source, kind, field, name,
                attributes.ContainsKey(attribute));
            if (annotation is not null) result[attribute] = annotation;
        }
        return result;
    }

    private static StatAnnotation? ReadSource(
        SdbCard source, string kind, string field, string name, bool numeric)
    {
        bool special = source.Flag(field + "_star");
        bool symbolic = PrintedNumbers.HasStat(kind, name) && source.Has(field)
            && source.Number(field) is null;
        if (!numeric && !symbolic && !special) return null;
        string? value = Symbol(source, field, name, special, symbolic);
        if (value is null && !special) return null;
        // cost_star is a special reminder, not a per-player icon. Engine
        // attributes retain their own notation; display facts disambiguate it.
        return new StatAnnotation(value, special, name == "Cost" ? false : null);
    }

    private static string? Symbol(SdbCard source, string field, string name, bool special, bool symbolic)
    {
        // The source uses -1 for X; a present null character field is a
        // dash, or a star-valued field when its structured star flag is set.
        if (special && source.Number(field) is null) return name == "Boost" ? "0" : "★";
        if (symbolic) return "—";
        if (name == "Cost") return null; // The numeric extractor already preserves cost X.
        return source.Number(field) == -1 ? "X" : null;
    }

    internal static void Write(Utf8JsonWriter writer, IReadOnlyDictionary<string, StatAnnotation> annotations)
    {
        if (annotations.Count == 0) return;
        writer.WriteStartObject("stat_annotations");
        foreach ((string key, StatAnnotation annotation) in annotations)
        {
            writer.WriteStartObject(key);
            if (annotation.Value is { } value) writer.WriteString("value", value);
            if (annotation.SpecialStar) writer.WriteBoolean("special_star", true);
            if (annotation.PerPlayer is { } perPlayer) writer.WriteBoolean("per_player", perPlayer);
            writer.WriteEndObject();
        }
        writer.WriteEndObject();
    }

    internal static IReadOnlyDictionary<string, StatAnnotation> Read(JsonElement card)
    {
        var result = new SortedDictionary<string, StatAnnotation>(StringComparer.Ordinal);
        if (!card.TryGetProperty("stat_annotations", out JsonElement annotations)) return result;
        foreach (JsonProperty annotation in annotations.EnumerateObject())
        {
            JsonElement source = annotation.Value;
            result[annotation.Name] = new StatAnnotation(
                source.TryGetProperty("value", out JsonElement value) ? value.GetString() : null,
                source.TryGetProperty("special_star", out JsonElement star) && star.GetBoolean(),
                source.TryGetProperty("per_player", out JsonElement perPlayer) ? perPlayer.GetBoolean() : null);
        }
        return result;
    }
}
