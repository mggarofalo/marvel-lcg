using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Optional instructions and abilities that would change nothing are not offered.
public sealed class GrootNoEffectOfferTests : CoreCardTestBase
{
    [Theory]
    [InlineData(10, true, false)]
    [InlineData(10, false, true)]
    [InlineData(9, true, true)]
    [Rule("rr:max-maximum.6")]
    public void FloraAndFaunaIsNotOfferedWhenItWouldChangeNothing(int growth, bool ready, bool offered)
    {
        // 16020: "Place 2 growth counters on Groot (to a maximum of 10) and ready him."
        // At 10 counters with Groot ready nothing changes; rulings 342e7b62e13a6806 and
        // e015aa6cd98e8ef2: an ability with no effect cannot be initiated.
        var world = GrootBoards.Board();
        var groot = GrootBoards.Groot(world);
        GrootBoards.Grow(groot, growth);
        if (!ready) groot.Exhaust();
        GrootBoards.InPlay(world, "16019", DeckType.AlliesArea);
        var card = GrootBoards.Hand(world, "16020");
        GrootBoards.Hand(world, "01088");

        Assert.Equal(offered, GrootBoards.Runner(world).Actions(world, 0).Any(a => a.Card == card.ObjectId));
    }

    [Fact]
    [Rule("rr:choose-option.1")]
    public void WiltOffersNoFlipToTheFormGrootIsAlreadyIn()
    {
        // 16025: "You may flip to alter-ego form." In alter-ego form the flip has no
        // valid target (rr:choose-option.1), so only declining it is offered, as for
        // every Core obligation (specs/behavior/core/encounter-reveal.feature).
        var world = GrootBoards.Board("16001b");
        GrootEncounterTests.Reveal(world, "16025");
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, GrootBoards.Runner(world), []));

        Assert.Equal("Remain in your current form", Assert.Single(choice.Affordances).Description);
    }
}
