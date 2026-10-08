namespace Marvel.Rules.State;

/// <summary>A supported mandatory activation cost; resource letters use the engine alphabet.</summary>
public sealed record PersistentCost(string Operation, string Target, long? Amount = null, string? Resources = null, string? Counter = null, bool PrintedOnly = false);
