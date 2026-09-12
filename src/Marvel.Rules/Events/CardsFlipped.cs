using System.Text.Json.Serialization;

namespace Marvel.Rules.Events;

/// <summary>Cards turning face up or face down.</summary>
/// <param name="Cards">Object ids.</param>
/// <param name="FaceUp">Their new state.</param>
public sealed record CardsFlipped(IReadOnlyList<int> Cards, bool FaceUp) : GameEvent;
