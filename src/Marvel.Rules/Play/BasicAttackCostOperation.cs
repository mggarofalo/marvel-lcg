namespace Marvel.Rules.Play;

/// <summary>A basic attack awaiting simultaneous hand-discard and exhaustion costs.</summary>
public sealed record BasicAttackCostOperation(int Enemy, int Incarnation, int Count, bool Exhaust) : AgendaOperation
{
    /// <inheritdoc />
    public override string What => "PayBasicAttackCost";
    /// <inheritdoc />
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Attack;
}
