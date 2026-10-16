namespace Marvel.Rules.Play;

/// <summary>An owned continuation of replacement, defeat, overkill and retaliation.</summary>
internal sealed record AttackDamageResolutionOperation(AttackDamageResolution Resolution) : AgendaOperation
{
    public override string What => "ContinueAttackDamage";
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Attack;
}
