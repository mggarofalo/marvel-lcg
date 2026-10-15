namespace Marvel.Rules.Events;

/// <summary>An attack gains causal attention or leaves its complete resolution lifetime.</summary>
public sealed record AttackBoundary(AttackIdentity Identity, int Source, int Target, bool Started) : GameEvent
{
    /// <summary>An already-open attack resumes after its nested attack resolves.</summary>
    public bool Resumed { get; init; }
}
