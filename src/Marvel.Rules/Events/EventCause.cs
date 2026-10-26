namespace Marvel.Rules.Events;

/// <summary>Exact occurrence and participants enclosing a result; a wire fact, not an inferred action.</summary>
/// <param name="Occurrence">The exact enclosing occurrence.</param>
/// <param name="Kind">The enclosing game function.</param>
/// <param name="Source">The acting card.</param>
/// <param name="Target">The recipient, or -1 for a thwart resolving an independent sentence without a scheme.</param>
public sealed record EventCause(int Occurrence, EventCauseKind Kind, int Source, int Target)
{
    /// <summary>Occurrence-time public title of the acting card.</summary>
    public string SourceTitle { get; init; } = string.Empty;
    /// <summary>Occurrence-time public title of the target card.</summary>
    public string TargetTitle { get; init; } = string.Empty;
}
