using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RiflePlacementTests : CoreCardTestBase
{
    [Theory]
    [InlineData(true, "Reveal")]
    [InlineData(false, "Reveal")]
    [InlineData(true, "Put_Into_Play")]
    [InlineData(false, "Put_Into_Play")]
    [Rule("rr:attach-to")]
    public void PrintedHostFallbackAppliesToEveryEntry(bool crossfireInPlay, string verb)
    {
        // "it must be attached to ... the specified game element as it enters play."
        var world = Hero("01001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var crossfire = world.CreateCard("04027", crossfireInPlay
            ? world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)) : world.AreaOf(DeckType.EncounterDiscardPile));
        var rifle = world.CreateCard("04029", world.AreaOf(DeckType.EncounterDiscardPile));
        var receipt = new EffectCompletion(1, 1);
        EncounterEntry.Schedule(world, EncounterEntryRequest.For(rifle, 0,
            new CardEntryPlacement(Verb: verb)), receipt);
        Sequence.Finish(world, Cards, runner, []);
        Assert.True(receipt.IsComplete);
        Assert.Equal(1, receipt.Applied);
        Assert.Equal(crossfireInPlay ? crossfire.ObjectId : villain.ObjectId, rifle.Area.Host);
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("[{\"query\":\"villain\"}]")]
    public void FallbackRequiresAnOrderedBoundedChoice(string options)
    {
        Assert.Throws<AbilityException>(() => new AbilityRunner(AbilityCatalog.Parse(
            "{\"cards\":[{\"card\":\"04029\",\"attachTo\":{\"firstAvailable\":" + options + "},\"abilities\":[]}]}")));
    }
}

