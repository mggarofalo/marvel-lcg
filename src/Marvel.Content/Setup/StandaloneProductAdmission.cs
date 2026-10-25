using System.Collections.Immutable;

namespace Marvel.Content.Setup;

/// <summary>Requires complete reviewed standalone products before runtime discovery.</summary>
public static class StandaloneProductAdmission
{
    private static readonly ImmutableArray<string> FixedWreckingRecipes =
        ["wrecking_crew_standard", "wrecking_crew_expert", "wrecking_crew_extreme"];

    /// <summary>Admits Captain America's complete face inventory and published starter.</summary>
    public static bool CaptainAmerica(SetupCatalog setup, CardCatalog cards, IReadOnlySet<string> authored) =>
        Complete("cap", ["03001a", "03001b", .. Enumerable.Range(2, 33).Select(number => $"03{number:000}")], cards, authored)
        && setup.HeroNames.Contains("captain_america")
        && setup.Hero("captain_america").Hero.SequenceEqual(["03001a,03001b"]);

    /// <summary>Admits all Wrecking Crew modes together with the complete face inventory.</summary>
    public static bool WreckingCrew(SetupCatalog setup, CardCatalog cards, IReadOnlySet<string> authored) =>
        Complete("twc", ["07001a", "07001b", .. Enumerable.Range(2, 58).Select(number => $"07{number:000}")], cards, authored)
        && WreckingRecipes().All(key => setup.CampaignNames.Contains(key)
            && setup.Campaign(key).Villain.SelectMany(spec => spec.Split(','))
                .All(face => cards.Has(face) && cards.Pack(face) == "twc"));

    private static IEnumerable<string> WreckingRecipes() =>
        FixedWreckingRecipes
        .Concat(Enumerable.Range(0, 16).Select(value => "wrecking_crew_custom_"
            + string.Concat(Enumerable.Range(0, 4).Select(index => (value & (1 << (3 - index))) == 0 ? 'a' : 'b'))));

    private static bool Complete(string pack, string[] required, CardCatalog cards, IReadOnlySet<string> authored)
    {
        // Admission is repository policy. These pinned unique face inventories include
        // both printed sides; physical extra-copy quantities are a separate source question.
        var faces = cards.Faces.Where(face => cards.Pack(face) == pack).ToHashSet(StringComparer.Ordinal);
        return faces.SetEquals(required) && required.All(authored.Contains);
    }
}
