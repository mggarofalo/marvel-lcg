using Marvel.Content.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class RunningInterferenceSetupTests
{
    [Fact]
    public void PrintedSetHasFivePhysicalCopiesAndStaysOutsidePublicAdmission()
    {
        // Generated printed membership and pinned MarvelSDB gob_encounter.json quantities.
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        Assert.Equal("Running Interference", setup.EncounterSetDisplayName("running_interference"));
        Assert.Equal(EncounterSetRole.Modular, setup.EncounterRole("running_interference"));
        Assert.Equal(["02046", "02047", "02048", "02049", "02049"], setup.EncounterSet("running_interference"));
        Assert.All(setup.EncounterSet("running_interference"), face =>
            Assert.Equal("running_interference", cards.EncounterSet(face)));
        var admitted = RuntimeProductAdmission.Discover(setup, cards, cards.Faces.ToHashSet(StringComparer.Ordinal));
        Assert.DoesNotContain("running_interference", admitted.Setup.EncounterSetNames);
        Assert.All(setup.EncounterSet("running_interference"), face => Assert.DoesNotContain(face, admitted.Playable));
        Assert.Contains("bomb_scare", admitted.Setup.EncounterSetNames);
    }
}
