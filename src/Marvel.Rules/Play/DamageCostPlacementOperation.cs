namespace Marvel.Rules.Play;

/// <summary>Resumes the placement stage of one owned damage cost.</summary>
internal sealed record DamageCostPlacementOperation(DamageCostPlacement Placement) : AgendaOperation
{
    public override string What => "ContinueDamageCostPlacement";
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Damage;
}
