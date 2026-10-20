using Marvel.Rules.Setup;

namespace Marvel.Rules.Campaign;

/// <summary>An admitted hero and validated starting recipe supplied by the content boundary.</summary>
public sealed record CampaignRosterEntry(string Hero, PlayerDeckRecipe Deck, int BaseHitPoints);
