using Marvel.Content.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class PowerDrainSetupTests
{
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void ModularRecipeMatchesThePrintedSetAndPhysicalQuantities()
    {
        // Authority: datasets/cards/cards.json identifies this five-face set;
        // the pinned MarvelSDB pack/gob_encounter.json records one each of
        // the five Power Drain cards, one physical copy of each.
        Assert.Equal("Power Drain", Setup.EncounterSetDisplayName("power_drain"));
        Assert.Equal(EncounterSetRole.Modular, Setup.EncounterRole("power_drain"));
        Assert.Equal(["02041", "02042", "02043", "02044", "02045"],
            Setup.EncounterSet("power_drain"));
        Assert.All(Setup.EncounterSet("power_drain"), face =>
        {
            Assert.Equal("gob", Cards.Pack(face));
            Assert.Equal("power_drain", Cards.EncounterSet(face));
        });
    }

    [Fact]
    public void IncompleteProductClosesModularRecipeDiscovery()
    {
        // Product admission requires every face, including faces outside this modular set.
        var admitted = RuntimeProductAdmission.Discover(Setup, Cards,
            Cards.Faces.Where(face => face != "02049").ToHashSet(StringComparer.Ordinal));

        Assert.Contains("power_drain", Setup.EncounterSetNames);
        Assert.DoesNotContain("power_drain", admitted.Setup.EncounterSetNames);
        Assert.All(Setup.EncounterSet("power_drain"), face =>
            Assert.DoesNotContain(face, admitted.Playable));
        Assert.Contains("bomb_scare", admitted.Setup.EncounterSetNames);
        Assert.Contains("hydra_patrol", admitted.Setup.EncounterSetNames);
    }
}
