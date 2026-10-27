using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the ThreatStage procedure.</summary>
public sealed record ThreatStageOperation(ThreatStageKind Kind) : AgendaOperation
{
    /// <summary>Whether the phase beginning has been observed before step one.</summary>
    internal bool PhaseBegun { get; init; }

    /// <summary>Whether step-one completion consumers have been observed after beginning windows.</summary>
    internal bool CompletionObserved { get; init; }

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
