namespace Marvel.Rules.Campaign;

/// <summary>Why a physical card belongs to a campaign deck overlay.</summary>
public enum CampaignCardRole
{
    /// <summary>A consumable campaign TECH upgrade.</summary>
    Tech,
    /// <summary>A persistent Basic or Improved Condition upgrade.</summary>
    Condition,
    /// <summary>An ally earned during the campaign.</summary>
    RescuedAlly,
    /// <summary>An encounter obligation assigned to a player's deck.</summary>
    Obligation,
}
