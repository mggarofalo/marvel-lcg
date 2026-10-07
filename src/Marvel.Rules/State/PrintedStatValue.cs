namespace Marvel.Rules.State;

/// <summary>A printed numeral or symbol and its independent printed marks.</summary>
public sealed record PrintedStatValue(
    string Value, bool SpecialStar, bool PerPlayer, int ConsequentialDamage);
