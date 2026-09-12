using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Session;

/// <summary>Canonical JSON settings for deterministic journal value records.</summary>

/// <summary>One numerical value requested by a durable cost.</summary>
public readonly record struct VariableRequestRecord(
    [property: JsonRequired] string Name,
    [property: JsonRequired] long Min,
    [property: JsonRequired] long Max)
{
    /// <summary>Captures one engine-authored variable range.</summary>
    public static VariableRequestRecord From(VariableRequest request) =>
        new(request.Name, request.Min, request.Max);
}
