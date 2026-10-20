namespace Marvel.Rules.Play;

/// <summary>A suspended simultaneous attack-damage batch.</summary>
internal sealed record SimultaneousAttackDamageOperation(SimultaneousAttackDamageResolution Resolution) : AgendaOperation
{
    public override string What => "ContinueSimultaneousAttackDamage";
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Attack;
}
