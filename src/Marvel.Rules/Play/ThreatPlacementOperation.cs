using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the ThreatPlacement procedure.</summary>
public sealed record ThreatPlacementOperation(ThreatPlacement Placement,
    EffectCompletion Completion, int Part) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Steps.PlaceThreatEffect;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Threat;
}
