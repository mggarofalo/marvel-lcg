using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Session;

/// <summary>Canonical JSON settings for deterministic journal value records.</summary>

/// <summary>One resource generator in a durable cost menu.</summary>
public readonly record struct ResourceSourceRecord(
    [property: JsonRequired] int Effect,
    [property: JsonRequired] string Generates)
{
    /// <summary>Captures one engine-authored generator.</summary>
    public static ResourceSourceRecord From(ResourceSource source) =>
        new(source.Effect, source.Generates);
}
