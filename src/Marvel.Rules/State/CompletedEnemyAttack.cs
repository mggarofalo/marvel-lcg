using System.Collections.Immutable;

namespace Marvel.Rules.State;

/// <summary>Facts fixed when an enemy attack completes, independent of later responses.</summary>
public sealed record CompletedEnemyAttack(int ActivationId, int Defender, long DamageDealt)
{
    /// <summary>Characters actually defeated by this attack's damage, in defeat order.</summary>
    public ImmutableArray<Defeated> DefeatedCharacters { get; init; } = [];
}
