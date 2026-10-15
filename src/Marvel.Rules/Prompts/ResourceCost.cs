namespace Marvel.Rules.Prompts;

/// <summary>One component of a simultaneous resource payment.</summary>
public sealed record ResourceCost(
    string Cost,
    IReadOnlyList<string>? Rule = null,
    bool Printed = false)
{
    /// <summary>Declarations this icon may make toward this component, before whole-payment assessment.</summary>
    public IReadOnlyList<char> Declarations(char icon) =>
        [.. Marvel.Rules.Play.Resources.Types.Where(declared =>
            ResourcePaymentRequirements.CanDeclare(icon, declared, Printed))];

    /// <summary>
    /// Requires every resource in Cost to have this type, including when Cost
    /// names a variable. Mutually exclusive with the fixed Rule requirements.
    /// </summary>
    public char? RepeatedResource { get; init; }
}
