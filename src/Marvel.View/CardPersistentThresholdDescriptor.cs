namespace Marvel.View;

/// <summary>A counter threshold checked after preceding effects, never a shield capacity.</summary>
public sealed record CardPersistentThresholdDescriptor(string Target, string Counter, long Minimum);
