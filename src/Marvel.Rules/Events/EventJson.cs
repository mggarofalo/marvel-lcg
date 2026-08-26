using System.Text.Json;

namespace Marvel.Rules.Events;

/// <summary>How the event stream is spelled on the wire.</summary>
/// <remarks>
/// <para>
/// Snake case, spelling its keys <c>face_up</c>, <c>from</c>, <c>to</c>. <b>Our
/// choice, not a rule</b> — no published rule says anything about a wire
/// format. What matters is that it holds still, so treat it as fixed.
/// </para>
/// <para>
/// Byte equality is <b>not</b> required here: events are consumed by a client
/// rather than compared. What matters is that the names hold, and nothing
/// checks that they do — MARVEL-251.
/// </para>
/// </remarks>
public static class EventJson
{
    /// <summary>The canonical options for reading and writing events.</summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false,
    };

    /// <summary>Serialises one event, discriminator included.</summary>
    public static string Write(GameEvent value) =>
        JsonSerializer.Serialize(value, Options);

    /// <summary>Reads an event back, dispatching on its <c>kind</c>.</summary>
    public static GameEvent Read(string json) =>
        JsonSerializer.Deserialize<GameEvent>(json, Options)
        ?? throw new JsonException("event document was null");
}
