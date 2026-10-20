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
            "setup:campaign:red_skull", "setup:campaign:red_skull_expert",
            "setup:campaign:taskmaster", "setup:campaign:taskmaster_expert",
            "setup:campaign:zola", "setup:campaign:zola_expert",
            "setup:encounter-set:hydra_assault", "setup:encounter-set:hydra_patrol",
            "setup:encounter-set:weapon_master", "setup:hero:hawkeye", "setup:hero:spider_woman",
        };
        var expansion = Catalog.Build().Sources.Where(source =>
            source.Kind == "setup" && source.Disposition == "outside-core").ToArray();

        Assert.Equal(expected, expansion.Select(source => source.Id));
        Assert.All(expansion.SelectMany(source => source.Obligations), obligation =>
        {
            Assert.Equal("outside-core", obligation.Disposition);
            Assert.Null(obligation.Implementation);
            Assert.Empty(obligation.Scenarios);
            Assert.Contains("Rise of Red Skull", obligation.Reason);
        });
    }

    [Fact]
    public void CorePrintedFactsDoNotBorrowAnExpansionScenarioForSharedCards()
    {
        string feature = CardFaceSpecifications.Build();

        Assert.Contains("| rhino | spider_man | legions_of_hydra |", feature);
        Assert.DoesNotContain("| crossbones |", feature);
        Assert.DoesNotContain("| hawkeye |", feature);
    }
}
