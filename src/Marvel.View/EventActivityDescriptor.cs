namespace Marvel.View;

/// <summary>Transport-neutral occurrence facts projected locally from filtered events.</summary>
public sealed record EventActivityDescriptor(
    IReadOnlyList<ActivitySubject> Subjects,
    IReadOnlyList<ActivityQuantity> Quantities)
{
    /// <summary>An accepted printed-face transition, rather than a stat-only receipt.</summary>
    public bool FaceChanged { get; init; }
    /// <summary>Typed destination zone for an authorized movement.</summary>
    public string DestinationZone { get; init; } = string.Empty;
    /// <summary>Exact attack lifetime enclosing this receipt.</summary>
    public Marvel.Rules.Events.AttackIdentity? AttackIdentity { get; init; }
    /// <summary>True at attack onset, false after its final response window, otherwise absent.</summary>
    public bool? AttackBoundary { get; init; }
    /// <summary>Structural event meaning, independent of its display copy.</summary>
    public EventActivityKind Kind { get; init; }
    /// <summary>Engine-authored response-local instruction identity.</summary>
    public int? Batch { get; init; }
    /// <summary>Round context supplied by the authoritative response, when available.</summary>
    public int? Round { get; init; }
    /// <summary>Explicit phase context; empty when not established.</summary>
    public string Phase { get; init; } = string.Empty;
    /// <summary>Whether the activity is one explicit phase procedure.</summary>
    public bool PhaseStep { get; init; }
    /// <summary>An engine-authored phase entry suitable for a chronology separator.</summary>
    public bool PhaseBoundary { get; init; }
    /// <summary>Actual quantities for each participating seat, including concealed draws.</summary>
    public IReadOnlyList<ActivityParticipantDescriptor> Participants { get; init; } = [];

    /// <summary>The explicit status-area transition, when present.</summary>
    public ActivityStatusChange StatusChange { get; init; }

    /// <summary>Authorized movement endpoints; empty when this is not a movement.</summary>
    public string From { get; init; } = string.Empty;
    /// <summary>Authorized movement destination.</summary>
    public string To { get; init; } = string.Empty;
    /// <summary>Exact engine operation; consumers must not recover it from prose.</summary>
    public string Operation { get; init; } = string.Empty;
}
