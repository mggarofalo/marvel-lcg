namespace Marvel.Rules.Play;

/// <summary>Physical card and player who generated resources by spending it.</summary>
public sealed record SpentResourceCard(int Card, int Incarnation, int Player);
