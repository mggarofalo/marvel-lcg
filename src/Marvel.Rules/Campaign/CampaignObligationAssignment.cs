namespace Marvel.Rules.Campaign;

/// <summary>An engine-selected obligation assigned from this seat's available physical pool.</summary>
public sealed record CampaignObligationAssignment(int Seat, CampaignCard Card);
