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
        var admitted = RuntimeProductAdmission.Discover(Setup, Cards,
            Cards.Faces.ToHashSet(StringComparer.Ordinal)).Setup;
        Assert.Contains("a_mess_of_things", admitted.EncounterSetNames);
        var modular = admitted.EncounterSetNames
            .Where(name => ModularEncounterSets.IsModular(admitted, Cards, name))
            .ToList();

        Assert.Equal(
            [
                "running_interference", "power_drain", "a_mess_of_things", "bomb_scare",
                "masters_of_evil", "under_attack", "legions_of_hydra", "the_doomsday_chair",
                "hydra_assault", "weapon_master", "hydra_patrol", "goblin_gimmicks", "kree_fanatic", "power_stone"],
            modular);
        foreach (string name in modular)
        {
            Assert.NotEmpty(Dealer.DealOrder(
                admitted, "rhino", ["spider_man"], [name], Cards));
        }

        foreach (string name in (string[])["standard", "expert"])
        {
            Assert.False(ModularEncounterSets.IsModular(admitted, Cards, name));
            Assert.Throws<ArgumentException>(() => Dealer.DealOrder(
                admitted, "rhino", ["spider_man"], [name], Cards));
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
                "broken": { "name": "Broken", "role": "modular", "encounters": [] }
              }
            }
            """);

        var failure = Assert.Throws<ArgumentException>(() =>
            ModularEncounterSets.IsModular(malformed, Cards, "broken"));

        Assert.Contains("0 printed set icons", failure.Message, StringComparison.Ordinal);
    }
}
