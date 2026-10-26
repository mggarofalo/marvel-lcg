using Marvel.Content.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class MessOfThingsSetupTests
{
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void ModularRecipeMatchesThePrintedSetAndPhysicalQuantities()
    {
        // Authority: datasets/cards/cards.json identifies this four-face set;
        // the pinned MarvelSDB pack/gob_encounter.json records one each of
        // A Mess of Things, Scorpion and Gang-Up, and two Tail Sweep copies.
        Assert.Equal("A Mess of Things", Setup.EncounterSetDisplayName("a_mess_of_things"));
        Assert.Equal(EncounterSetRole.Modular, Setup.EncounterRole("a_mess_of_things"));
        Assert.Equal(["02037", "02038", "02039", "02040", "02040"],
            Setup.EncounterSet("a_mess_of_things"));
        Assert.All(Setup.EncounterSet("a_mess_of_things"), face =>
        {
            Assert.Equal("gob", Cards.Pack(face));
            Assert.Equal("a_mess_of_things", Cards.EncounterSet(face));
        });
    }

    [Fact]
    public void ResearchModularRecipeStaysOutsideRuntimeDiscoveryEvenWithEveryFaceAuthored()
    {
        // Product acceptance is repository policy. Complete authoring alone
        // cannot admit the Green Goblin product or one of its modular sets.
        var admitted = RuntimeProductAdmission.Discover(Setup, Cards,
            Cards.Faces.ToHashSet(StringComparer.Ordinal));

        Assert.Contains("a_mess_of_things", Setup.EncounterSetNames);
        Assert.DoesNotContain("a_mess_of_things", admitted.Setup.EncounterSetNames);
        Assert.All(Setup.EncounterSet("a_mess_of_things"), face =>
            Assert.DoesNotContain(face, admitted.Playable));
        Assert.Contains("bomb_scare", admitted.Setup.EncounterSetNames);
        Assert.Contains("hydra_patrol", admitted.Setup.EncounterSetNames);
    }
}
