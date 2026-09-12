using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Session;

/// <summary>Canonical JSON settings for deterministic journal value records.</summary>

/// <summary>The independent values in one durable target request.</summary>
public sealed record TargetRequestRecord(
    [property: JsonRequired] IReadOnlyList<int> Legal,
    [property: JsonRequired] int Min,
    [property: JsonRequired] int Max,
    [property: JsonRequired] IReadOnlyList<IReadOnlyList<int>>? Groups,
    [property: JsonRequired] IReadOnlyList<string>? MustIncludeTraits,
    [property: JsonRequired] string Rule,
    [property: JsonRequired] bool IsSearch,
    [property: JsonRequired] bool AllowRepeated,
    [property: JsonRequired] IReadOnlyDictionary<int, int>? MaximumOccurrences,
    [property: JsonRequired] IReadOnlyDictionary<int, string>? Details)
{
    [JsonIgnore]
    internal bool LegacyAllowRepeatedRecorded { get; init; } = true;

    [JsonIgnore]
    internal bool LegacyMaximumOccurrencesRecorded { get; init; } = true;

    [JsonIgnore]
    internal bool LegacyDetailsRecorded { get; init; } = true;

    /// <summary>Captures source fields without the derived grouped alias.</summary>
    public static TargetRequestRecord From(TargetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new(
            request.Legal,
            request.Min,
            request.Max,
            request.Groups,
            request.MustIncludeTraits,
            request.Rule,
            request.IsSearch,
            request.AllowRepeated,
            request.MaximumOccurrences,
            request.Details);
    }
}
