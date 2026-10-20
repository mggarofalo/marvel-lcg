using Marvel.Rules.State;

namespace Marvel.Content.Setup;

/// <summary>Applies campaign-authorized ally removals without relaxing other construction rules.</summary>
public static class CampaignDeckExclusions
{
    /// <summary>Requires complete replacement recipes and forbids reintroducing a removed ally.</summary>
    public static void ValidateRecipes(DealRequest request)
    {
        if (request.CampaignExcludedAllies.Count == 0) return;
        if (request.PlayerDecks is null)
            throw new ArgumentException("campaign removals require explicit replacement decks", nameof(request));
        if (request.PlayerDecks.SelectMany(deck => deck.Cards)
            .Any(face => request.CampaignExcludedAllies.Contains(face, StringComparer.Ordinal)))
            throw new ArgumentException("a removed ally cannot return to the campaign", nameof(request));
    }

    /// <summary>Only printed allies may receive the campaign's missing-signature exception.</summary>
    public static void Validate(IReadOnlyList<string> excluded, ICardFacts? facts)
    {
        ArgumentNullException.ThrowIfNull(excluded);
        foreach (string face in excluded)
        {
            if (facts is null || facts.Kind(face) != CardKind.Ally)
                throw new ArgumentException("campaign exclusions must name printed allies", nameof(excluded));
        }
    }

    /// <summary>Removes banned allies from required signatures and default customization.</summary>
    public static HeroSetup Apply(HeroSetup hero, IReadOnlyList<string> excluded) => excluded.Count == 0 ? hero : hero with
    {
        // pack:mc10:zola-campaign-instructions.victory removes imprisoned allies
        // "from the campaign"; rr:modes-of-play.10 forbids using them again.
        HeroDeck = hero.HeroDeck.Where(face => !excluded.Contains(face, StringComparer.Ordinal)).ToArray(),
        PlayerDeck = hero.PlayerDeck.Where(face => !excluded.Contains(face, StringComparer.Ordinal)).ToArray(),
    };
}
