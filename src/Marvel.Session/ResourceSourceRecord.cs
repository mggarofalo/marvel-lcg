using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Session;

/// <summary>One resource generator in a durable cost menu.</summary>
public readonly record struct ResourceSourceRecord(
    [property: JsonRequired] int Effect,
    [property: JsonRequired] string Generates)
{
    /// <summary>The authored ability name retained with the offered generator.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? AbilityName { get; init; }

    /// <summary>The non-resource commitments shown before selecting this generator.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CostDescription { get; init; }

    /// <summary>Captures one engine-authored generator.</summary>
    public static ResourceSourceRecord From(ResourceSource source) =>
        new(source.Effect, source.Generates)
        {
            AbilityName = source.AbilityName,
            CostDescription = source.CostDescription,
        };
}
