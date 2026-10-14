namespace Marvel.View;

/// <summary>Authorized participants of an attack still resolving, including its response windows.</summary>
public sealed record ActiveAttackDescriptor(ActivitySubject Source, ActivitySubject Target)
{
    /// <summary>Exact authoritative attack lifetime, independent of its participants.</summary>
    public Marvel.Rules.Events.AttackIdentity? Identity { get; init; }
}
