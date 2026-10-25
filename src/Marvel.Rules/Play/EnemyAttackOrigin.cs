namespace Marvel.Rules.Play;

/// <summary>Historical identity of an attack that caused scheduled work.</summary>
/// <remarks>These game-local identifiers are engine choices. Departure does not erase history.</remarks>
public sealed record EnemyAttackOrigin(int ActivationId, int Enemy, int Player);
