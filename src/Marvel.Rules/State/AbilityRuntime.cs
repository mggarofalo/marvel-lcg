namespace Marvel.Rules.State;

/// <summary>Game-owned execution state of the admitted ability program.</summary>
/// <remarks>The interpreter owns its concrete frames; rules procedures never inspect them.</remarks>
public abstract class AbilityRuntime
{
    /// <summary>Creates an interpreter's execution state.</summary>
    protected AbilityRuntime() { }
}
