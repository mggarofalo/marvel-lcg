namespace Marvel.Rules.Play;

/// <summary>The optional interrupt window before one direct-damage placement.</summary>
internal sealed record DirectDamagePreventionOperation(DirectDamagePrevention Prevention) : AgendaOperation
{
    public override string What => "DirectDamagePrevention";
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Damage;
}
