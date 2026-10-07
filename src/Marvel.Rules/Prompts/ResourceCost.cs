namespace Marvel.Rules.Prompts;

/// <summary>One component of a simultaneous resource payment.</summary>
public sealed record ResourceCost(
    string Cost,
    IReadOnlyList<string>? Rule = null,
    bool Printed = false)
{
    /// <summary>
    /// Requires every resource in Cost to have this type, including when Cost
    /// names a variable. Mutually exclusive with the fixed Rule requirements.
    /// </summary>
    public char? RepeatedResource { get; init; }
}
