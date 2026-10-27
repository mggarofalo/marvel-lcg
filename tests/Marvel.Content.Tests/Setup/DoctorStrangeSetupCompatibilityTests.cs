using Marvel.Content.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Setup.DoctorStrangeSetupFixture;

namespace Marvel.Content.Tests.Setup;

public sealed class DoctorStrangeSetupCompatibilityTests
{
    [Theory]
    [InlineData("rhino", "spider_man", "1d35698217cfae6385b4b6ca74ce4c281cd9c5958a77971e440d53af158f2cbc:100:3990082908")]
    [InlineData("crossbones", "hawkeye", "997010b9768435e0a24bd6daf523b22b2590d545a0a196f50b192b221493e76c:110:1600414041")]
    [InlineData("rhino", "captain_america", "4c6deb59e0b9e7353187d52416ab4ef682675aff336665a808bfd27fd5ead4a6:100:3990082908")]
    [InlineData("rhino", "ms_marvel", "3ad97a1941bab7af53190284fa635d69bd335e8d32ecb19e73fcb7d9285148df:100:3990082908")]
    [InlineData("rhino", "thor", "fc33b1483d861f3f9932c1ffdab563029141f86f17269b730ef6a5bc10c5e7f5:100:3990082908")]
    [InlineData("rhino", "black_widow", "3c608961518f91f3cbc7bb153ddd511d0a812fd7323f40b743d2ccaad535a66d:100:3990082908")]
    public void AcceptedSetupRetainsPrePrerequisiteDigestAndRandomState(string scenario, string hero, string expected)
    {
        // Engine compatibility controls captured at de695836, before this prerequisite.
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var blueprints = Marvel.Rules.Setup.Blueprints.From(Dealer.DealOrder(setup, scenario, [hero], facts: PrintedCards), PrintedCards);
        var world = WorldSetup.DealWithoutCardAbilities(PrintedCards, blueprints, [hero], 8123);
        string actual = world.Digest().Fingerprint() + ":" + world.Random.Generator.WordsConsumed
            + ":" + world.Random.Generator.NextUInt32();
        Assert.Equal(expected, actual);
        Assert.Null(world.Seats[0].InvocationDeck);
        Assert.DoesNotContain(world.Areas, area => area.Type is DeckType.AdditionalDeck or DeckType.AdditionalDiscardPile);
    }
}
