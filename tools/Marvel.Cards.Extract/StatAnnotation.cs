namespace Marvel.Cards.Extract;

/// <summary>Source facts not represented by the engine's numeric attribute notation.</summary>
internal sealed record StatAnnotation(string? Value, bool SpecialStar, bool? PerPlayer);
