using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>State owned by the ThreatPlacement procedure.</summary>
public sealed record ThreatPlacementOperation(ThreatPlacement Placement,
    EffectCompletion Completion, int Part) : AgendaOperation
{
    internal ThreatMoveReceipt? Movement { get; init; }
    /// <summary>The shared response occurrence for a multi-scheme instruction.</summary>
    public Occurrence? Responses { get; init; }
    /// <inheritdoc/>
    public override string What => Steps.PlaceThreatEffect;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Threat;
}
