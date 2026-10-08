namespace Marvel.Rules.Timing;

/// <summary>How a numeric continuous effect participates in quantity evaluation.</summary>
public enum ContinuousValueRole
{
    /// <summary>An additive contribution to the defined base.</summary>
    Modifier,
    /// <summary>The numerical definition of the source's own basic-power base.</summary>
    BaseDefinition,
}
