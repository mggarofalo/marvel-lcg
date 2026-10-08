namespace Marvel.View;

/// <summary>Mandatory cost with explicit Source, ActingIdentity, or ActingPlayer responsibility.</summary>
public sealed record CardPersistentCostDescriptor(string Operation, string Target, long? Amount, string? Resources, string? Counter, bool PrintedOnly);
