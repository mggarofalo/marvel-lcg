using Marvel.Rules.Campaign;
using Marvel.Rules.Play;
using Marvel.Rules.Setup;

namespace Marvel.Server;

/// <summary>Content admission and pure campaign facts supplied by the authoritative game factory.</summary>
public interface ICampaignGameFactory : IDurableGameFactory
{
    /// <summary>The complete admitted campaign, or null until its complete content boundary is executable.</summary>
    CampaignDefinition? CampaignDefinition { get; }

    /// <summary>Reads cumulative persistent changes and terminal facts without changing the game or RNG.</summary>
    CampaignGameProgress InspectCampaign(Game game);

    /// <summary>Assesses a campaign recipe with its authorized removed-card exceptions.</summary>
    DeckAssessment AssessCampaignDeck(string hero, IReadOnlyList<string> cards, IReadOnlyList<string> bannedAllies);

    /// <summary>Returns the printed identity base HP after hero admission.</summary>
    int CampaignBaseHitPoints(string hero);
}
