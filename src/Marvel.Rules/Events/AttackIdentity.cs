namespace Marvel.Rules.Events;

/// <summary>Engine-assigned attack identity; activation and occurrence ids occupy separate domains.</summary>
public sealed record AttackIdentity(int Id, bool EnemyActivation);
