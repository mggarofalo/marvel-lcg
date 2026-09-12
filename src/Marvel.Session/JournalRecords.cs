using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Session;

/// <summary>Canonical JSON settings for deterministic journal value records.</summary>
public static class JournalJson
{
    /// <summary>Uses the snake-case spelling already pinned by simulation schema 2.</summary>
    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false,
        };
        options.Converters.Add(new ResourceAllocationJsonConverter());
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }

    /// <summary>Captures one semantic event without presentation-only evidence.</summary>
    public static JsonElement Event(GameEvent happened)
    {
        ArgumentNullException.ThrowIfNull(happened);
        return JsonSerializer.SerializeToElement<GameEvent>(
            happened with { Subjects = null }, EventJson.Options);
    }
}
