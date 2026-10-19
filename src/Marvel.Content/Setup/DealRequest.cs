using Marvel.Rules.Setup;

namespace Marvel.Content.Setup;

/// <summary>Scenario, roster and deck composition for one deterministic deal.</summary>
public sealed record DealRequest(
    string Scenario,
    IReadOnlyList<string> Heroes,
    IReadOnlyList<string>? ModularSets = null,
    IReadOnlyList<string>? StarterDecks = null,
    IReadOnlyList<PlayerDeckRecipe>? PlayerDecks = null)
{
    /// <summary>Campaign-authorized removed allies, including otherwise mandatory signature cards.</summary>
    public IReadOnlyList<string> CampaignExcludedAllies { get; init; } = [];
}
