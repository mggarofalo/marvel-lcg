using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Kang's delayed phase-end effect and other delayed effects due at the same ending.</summary>
/// <param name="Owner">The phase-ending step both are due at.</param>
/// <param name="Origin">That step's occurrence.</param>
/// <param name="Deferred">Whether this frame resolves the other delayed effects after Kang's.</param>
public sealed record KangPhaseEndOrderOperation(PhaseStep Owner, Occurrence Origin, bool Deferred = false)
    : AgendaOperation
{
    /// <summary>Engine-chosen name of the first player's ordering decision.</summary>
    public const string Order = "OrderKangPhaseEnd";

    /// <summary>Engine-chosen name of the frame resolving the other delayed effects last.</summary>
    public const string ResolveDeferred = "ResolveDeferredPhaseEndEffects";

    /// <inheritdoc/>
    public override string What => Deferred ? ResolveDeferred : Order;

    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.PhaseTransition;
}
