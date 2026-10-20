namespace Marvel.Rules.Campaign;

/// <summary>An engine-accepted permanent campaign removal from an attempt.</summary>
public sealed record CampaignCardRemoval(int Seat, string CardKey);
