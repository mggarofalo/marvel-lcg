using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual Clea on nonpublic boards, including a physically foreign owner.</summary>
public sealed class DoctorStrangeCleaCardTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:interrupt.3")]
    [Rule("rr:ownership-and-control.7.2")]
    public void DefeatInterruptUsesExactOwnerDeckAndSeededShuffle(bool accept, bool eliminated)
    {
        // "before that triggering condition resolves"; leaving play uses the owner's area,
        // "or removed from the game if that player is no longer in the game".
        var world = ThorActualFixture.Board("06001a", 3);
        var control = ThorActualFixture.Board("06001a", 3);
        var source = Ally(world);
        var expected = Ally(control);
        if (eliminated)
        {
            Elimination.Eliminate(world, world.Facts, 1, "CleaOwner", []);
            Elimination.Eliminate(control, control.Facts, 1, "CleaOwner", []);
            Assert.Equal(DeckType.RemovedArea, source.Area.Type);
            Assert.Equal(DeckType.RemovedArea, expected.Area.Type);
            // Synthetic late copies isolate absent-owner routing after normal cleanup.
            source = Ally(world);
            expected = Ally(control);
        }
        Assert.Equal(eliminated, world.Seats[1].Eliminated);
        Assert.Equal(DeckType.AlliesArea, source.Area.Type);
        int[] controllerDeck = [.. world.Seats[0].Deck.Cards.Select(card => card.ObjectId)];
        var destination = eliminated ? control.AreaOf(DeckType.RemovedArea)
            : accept ? control.Seats[1].Deck : control.AreaOf(DeckType.DiscardPile, PlayArea.Of(1));
        World.MoveToTop(expected, destination);
        if (accept && !eliminated) control.Shuffle(destination);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var events = new List<GameEvent>();
        DamagePlacement.Resolve(world, world.Facts, new(source, source, 2, "Clea", "Damage"), events);
        var prompt = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.True(prompt.Cancellable);
        Assert.Equal(0, prompt.Player);
        Assert.Single(prompt.Affordances);
        Sequence.Answer(world, world.Facts, runner, prompt,
            accept ? Decision.Take(prompt.Affordances[0].Id) : Decision.Decline, events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(expected.Area.Type, source.Area.Type);
        Assert.Equal(expected.Area.PlayArea, source.Area.PlayArea);
        Assert.Equal(control.Seats[1].Deck.Cards.Select(card => card.ObjectId),
            world.Seats[1].Deck.Cards.Select(card => card.ObjectId));
        Assert.Equal(controllerDeck, world.Seats[0].Deck.Cards.Select(card => card.ObjectId));
        Assert.Equal(control.Random.Generator.WordsConsumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(control.Random.Generator.GetState().Words, world.Random.Generator.GetState().Words);
        Assert.Equal(control.Random.Generator.GetState().Index, world.Random.Generator.GetState().Index);
        Assert.Single(events.OfType<CardsMoved>(), move => move.Cards.Any(card => card.Card == source.ObjectId));
        Assert.Equal(accept && !eliminated, world.Random.Generator.WordsConsumed > 0);
        Assert.False(world.Agenda.IsBusy);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void DirectDiscardDoesNotOfferTheDefeatInterrupt()
    {
        var world = ThorActualFixture.Board("06001a", 2);
        var source = Ally(world);
        long words = world.Random.Generator.WordsConsumed;
        var events = new List<GameEvent>();
        Discard.Card(world, source, "Discard", events);
        Sequence.Finish(world, world.Facts, world.Abilities!, events);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(PlayArea.Of(1), source.Area.PlayArea);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.False(world.Agenda.IsBusy);
    }

    private static Card Ally(World world) => world.CreateCard("09013",
        world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 1));
}
