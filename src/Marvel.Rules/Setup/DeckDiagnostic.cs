namespace Marvel.Rules.Setup;

/// <summary>A correction the player can make before committing a deck.</summary>
/// <remarks>Codes and diagnostic ordering are engine choices, not printed rules.</remarks>
public sealed record DeckDiagnostic(string Code, string Face, string Message, int? Actual = null, long? Expected = null);
