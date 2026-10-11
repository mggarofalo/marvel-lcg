namespace Marvel.View;

/// <summary>Transport-neutral occurrence facts projected locally from filtered events.</summary>
public sealed record EventActivityDescriptor(
    IReadOnlyList<ActivitySubject> Subjects,
    IReadOnlyList<ActivityQuantity> Quantities)
{
    /// <summary>The explicit status-area transition, when present.</summary>
    public ActivityStatusChange StatusChange { get; init; }

    /// <summary>Authorized movement endpoints; empty when this is not a movement.</summary>
    public string From { get; init; } = string.Empty;
    /// <summary>Authorized movement destination.</summary>
    public string To { get; init; } = string.Empty;
    /// <summary>Exact engine operation; consumers must not recover it from prose.</summary>
    public string Operation { get; init; } = string.Empty;
}
