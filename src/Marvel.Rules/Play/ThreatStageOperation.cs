using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the ThreatStage procedure.</summary>
public sealed record ThreatStageOperation(ThreatStageKind Kind) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Kind switch
    {
        ThreatStageKind.PlaceThreat => Steps.PlaceThreat,
        ThreatStageKind.SchemeThreat => Steps.SchemeThreat,
        ThreatStageKind.EndSchemeEarly => Steps.EndSchemeEarly,
        _ => throw new ArgumentOutOfRangeException(nameof(Kind)),
    };
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Threat;
}
