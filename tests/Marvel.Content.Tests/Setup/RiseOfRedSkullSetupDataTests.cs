using Marvel.Content.Setup;
using Marvel.Rules.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class RiseOfRedSkullSetupDataTests
{
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    [Fact]
    public void AuthoredProductsHaveCompleteScenarioAndIdentityRecipes()
    {
        Assert.Equal(16, Setup.CampaignNames.Count);
        Assert.Equal(7, Setup.HeroNames.Count);
        Assert.Equal(10, Setup.EncounterSetNames.Count);
        foreach (string scenario in new[] { "crossbones", "absorbing_man", "taskmaster", "zola", "red_skull" })
        {
            var standard = Setup.Campaign(scenario);
            var expert = Setup.Campaign(scenario + "_expert");
            Assert.False(standard.Expert);
            Assert.True(expert.Expert);
            Assert.Equal(standard.Villain[1], expert.Villain[0]);
            Assert.Equal(standard.Encounters, expert.Encounters);
            Assert.Equal(standard.SetAside, expert.SetAside);
            Assert.Contains("expert", expert.EncounterSets);
            Assert.DoesNotContain("expert", standard.EncounterSets);
        }
        Assert.Throws<KeyNotFoundException>(() => Setup.Campaign("unus"));
        Assert.Throws<KeyNotFoundException>(() => Setup.Hero("sp_dr"));
    }

    // Authority: pack:mc10:scenario-1-crossbones
    // Authority: pack:mc10:scenario-3-taskmaster
    [Fact]
    public void RequiredSetsCannotBeReplacedAsOptionalModularSets()
    {
        // Crossbones: "The Hydra Assault and Weapon Master sets can be removed".
        // Taskmaster: "The Hydra Patrol set ... is required when playing Taskmaster."
        var crossbones = Setup.Campaign("crossbones");
        Assert.Contains("legions_of_hydra", crossbones.EncounterSets);
        Assert.Equal(["hydra_assault", "weapon_master"], crossbones.ModularSets);
        Assert.Equal(["04072", "04073", "04074", "04075"], crossbones.SetAside);
        var taskmaster = Setup.Campaign("taskmaster");
        Assert.Contains("hydra_patrol", taskmaster.EncounterSets);
        Assert.Equal(["weapon_master"], taskmaster.ModularSets);
        Assert.Equal(["04097", "04098", "04099", "04100"], taskmaster.SetAside);
    }

    // Authority: pack:mc10:hawkeye-starter-deck
    // Authority: pack:mc10:spider-woman-starter-deck
    [Fact]
    public void PrintedStarterDeckMultiplicitiesAndIdentityExceptionArePreserved()
    {
        // "Expert Marksman (x2)"; "Finesse (x2)"; "Self-Propelled Glide (x3)".
        var hawkeye = Setup.Hero("hawkeye");
        var spiderWoman = Setup.Hero("spider_woman");
        Assert.Equal(2, hawkeye.HeroDeck.Count(face => face == "04010"));
        Assert.Equal(2, spiderWoman.HeroDeck.Count(face => face == "04033"));
        Assert.Equal(3, spiderWoman.HeroDeck.Count(face => face == "04039"));
        Assert.Equal(DeckAspectRule.EqualTwoAspects, spiderWoman.AspectRule);
        Assert.Equal(15, hawkeye.HeroDeck.Count);
        Assert.Equal(25, hawkeye.PlayerDeck.Count);
        Assert.Equal(15, spiderWoman.HeroDeck.Count);
        Assert.Equal(25, spiderWoman.PlayerDeck.Count);
    }
}
