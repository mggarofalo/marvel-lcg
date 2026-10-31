namespace Marvel.Rules.Play;

/// <summary>A basic attack awaiting simultaneous additional and exhaustion costs.</summary>
public sealed record BasicAttackCostOperation(int Enemy, int Incarnation, int Count, bool Exhaust) : AgendaOperation
{
    /// <summary>Resources of any type the attacker's controller spends.</summary>
    public int Resources { get; init; }
    /// <inheritdoc />
    public override string What => "PayBasicAttackCost";
    /// <inheritdoc />
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Attack;
}
