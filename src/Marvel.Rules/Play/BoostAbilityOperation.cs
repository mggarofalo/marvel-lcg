namespace Marvel.Rules.Play;

/// <summary>The star ability after all faceup interrupts and responses complete.</summary>
public sealed record BoostAbilityOperation(BoostFaceupOperation Faceup) : AgendaOperation
{
    internal const string Resolve = "ResolveBoostAbility";
    /// <inheritdoc/>
    public override string What => Resolve;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Attack;
}
