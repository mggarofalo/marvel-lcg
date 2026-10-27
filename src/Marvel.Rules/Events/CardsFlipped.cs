using System.Text.Json.Serialization;

namespace Marvel.Rules.Events;

/// <summary>Cards turning face up or face down.</summary>
/// <param name="Cards">Object ids.</param>
/// <param name="FaceUp">Their new state.</param>
public sealed record CardsFlipped(IReadOnlyList<int> Cards, bool FaceUp) : GameEvent
{
    /// <summary>Engine-captured faces publicly exposed by this flip, even if concealed again in the same result.</summary>
    /// <remarks>
    /// Engine choice: this event-local disclosure authority is reconstructed during replay.
    /// It grants no current location, subsequent movement or concealed ordering and is not a wire/save field.
    /// </remarks>
    [JsonIgnore]
    public IReadOnlyDictionary<int, string>? PublicFaces { get; init; }
}
