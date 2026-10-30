using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual authored cards on a prepared board; this does not admit the Ant-Man pack.
public sealed class HankPymIndependentReviewTests
{
    [Theory]
    [InlineData("01072", 1)]
    [InlineData("01055", 1)]
    [InlineData("01088", 2)]
    [Rule("rr:cost.4.1")]
    public void ActualEntryUsesResourcesGeneratedWhenWhollyOverpaying(string resourceFace, long expected)
    {
        // Excess resources "were not paid for that cost"; ruling:5e6ac90dc4187eef
        // confirms The Power Of cards generate one resource for Hank's zero cost.
        var runner = AuthoredCards.Runner();
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var seat = world.Seats[0];
        var hank = world.CreateCard("12011", seat.Hand);
        var resource = world.CreateCard(resourceFace, seat.Hand);
        CardPlay.Play(world, world.Facts, runner, seat, hank, [resource.ObjectId], []);
        var interrupt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var offer = Assert.Single(interrupt.Affordances, item => item.AnchorId == hank.ObjectId);
        Sequence.Answer(world, world.Facts, runner, interrupt, Decision.Take(offer.Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(expected, hank.Tokens.GetValueOrDefault("c_pym"));
        Assert.Equal(expected, DamagePlacement.Health(world, world.Facts, hank));
        Assert.Equal(DeckType.AlliesArea, hank.Area.Type);
        Assert.Equal(DeckType.DiscardPile, resource.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
