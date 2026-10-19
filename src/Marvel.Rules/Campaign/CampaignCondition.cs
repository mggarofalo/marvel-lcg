namespace Marvel.Rules.Campaign;

/// <summary>The two admitted faces of one physical campaign Condition upgrade.</summary>
public sealed record CampaignCondition(CampaignCard Basic, string ImprovedFace, int HitPointBonus);
