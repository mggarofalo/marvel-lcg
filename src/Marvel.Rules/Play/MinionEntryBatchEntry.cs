using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>One minion's engagement in a <see cref="MinionEntryBatch"/>, with its entry occurrence.</summary>
public sealed record MinionEntryBatchEntry(int Card, int Incarnation, int Player, Occurrence Occurrence);
