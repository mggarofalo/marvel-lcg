namespace Marvel.Rules.Play;

/// <summary>Resumes the exact pending direct damage execution.</summary>
internal sealed record DamageResolutionOperation(DamageResolution Resolution) : AgendaOperation
{
    public override string What => "ContinueDamageResolution";
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Damage;
}
