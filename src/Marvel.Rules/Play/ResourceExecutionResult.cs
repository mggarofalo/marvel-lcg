namespace Marvel.Rules.Play;

/// <summary>Owned generator execution and its separately committed resource production.</summary>
public sealed record ResourceExecutionResult(
    AbilityExecutionResult Execution, ResourceGenerationReceipt Generation);
