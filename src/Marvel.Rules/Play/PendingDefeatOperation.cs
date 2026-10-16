namespace Marvel.Rules.Play;

/// <summary>Rechecks the same physical card instance after its imminent-defeat interrupts finish.</summary>
public sealed record PendingDefeatOperation(DefeatCause Cause, int Incarnation) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Steps.FinishWouldBeDefeated;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Defeat;
}
