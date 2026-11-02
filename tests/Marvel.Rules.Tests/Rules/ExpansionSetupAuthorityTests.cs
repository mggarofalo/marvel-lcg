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
            "setup:campaign:brotherhood_of_badoon", "setup:campaign:brotherhood_of_badoon_expert",
            "setup:campaign:crossbones", "setup:campaign:crossbones_expert",
            "setup:campaign:kang", "setup:campaign:kang_expert",
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
            "setup:encounter-set:anachronauts", "setup:encounter-set:band_of_badoon",
            "setup:encounter-set:goblin_gimmicks",
            "setup:encounter-set:hydra_assault", "setup:encounter-set:hydra_patrol",
            "setup:encounter-set:kree_fanatic", "setup:encounter-set:mot",
            "setup:encounter-set:power_drain", "setup:encounter-set:power_stone", "setup:encounter-set:running_interference",
            "setup:encounter-set:ship_command", "setup:encounter-set:temporal", "setup:encounter-set:weapon_master", "setup:hero:ant_man", "setup:hero:black_widow", "setup:hero:captain_america",
            "setup:hero:doctor_strange", "setup:hero:groot", "setup:hero:hawkeye", "setup:hero:hulk", "setup:hero:ms_marvel", "setup:hero:quicksilver", "setup:hero:rocket_raccoon", "setup:hero:scarlet_witch", "setup:hero:spider_woman", "setup:hero:thor",
            "setup:hero:wasp",
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
        "setup:hero:ant_man" => "Ant-Man hero pack",
        "setup:hero:black_widow" => "Black Widow hero pack",
        "setup:hero:captain_america" => "Captain America hero pack",
        "setup:hero:ms_marvel" => "Ms. Marvel hero pack",
        "setup:hero:doctor_strange" => "Doctor Strange hero pack",
        "setup:hero:hulk" => "Hulk hero pack",
        "setup:hero:thor" => "Thor hero pack",
        "setup:hero:wasp" => "Wasp hero pack",
        "setup:hero:quicksilver" => "Quicksilver hero pack",
        "setup:hero:groot" or "setup:hero:rocket_raccoon" or "setup:encounter-set:power_stone"
            or "setup:encounter-set:ship_command" or "setup:encounter-set:band_of_badoon"
            or "setup:campaign:brotherhood_of_badoon" or "setup:campaign:brotherhood_of_badoon_expert" => "The Galaxy's Most Wanted",
        "setup:hero:scarlet_witch" => "Scarlet Witch hero pack",
        "setup:encounter-set:kree_fanatic" => "Ronan Kree Fanatic modular set",
        "setup:encounter-set:anachronauts" or "setup:encounter-set:mot" or "setup:encounter-set:temporal"
            or "setup:campaign:kang" or "setup:campaign:kang_expert" => "The Once and Future Kang scenario pack",
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
