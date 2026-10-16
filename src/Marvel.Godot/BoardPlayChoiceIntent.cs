namespace Marvel.Godot;

/// <summary>Stable card and destination identities selected by a hand-play gesture.</summary>
internal sealed record BoardPlayChoiceIntent(int Source, int? Destination, bool IsHandCard, bool OnPlayerLane);
