using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CardPlayPaymentContinuationTests
{
    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void ACardEntersOnlyAfterItsResourceGeneratorsCostFinishes()
    {
        // "Pay the cost(s)." Card entry follows payment, including a generator's interrupt.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01063","abilities":[]},
              {"card":"01030","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
                "cost":{"dealDamage":{"cards":"this","amount":2}},
                "effect":{"generate":"Y"}
              }]},
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
                "effect":{"draw":{"player":"you","count":1}}
              }]}
            ]}
            """));
        Card? purchased = null;
        Card? generator = null;
        var (_, world) = Playing(board =>
        {
            purchased = board.CreateCard("01063", board.Seats[0].Hand);
            generator = board.CreateCard("01030",
                board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            generator.TakeDamage(2);
            InPlay(board, "01092");
        }, abilities: runner);
        var events = new List<GameEvent>();

        CardPlay.Play(world, world.Facts, runner, world.Seats[0], purchased!, [generator!.ObjectId], events);
        var entry = Assert.Single(world.Agenda.Outstanding, step => step.What == "CompleteCardPayment");
        var remainder = Assert.Single(world.Agenda.Outstanding, step => step.What == "ContinueResourcePayment");
        var (other, _) = FixedCountBoard(runner);
        string before = world.Digest().Canonical();
        string otherBefore = other.Digest().Canonical();
        Assert.Throws<InvalidOperationException>(() => AgendaProcedures.Apply(other, other.Facts, runner, entry, []));
        Assert.Throws<InvalidOperationException>(() => AgendaProcedures.Apply(other, other.Facts, runner, remainder, []));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(otherBefore, other.Digest().Canonical());
        var prompt = Sequence.Work(world, world.Facts, runner, events)!;

        Assert.Equal(Question.Opportunity, prompt.Asking);
        Assert.Equal(DeckType.RevealingArea, purchased!.Area.Type);
        Assert.Equal(4, generator.Damage);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
        Sequence.Finish(world, world.Facts, runner, events);

        Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
        Assert.Equal(DeckType.SupportsArea, purchased.Area.Type);
        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.IsOver);
        string completed = world.Digest().Canonical();
        Assert.Throws<InvalidOperationException>(() => AgendaProcedures.Apply(world, world.Facts, runner, entry, []));
        Assert.Throws<InvalidOperationException>(() => AgendaProcedures.Apply(world, world.Facts, runner, remainder, []));
        Assert.Equal(completed, world.Digest().Canonical());
    }
}
