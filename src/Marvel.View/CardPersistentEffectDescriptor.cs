namespace Marvel.View;

/// <summary>A supported complete effect. Until bounds a grant; After defers execution.</summary>
public sealed record CardPersistentEffectDescriptor(string Operation, string Target, long? Amount, string? Field, string? Until, string? After, CardPersistentThresholdDescriptor? Condition);
