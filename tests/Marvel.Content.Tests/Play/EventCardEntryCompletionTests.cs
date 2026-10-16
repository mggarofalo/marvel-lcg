using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class EventCardEntryCompletionTests
{
    [Fact]
    [Rule("rr:play-put-into-play.2")]
    public void APlayedEventStaysFaceupUntilItsChoiceAndEffectComplete()
    {
        // "Resolve its ability, and place the card in its owner's discard pile."
        // Synthetic instructions isolate repeated suspension of the engine's event-entry contract.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01086","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
              "effect":{"seq":[
                {"choose":{"descriptions":["One","Two"],"options":[
                  {"draw":{"player":"you","count":1}},
                  {"draw":{"player":"you","count":2}}
                ]}},
                {"choose":{"descriptions":["One more","Two more"],"options":[
                  {"draw":{"player":"you","count":1}},
                  {"draw":{"player":"you","count":2}}
                ]}}
              ]}
            }]}]}
            """));
        var (_, world) = Playing(_ => { }, abilities: runner);
        var card = world.CreateCard("01086", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        int held = world.Seats[0].Hand.Cards.Count;
        var events = new List<GameEvent>();

        CardPlay.Play(world, world.Facts, runner, world.Seats[0], card, [payment.ObjectId], events);
        var prompt = Sequence.Work(world, world.Facts, runner, events)!;

        Assert.Equal(DeckType.RevealingArea, card.Area.Type);
        Assert.Equal(held - 2, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(2, prompt.Affordances.Count);
        var choice = prompt.Affordances[1];
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(choice.Id), events);
        var second = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(DeckType.RevealingArea, card.Area.Type);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        Sequence.Answer(world, world.Facts, runner, second, Decision.Take(second.Affordances[0].Id), events);
        Sequence.Finish(world, world.Facts, runner, events);

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }
}
