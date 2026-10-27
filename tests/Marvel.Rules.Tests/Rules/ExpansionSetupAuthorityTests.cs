using Marvel.Behavior.Index;
using Xunit;

namespace Marvel.Rules.Tests.Rules;

public sealed class ExpansionSetupAuthorityTests
{
    [Fact]
    public void ExpansionSetupIsAccountedForWithoutClaimingCoreTranscriptCoverage()
    {
        var expected = new[]
        {
            "setup:campaign:absorbing_man", "setup:campaign:absorbing_man_expert",
            "setup:campaign:crossbones", "setup:campaign:crossbones_expert",
            "setup:campaign:mutagen_formula", "setup:campaign:mutagen_formula_expert",
            "setup:campaign:red_skull", "setup:campaign:red_skull_expert",
            "setup:campaign:risky_business", "setup:campaign:risky_business_expert",
            "setup:campaign:taskmaster", "setup:campaign:taskmaster_expert",
            "setup:campaign:wrecking_crew_custom_aaaa",
            "setup:campaign:wrecking_crew_custom_aaab",
            "setup:campaign:wrecking_crew_custom_aaba",
            "setup:campaign:wrecking_crew_custom_aabb",
            "setup:campaign:wrecking_crew_custom_abaa",
            "setup:campaign:wrecking_crew_custom_abab",
            "setup:campaign:wrecking_crew_custom_abba",
            "setup:campaign:wrecking_crew_custom_abbb",
            "setup:campaign:wrecking_crew_custom_baaa",
            "setup:campaign:wrecking_crew_custom_baab",
            "setup:campaign:wrecking_crew_custom_baba",
            "setup:campaign:wrecking_crew_custom_babb",
            "setup:campaign:wrecking_crew_custom_bbaa",
            "setup:campaign:wrecking_crew_custom_bbab",
            "setup:campaign:wrecking_crew_custom_bbba",
            "setup:campaign:wrecking_crew_custom_bbbb",
            "setup:campaign:wrecking_crew_expert",
            "setup:campaign:wrecking_crew_extreme",
            "setup:campaign:wrecking_crew_standard",
            "setup:campaign:zola", "setup:campaign:zola_expert",
            "setup:encounter-set:a_mess_of_things",
            "setup:encounter-set:goblin_gimmicks",
            "setup:encounter-set:hydra_assault", "setup:encounter-set:hydra_patrol",
            "setup:encounter-set:power_drain", "setup:encounter-set:running_interference", "setup:encounter-set:weapon_master", "setup:hero:black_widow", "setup:hero:captain_america",
            "setup:hero:doctor_strange", "setup:hero:hawkeye", "setup:hero:hulk", "setup:hero:ms_marvel", "setup:hero:spider_woman", "setup:hero:thor",
        };
        var expansion = Catalog.Build().Sources.Where(source =>
            source.Kind == "setup" && source.Disposition == "outside-core").ToArray();

        Assert.Equal(expected, expansion.Select(source => source.Id));
        Assert.All(expansion, source => Assert.All(source.Obligations, obligation =>
        {
            Assert.Equal("outside-core", obligation.Disposition);
            Assert.Null(obligation.Implementation);
            Assert.Empty(obligation.Scenarios);
            Assert.Contains(Product(source.Id), obligation.Reason);
        }));
    }

    private static string Product(string source) => source switch
    {
        "setup:hero:black_widow" => "Black Widow hero pack",
        "setup:hero:captain_america" => "Captain America hero pack",
        "setup:hero:ms_marvel" => "Ms. Marvel hero pack",
        "setup:hero:doctor_strange" => "Doctor Strange hero pack",
        "setup:hero:hulk" => "Hulk hero pack",
        "setup:hero:thor" => "Thor hero pack",
        "setup:encounter-set:a_mess_of_things" or "setup:encounter-set:goblin_gimmicks"
            or "setup:encounter-set:power_drain" or "setup:encounter-set:running_interference" or "setup:campaign:risky_business" or "setup:campaign:risky_business_expert"
            or "setup:campaign:mutagen_formula" or "setup:campaign:mutagen_formula_expert"
            => "Green Goblin scenario pack",
        _ when source.StartsWith("setup:campaign:wrecking_crew_", StringComparison.Ordinal) => "The Wrecking Crew",
        _ => "Rise of Red Skull",
    };

    [Fact]
    public void CorePrintedFactsDoNotBorrowAnExpansionScenarioForSharedCards()
    {
        string feature = CardFaceSpecifications.Build();

        Assert.Contains("| rhino | spider_man | legions_of_hydra |", feature);
        Assert.DoesNotContain("| crossbones |", feature);
        Assert.DoesNotContain("| hawkeye |", feature);
    }
}
