using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Rules.Setup;

namespace Marvel.Server;

/// <summary>Campaign deck edits use the ordinary rules with only authorized ally exclusions.</summary>
internal static class CampaignDecks
{
    internal static DeckAssessment Assess(IReadOnlyList<string> draft, HeroSetup hero,
        CardCatalog cards, IReadOnlySet<string> authored, IReadOnlyList<string> banned)
    {
        CampaignDeckExclusions.Validate(banned, cards);
        var assessment = CoreDeckAssessment.Assess(draft, CampaignDeckExclusions.Apply(hero, banned), cards, authored);
        var forbidden = draft.Where(face => banned.Contains(face, StringComparer.Ordinal)).Distinct(StringComparer.Ordinal)
            .Select(face => new DeckDiagnostic("campaign-removed", face, "This ally was permanently removed from the campaign."));
        return new DeckAssessment(assessment.Size, assessment.Diagnostics.Concat(forbidden));
    }

    internal static IReadOnlyDictionary<string, int[]> Quantities(IReadOnlyList<string> draft, HeroSetup hero,
        CardCatalog cards, IReadOnlySet<string> authored, IReadOnlyList<string> banned)
    {
        CampaignDeckExclusions.Validate(banned, cards);
        return CoreDeckQuantities.Assess(draft, CampaignDeckExclusions.Apply(hero, banned), cards, authored)
            .ToDictionary(pair => pair.Key,
                pair => banned.Contains(pair.Key, StringComparer.Ordinal) ? new int[1] : pair.Value,
                StringComparer.Ordinal);
    }
}
