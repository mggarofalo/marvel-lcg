namespace Marvel.Rules.Play;

/// <summary>An owned removal followed by the destination's ordinary threat placement windows.</summary>
public sealed record ThreatMovementOperation(ThreatMoveRequest Request,
    ThreatMoveReceipt Receipt) : AgendaOperation
{
    internal bool PlacementPending { get; init; }

    /// <inheritdoc/>
    public override string What => Steps.CardEffectResolved;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Threat;
}
