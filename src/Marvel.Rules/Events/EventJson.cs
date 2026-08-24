using System.Text.Json;

namespace Marvel.Rules.Events;

/// <summary>How the event stream is spelled on the wire.</summary>
/// <remarks>
/// <para>
/// Snake case, because the vocabulary contract in
/// <c>datasets/events/vocabulary.json</c> is written by the Python side and
/// names its keys <c>face_up</c>, <c>from</c>, <c>to</c>. Two implementations
/// agreeing on the kinds but not on the key spelling would be a contract that
/// passes its own test and fails in the field.
/// </para>
/// <para>
/// Unlike the state digest, byte equality is <b>not</b> required here: events
/// are consumed by a client, not compared between engines. What matters is that
/// the names match, which <c>EventVocabularyTests</c> checks against the
/// fixture.
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
