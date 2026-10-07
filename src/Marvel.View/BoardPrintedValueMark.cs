namespace Marvel.View;

/// <summary>Meaning of a printed numeric annotation, independent of typography.</summary>
public sealed record BoardPrintedValueMark(string Attribute, bool PerPlayer, int ConsequentialDamage);
