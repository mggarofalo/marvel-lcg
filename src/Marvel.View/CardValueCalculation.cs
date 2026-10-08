namespace Marvel.View;

/// <summary>One authorized operation from the engine's quantity evaluation.</summary>
public sealed record CardValueCalculation(
    string Operation,
    long Amount,
    CardValueSourceDescriptor? Source,
    CardValueDuration? Duration);
