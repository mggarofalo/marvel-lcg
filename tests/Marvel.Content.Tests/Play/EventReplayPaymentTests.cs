using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class EventReplayPaymentTests
{
    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void SecondEventPlayKeepsItsOwnResolutionWhenPaymentSuspends()
    {
        // "Pay the cost(s)." Synthetic instructions isolate a resource cost
        // that suspends the replay of one physical event in the same window.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"05003","abilities":[{
                "trigger":{"event":"WhenDamageWouldBeDealt","timing":"Interrupt","subject":"game"},
                "effect":{"draw":{"player":"you","count":1}}
              }]},
              {"card":"01030","abilities":[{
                "trigger":{"event":"WhenActionTriggered","timing":"Resource","subject":"game"},
                "cost":{"dealDamage":{"cards":"this","amount":2}},
                "effect":{"generate":"YY"}
              }]},
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
                "effect":{"draw":{"player":"you","count":1}}
              }]}
            ]}
            """));
        var (_, world) = Playing(board => InPlay(board, "01092"), abilities: runner);
        var played = world.CreateCard("05003", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        var generator = world.CreateCard("01030",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        generator.TakeDamage(2);
        var origin = new Occurrence(90, [Steps.DamageWouldBeDealt], Player: 0);
        var first = Assert.Single(runner.Waiting(world, origin, WindowKind.Interrupt));
        runner.ResolveWindow(world, origin, first, new(played.ObjectId, [], [payment.ObjectId]));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(ResolutionStatus.Resolved, origin.Resolution.CardStatus(played.ObjectId));
        World.MoveToTop(played, world.Seats[0].Hand);
        var second = Assert.Single(runner.Waiting(world, origin, WindowKind.Interrupt));
        Assert.Equal(1, second.EventPlay);
        int held = world.Seats[0].Hand.Cards.Count;
        var events = new List<GameEvent>();
        runner.ResolveWindow(world, origin, second, new(played.ObjectId, [], [generator.ObjectId]));
        var prompt = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.NotNull(prompt);
        Assert.Equal(held - 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(ResolutionStatus.Pending, origin.Resolution.StatusOf(second));
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, played.Area.Type);
        Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
        Assert.Equal(2, origin.EventPlay(played.ObjectId));
        Assert.Equal(ResolutionStatus.Resolved, origin.Resolution.StatusOf(first));
        Assert.Equal(ResolutionStatus.Resolved, origin.Resolution.StatusOf(second));
        var aggregates = origin.Resolutions.Where(entry => entry.Card == played.ObjectId
            && entry.Ability is null).ToArray();
        Assert.Equal([0, 1], aggregates.Select(entry => entry.EventPlay));
        Assert.All(aggregates, entry => Assert.Equal(ResolutionStatus.Resolved, entry.Status));
        Assert.False(world.Agenda.IsBusy);
    }
}
