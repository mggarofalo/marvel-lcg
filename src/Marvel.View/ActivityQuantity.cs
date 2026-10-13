namespace Marvel.View;

/// <summary>Exact numeric evidence from an authorized event. Null means absent, never zero.</summary>
public sealed record ActivityQuantity(string Field, long? Before, long? After);
