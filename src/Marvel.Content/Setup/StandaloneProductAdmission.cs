using System.Collections.Immutable;

namespace Marvel.Content.Setup;

/// <summary>Requires complete reviewed standalone products before runtime discovery.</summary>
public static class StandaloneProductAdmission
{
    private static readonly ImmutableArray<string> KreeFanaticCopies =
        ["90001", "90002", "90003", "90004", "90004", "90005", "90005"];
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

    /// <summary>Admits Black Widow's complete face inventory and published starter.</summary>
    public static bool BlackWidow(SetupCatalog setup, CardCatalog cards, IReadOnlySet<string> authored) =>
        Complete("bkw", ["08001a", "08001b", .. Enumerable.Range(2, 32).Select(number => $"08{number:000}")], cards, authored)
        && setup.HeroNames.Contains("black_widow")
        && BlackWidowRecipe.Complete(setup.Hero("black_widow"));

    /// <summary>Admits Doctor Strange's complete face inventory and separate Invocation recipe.</summary>
    public static bool DoctorStrange(SetupCatalog setup, CardCatalog cards, IReadOnlySet<string> authored) =>
        Complete("drs", ["09001a", "09001b", .. Enumerable.Range(2, 38).Select(number => $"09{number:000}")], cards, authored)
        && setup.HeroNames.Contains("doctor_strange")
        && DoctorStrangeRecipe.Complete(setup.Hero("doctor_strange"));

    /// <summary>Admits Hulk's complete face inventory and published starter.</summary>
    public static bool Hulk(SetupCatalog setup, CardCatalog cards, IReadOnlySet<string> authored) =>
        Complete("hlk", ["10001a", "10001b", .. Enumerable.Range(2, 31).Select(number => $"10{number:000}")], cards, authored)
        && setup.HeroNames.Contains("hulk")
        && HulkRecipe.Complete(setup.Hero("hulk"));

    /// <summary>Admits Ant-Man's complete face inventory and published starter.</summary>
    public static bool AntMan(SetupCatalog setup, CardCatalog cards, IReadOnlySet<string> authored) =>
        Complete("ant", ["12001a", "12001b", "12001c", .. Enumerable.Range(2, 32).Select(number => $"12{number:000}")], cards, authored)
        && setup.HeroNames.Contains("ant_man")
        && AntManRecipe.Complete(setup.Hero("ant_man"));

    /// <summary>Admits Wasp's complete face inventory and published starter.</summary>
    public static bool Wasp(SetupCatalog setup, CardCatalog cards, IReadOnlySet<string> authored) =>
        Complete("wsp", ["13001a", "13001b", "13001c", .. Enumerable.Range(2, 33).Select(number => $"13{number:000}")], cards, authored)
        && setup.HeroNames.Contains("wasp")
        && WaspRecipe.Complete(setup.Hero("wasp"));

    /// <summary>Admits the complete seven-card Kree Fanatic modular set.</summary>
    public static bool KreeFanatic(SetupCatalog setup, CardCatalog cards, IReadOnlySet<string> authored) =>
        Complete("ron", ["90001", "90002", "90003", "90004", "90005"], cards, authored)
        && setup.EncounterSetNames.Contains("kree_fanatic")
        && setup.EncounterRole("kree_fanatic") == EncounterSetRole.Modular
        // Repository policy pins physical quantities to the seven numbered FFG PDF pages.
        && setup.EncounterSet("kree_fanatic").Order(StringComparer.Ordinal)
            .SequenceEqual(KreeFanaticCopies);

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
