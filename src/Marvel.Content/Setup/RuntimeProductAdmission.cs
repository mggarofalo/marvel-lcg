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
        bool greenGoblin = StandaloneProductAdmission.GreenGoblin(setup, cards, authored);
        bool msMarvel = StandaloneProductAdmission.MsMarvel(setup, cards, authored);
        bool thor = StandaloneProductAdmission.Thor(setup, cards, authored);
        bool blackWidow = StandaloneProductAdmission.BlackWidow(setup, cards, authored);
        bool doctorStrange = StandaloneProductAdmission.DoctorStrange(setup, cards, authored);
        bool hulk = StandaloneProductAdmission.Hulk(setup, cards, authored);
        bool antMan = StandaloneProductAdmission.AntMan(setup, cards, authored);
        bool wasp = StandaloneProductAdmission.Wasp(setup, cards, authored);
        bool quicksilver = StandaloneProductAdmission.Quicksilver(setup, cards, authored);
        bool groot = StandaloneProductAdmission.Groot(setup, cards, authored);
        bool kreeFanatic = StandaloneProductAdmission.KreeFanatic(setup, cards, authored);
        if (!kreeFanatic) setup = setup.WithoutEncounterSet("kree_fanatic");
        bool Accepted(string pack) => pack switch
        {
            "core" => true,
            "trors" => campaign is not null,
            "cap" => captainAmerica,
            "twc" => wreckingCrew,
            "gob" => greenGoblin,
            "msm" => msMarvel,
            "thor" => thor,
            "bkw" => blackWidow,
            "drs" => doctorStrange,
            "hlk" => hulk,
            "ant" => antMan,
            "wsp" => wasp,
            "qsv" => quicksilver,
            "ron" => kreeFanatic,
            _ => false,
        };
        // The Galaxy's Most Wanted is admitted in parts: Groot's faces are a product
        // of their own while the rest of the box stays closed.
        var parts = groot ? GrootRecipe.Faces.ToImmutableHashSet(StringComparer.Ordinal)
            : ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal);
        bool AcceptedFace(string face) => Accepted(cards.Pack(face)) || parts.Contains(face);
        var playable = authored.Where(face => cards.Has(face) && AcceptedFace(face))
            .ToImmutableHashSet(StringComparer.Ordinal);
        foreach (string pack in cards.Faces.Select(cards.Pack).Distinct(StringComparer.Ordinal)
            .Where(pack => !Accepted(pack)).Order(StringComparer.Ordinal))
            setup = setup.WithoutFaces(cards, face => cards.Pack(face) == pack && !AcceptedFace(face));
        // A retained Core recipe referencing unavailable content remains an error.
        SetupCatalogAdmission.Validate(setup, cards, playable);
        return new(setup, playable, campaign);
    }
}
