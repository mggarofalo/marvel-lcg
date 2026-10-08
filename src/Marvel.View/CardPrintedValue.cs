namespace Marvel.View;

/// <summary>Visibility-safe printed value and independent marks; a missing field is absent.</summary>
public sealed record CardPrintedValue(
    string Value, bool SpecialStar, bool PerPlayer, int ConsequentialDamage);
