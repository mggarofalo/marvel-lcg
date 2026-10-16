namespace Marvel.Rules.State;

/// <summary>Interpreter-owned suspended work carried opaquely by the agenda.</summary>
public abstract record CardAbilityContinuation
{
    /// <summary>The Rules-owned invocation resumed by this opaque payload.</summary>
    public Marvel.Rules.Play.AbilityExecution? Execution { get; init; }
}
