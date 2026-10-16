namespace Marvel.Rules.State;

/// <summary>Opaque continuation payload carried by a card-labelled basic power.</summary>
public abstract record CardPowerContinuation
{
    /// <summary>The Rules-owned invocation resumed by this opaque payload.</summary>
    public Marvel.Rules.Play.AbilityExecution? Execution { get; init; }
}
