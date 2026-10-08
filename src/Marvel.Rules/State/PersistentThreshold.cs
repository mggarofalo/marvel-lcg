namespace Marvel.Rules.State;

/// <summary>A counter threshold checked after preceding effects, not a capacity or prevention pool.</summary>
public sealed record PersistentThreshold(string Target, string Counter, long Minimum);
