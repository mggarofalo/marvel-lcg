using Marvel.Rules.Timing;

namespace Marvel.Rules.State;

/// <summary>Actual positive damage from one enemy attack placement, before departure.</summary>
public sealed record ReceivedAttackDamage(OccurrenceCard Attacker, OccurrenceCard Recipient, long Amount)
{
    /// <summary>Exact same-character villain stages configured when damage was taken.</summary>
    public IReadOnlyList<OccurrenceCard> AttackerStages { get; init; } = [];
}
