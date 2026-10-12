namespace Marvel.Rules.Events;

/// <summary>One seat's actual quantity and card subjects in a completed activity.</summary>
public sealed record ActivityParticipant(int Player, int Count, IReadOnlyList<int> Cards);
