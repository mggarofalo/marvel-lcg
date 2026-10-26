namespace Marvel.Rules.Play;

/// <summary>Resumes one owned simultaneous direct-damage procedure.</summary>
internal sealed record SimultaneousDirectDamageOperation(SimultaneousDirectDamageResolution Resolution) : AgendaOperation
{
    public override string What => "ContinueSimultaneousDirectDamage";
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Damage;
}
