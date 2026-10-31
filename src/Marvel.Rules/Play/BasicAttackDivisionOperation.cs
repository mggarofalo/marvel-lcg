namespace Marvel.Rules.Play;

/// <summary>A divided basic attack waiting for its late recipients or its damage to settle.</summary>
internal sealed record BasicAttackDivisionOperation(BasicAttackDivisionResolution Resolution) : AgendaOperation
{
    public override string What => "ContinueDividedBasicAttack";
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Attack;
}
