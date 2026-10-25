using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>One card instruction assigning the same amount to ordered schemes.</summary>
public sealed record ThreatBatch(IReadOnlyList<Card> Schemes, Card? Source,
    long Amount, ThreatCause Cause, string Trigger, int Player = -1)
{
    internal ThreatMoveReceipt? Movement { get; init; }
}
