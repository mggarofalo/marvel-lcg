namespace Marvel.Rules.Prompts;

/// <summary>One component of a simultaneous resource payment.</summary>
public sealed record ResourceCost(
    string Cost,
    IReadOnlyList<string>? Rule = null,
    bool Printed = false);
