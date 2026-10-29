using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class EntryPaymentOriginReviewTests
{
    [Fact]
    public void MovingPendingTargetOutOfItsOriginRejectsPaidEntry()
    {
        var runner = new AbilityRunner(AbilityCatalog.Parse("{\"cards\":[]}"));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var seat = world.Seats[0];
        var target = world.CreateCard("06012", seat.Hand);
        var resource = world.CreateCard("01003", seat.Hand);
        var payment = CardPlay.Spend(world, world.Facts, runner, [seat.Hand], [resource.ObjectId],
            1, "", -1, 0, [], payingFor: target);
        var receipt = EntryPayment.Capture(target, 0, payment);
        World.MoveToTop(target, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        Assert.Throws<RulesNotImplementedException>(() => AllyCardEntry.PutPaid(world, world.Facts, runner, receipt, "test", []));
        Assert.Equal(DeckType.DiscardPile, target.Area.Type);
    }

    [Fact]
    [Rule("rr:ignore.1")]
    public void IgnoringResourceCostProducesNoEntryPayment()
    {
        // Ignoring a resource cost means "no resources are considered paid".
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"06012","abilities":[{
              "trigger":{"event":"WhenCardEntersPlay","timing":"Response","subject":"this"},
              "effect":{"placeCounters":{"card":"this","counter":"test","count":1}}}]}]}
            """));
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var seat = world.Seats[0];
        var ally = world.CreateCard("06012", seat.Hand);
        CardPlay.PlayIgnoringResourceCost(world, world.Facts, runner, seat, ally, []);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Null(world.Agenda.Occurrence!.EntryPayment);
        Assert.Equal("", Assert.IsType<PlayedCardPayment>(world.Agenda.Occurrence.CardPlayPayment).Paid);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
        Sequence.Finish(world, world.Facts, runner, []);
    }
}

