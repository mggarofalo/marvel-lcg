namespace Marvel.Rules.State;

/// <summary>A supported effect verb. Until bounds a grant; After defers execution.</summary>
public sealed record PersistentEffect(string Operation, string Target, long? Amount = null, string? Field = null, string? Until = null, string? After = null, PersistentThreshold? Condition = null);
