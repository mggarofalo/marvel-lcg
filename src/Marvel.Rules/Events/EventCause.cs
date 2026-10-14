namespace Marvel.Rules.Events;

/// <summary>Exact occurrence and participants enclosing a result; a wire fact, not an inferred action.</summary>
public sealed record EventCause(int Occurrence, EventCauseKind Kind, int Source, int Target)
{
    /// <summary>Occurrence-time public title of the acting card.</summary>
    public string SourceTitle { get; init; } = string.Empty;
    /// <summary>Occurrence-time public title of the target card.</summary>
    public string TargetTitle { get; init; } = string.Empty;
}
