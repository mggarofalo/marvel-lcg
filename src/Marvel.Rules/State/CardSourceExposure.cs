namespace Marvel.Rules.State;

/// <summary>Exposure facts at creation; authorization belongs to the projection.</summary>
public sealed record CardSourceExposure(
    DeckType Zone, int Player, bool FaceUp, bool ReplacementIdentity);
