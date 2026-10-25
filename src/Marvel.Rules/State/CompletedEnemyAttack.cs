namespace Marvel.Rules.State;

/// <summary>Facts fixed when an enemy attack completes, independent of later responses.</summary>
public sealed record CompletedEnemyAttack(int ActivationId, int Defender, long DamageDealt);
