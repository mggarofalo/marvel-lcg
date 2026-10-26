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
        && StandaloneProductRecipes.CaptainAmerica(setup.Hero("captain_america"));

    /// <summary>Admits all Wrecking Crew modes together with the complete face inventory.</summary>
    public static bool WreckingCrew(SetupCatalog setup, CardCatalog cards, IReadOnlySet<string> authored) =>
        Complete("twc", ["07001a", "07001b", .. Enumerable.Range(2, 58).Select(number => $"07{number:000}")], cards, authored)
        && WreckingRecipes().All(key => setup.CampaignNames.Contains(key)
            && StandaloneProductRecipes.WreckingCrew(key, setup.Campaign(key)));

    /// <summary>Admits both Green Goblin scenarios and all four modular sets together.</summary>
    public static bool GreenGoblin(SetupCatalog setup, CardCatalog cards, IReadOnlySet<string> authored) =>
        Complete("gob", [.. Enumerable.Range(1, 6).SelectMany(number => new[] { $"02{number:000}a", $"02{number:000}b" }),
            .. Enumerable.Range(7, 10).Select(number => $"02{number:000}"),
            "02017a", "02017b", "02018a", "02018b",
            .. Enumerable.Range(19, 31).Select(number => $"02{number:000}")], cards, authored)
        && GreenGoblinRecipes.Complete(setup);

    /// <summary>Admits Ms. Marvel's complete face inventory and published starter.</summary>
    public static bool MsMarvel(SetupCatalog setup, CardCatalog cards, IReadOnlySet<string> authored) =>
        Complete("msm", ["05001a", "05001b", .. Enumerable.Range(2, 32).Select(number => $"05{number:000}")], cards, authored)
        && setup.HeroNames.Contains("ms_marvel")
        && MsMarvelRecipe.Complete(setup.Hero("ms_marvel"));

    /// <summary>Admits Thor's complete face inventory and published starter.</summary>
    public static bool Thor(SetupCatalog setup, CardCatalog cards, IReadOnlySet<string> authored) =>
        Complete("thor", ["06001a", "06001b", .. Enumerable.Range(2, 33).Select(number => $"06{number:000}")], cards, authored)
        && setup.HeroNames.Contains("thor")
        && ThorRecipe.Complete(setup.Hero("thor"));

    private static IEnumerable<string> WreckingRecipes() =>
        FixedWreckingRecipes
        .Concat(Enumerable.Range(0, 16).Select(value => "wrecking_crew_custom_"
            + string.Concat(Enumerable.Range(0, 4).Select(index => (value & (1 << (3 - index))) == 0 ? 'a' : 'b'))));

    private static bool Complete(string pack, string[] required, CardCatalog cards, IReadOnlySet<string> authored)
    {
        // Admission is repository policy. These pinned unique face inventories include
        // both printed sides and are independent of physical packaging quantities.
        var faces = cards.Faces.Where(face => cards.Pack(face) == pack).ToHashSet(StringComparer.Ordinal);
        return faces.SetEquals(required) && required.All(authored.Contains);
    }
}
