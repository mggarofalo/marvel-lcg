namespace Marvel.Rules.Play;

/// <summary>Deals the recorded campaign penalty after every recovery choice resolves.</summary>
public sealed record CampaignEncounterDealOperation : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => "CampaignEncounterDeal";
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Campaign;
}
