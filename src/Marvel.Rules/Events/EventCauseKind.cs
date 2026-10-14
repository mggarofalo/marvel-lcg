namespace Marvel.Rules.Events;

/// <summary>Engine-authored causal operations that are distinct from the shape of a result.</summary>
public enum EventCauseKind
{
    /// <summary>An enemy scheme activation against a player.</summary>
    Scheme,
    /// <summary>A character thwart against a scheme.</summary>
    Thwart,
}
