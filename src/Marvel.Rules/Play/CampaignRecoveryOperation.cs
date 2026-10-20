namespace Marvel.Rules.Play;

/// <summary>One seat's campaign setup recovery, before opening hands.</summary>
public sealed record CampaignRecoveryOperation : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => "CampaignRecovery";
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Campaign;
}
