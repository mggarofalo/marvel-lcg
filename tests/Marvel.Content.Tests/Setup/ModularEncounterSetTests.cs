using Marvel.Content.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class ModularEncounterSetTests
{
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void DiscoveryClassificationAndDealerValidationShareOneBoundary()
    {
        var modular = Setup.EncounterSetNames
            .Where(name => ModularEncounterSets.IsModular(Setup, Cards, name))
            .ToList();

        Assert.Equal(
            [
                "bomb_scare", "masters_of_evil", "under_attack",
                "legions_of_hydra", "the_doomsday_chair",
            ],
            modular);
        foreach (string name in modular)
        {
            Assert.NotEmpty(Dealer.DealOrder(
                Setup, "rhino", ["spider_man"], [name], Cards));
        }

        foreach (string name in (string[])["standard", "expert"])
        {
            Assert.False(ModularEncounterSets.IsModular(Setup, Cards, name));
            Assert.Throws<ArgumentException>(() => Dealer.DealOrder(
                Setup, "rhino", ["spider_man"], [name], Cards));
        }
    }

    [Fact]
    public void EncounterSetDisplayNamesAreAuthoredRatherThanDerivedFromKeys()
    {
        Assert.Equal("The Doomsday Chair", Setup.EncounterSetDisplayName("the_doomsday_chair"));
    }

    [Fact]
    public void ASetWithoutOnePrintedIconCannotBeAdvertisedAsModular()
    {
        SetupCatalog malformed = SetupCatalog.Parse(
            """
            {
              "campaigns": {},
              "heroes": {},
              "encounter_sets": {
                "broken": { "name": "Broken", "encounters": [] }
              }
            }
            """);

        var failure = Assert.Throws<ArgumentException>(() =>
            ModularEncounterSets.IsModular(malformed, Cards, "broken"));

        Assert.Contains("0 printed set icons", failure.Message, StringComparison.Ordinal);
    }
}
