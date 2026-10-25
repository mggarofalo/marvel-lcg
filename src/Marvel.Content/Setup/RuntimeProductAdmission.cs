using System.Collections.Immutable;
using Marvel.Rules.Campaign;

namespace Marvel.Content.Setup;

/// <summary>Separates authored research interpretations from accepted executable products.</summary>
public sealed class RuntimeProductAdmission
{
    private RuntimeProductAdmission(SetupCatalog setup, ImmutableHashSet<string> playable,
        CampaignDefinition? campaign)
    {
        Setup = setup;
        Playable = playable;
        Campaign = campaign;
    }

    /// <summary>Recipes belonging to accepted products, validated against executable faces.</summary>
    public SetupCatalog Setup { get; }
    /// <summary>Authored faces within the explicitly accepted product boundary.</summary>
    public ImmutableHashSet<string> Playable { get; }
    /// <summary>The complete accepted campaign, if present.</summary>
    public CampaignDefinition? Campaign { get; }

    /// <summary>Applies the reviewed product policy before discovery or game creation.</summary>
    public static RuntimeProductAdmission Discover(SetupCatalog setup, CardCatalog cards,
        IReadOnlySet<string> authored)
    {
        var campaign = RiseOfRedSkullAdmission.Discover(setup, cards, authored);
        // Product acceptance is repository policy, not a card rule. New authored
        // rows cannot extend this closed boundary; a product requires explicit review.
        bool captainAmerica = StandaloneProductAdmission.CaptainAmerica(setup, cards, authored);
        bool wreckingCrew = StandaloneProductAdmission.WreckingCrew(setup, cards, authored);
        bool Accepted(string pack) => pack switch
        {
            "core" => true,
            "trors" => campaign is not null,
            "cap" => captainAmerica,
            "twc" => wreckingCrew,
            _ => false,
        };
        var playable = authored.Where(face => Accepted(cards.Pack(face)))
            .ToImmutableHashSet(StringComparer.Ordinal);
        foreach (string pack in cards.Faces.Select(cards.Pack).Distinct(StringComparer.Ordinal)
            .Where(pack => !Accepted(pack)).Order(StringComparer.Ordinal))
            setup = setup.WithoutPack(cards, pack);
        // A retained Core recipe referencing unavailable content remains an error.
        SetupCatalogAdmission.Validate(setup, cards, playable);
        return new(setup, playable, campaign);
    }
}
