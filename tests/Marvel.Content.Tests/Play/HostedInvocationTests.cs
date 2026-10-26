using System.Text.Json;
using Marvel.Core.Random;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.HostedInvocationFixture;

namespace Marvel.Content.Tests.Play;

public sealed class HostedInvocationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:in-play-and-out-of-play.10")]
    public void RevealingStoresTheExactTopConcealedAndRefreshesOrRecyclesItsOwnDeck(bool last)
    {
        // "Facedown cards attached to in-play cards are out of play."
        // 09029: "Place the top card ... facedown under this scheme."
        var (world, deck, scheme, runner) = Board(last);
        var top = deck.PublicTop!;
        var ordinary = world.Seats.Select(seat => seat.Deck.Cards.ToArray()).ToArray();
        var expected = (last ? deck.Discard.Cards : deck.Deck.Cards.SkipLast(1)).ToList();
        var random = new EngineRandom(0);
        random.Generator.SetState(world.Random.Generator.GetState());
        if (last) random.Shuffle(expected);
        var events = new List<GameEvent>();

        Reveal(world, scheme, runner, events);

        Assert.Equal([top], BeneathCards.Of(world, scheme));
        Assert.Equal(DeckType.BeneathCardsPile, top.Area.Type);
        Assert.False(top.FaceUp);
        Assert.False(DeckTypes.IsInPlay(top.Area.Type));
        Assert.Equal(1, top.Owner);
        Assert.Equal(0, top.Incarnation);
        Assert.Equal(expected, deck.Deck.Cards);
        Assert.Equal([deck.PublicTop], deck.Deck.Cards.Where(card => card.FaceUp));
        Assert.Equal(random.Generator.GetState().Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Generator.GetState().Index, world.Random.Generator.GetState().Index);
        for (int seat = 0; seat < 2; seat++) Assert.Equal(ordinary[seat], world.Seats[seat].Deck.Cards);
        Assert.DoesNotContain(world.Areas, area => area.Type == DeckType.DealtEncounterCardsDeck && area.Cards.Count > 0);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:when-defeated-abilities.2.1")]
    public void DefeatShufflesTheSamePhysicalCardBeforeHostDepartureAndResumesAfterAChoice()
    {
        // "A 'When Defeated' ability is resolved before the defeated card leaves play."
        // The subsequent draw choice is synthetic continuation evidence.
        var (world, deck, scheme, runner) = Board(choice: true);
        var top = deck.PublicTop!;
        Reveal(world, scheme, runner, []);
        var expected = deck.Deck.Cards.Append(top).ToList();
        var random = new EngineRandom(0);
        random.Generator.SetState(world.Random.Generator.GetState());
        random.Shuffle(expected);
        var events = new List<GameEvent>();
        var prompt = Assert.IsType<Prompt>(Defeat(world, scheme, runner, events));
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        Assert.Equal(0, prompt.Player);
        Assert.Equal(expected, deck.Deck.Cards);
        Assert.Equal(1, top.Owner);
        Assert.Empty(BeneathCards.Of(world, scheme));
        Assert.Equal([deck.PublicTop], deck.Deck.Cards.Where(card => card.FaceUp));
        Assert.Equal(random.Generator.GetState().Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Generator.GetState().Index, world.Random.Generator.GetState().Index);
        string before = SpellMasteryFixture.Snapshot(world);
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(int.MaxValue), events));
        Assert.Equal(before, SpellMasteryFixture.Snapshot(world));
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(prompt.Affordances[0].Id), events);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(DeckType.EncounterDiscardPile, scheme.Area.Type);
        Assert.Same(deck.Deck, top.Area);
        Assert.DoesNotContain(events.OfType<CardsMoved>(), moved => moved.Verb == "Discard" && moved.Cards.Any(card => card.Card == top.ObjectId));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:attach-to.1")]
    public void NonDefeatDepartureDiscardsStoredInvocationWithoutShufflingItBack()
    {
        // "If the game element an attachment is attached to leaves play, the attachment is discarded."
        var (world, deck, scheme, runner) = Board();
        var top = deck.PublicTop!;
        Reveal(world, scheme, runner, []);
        var before = world.Random.Generator.GetState();
        Discard.Card(world, scheme, "ordinary departure", []);
        Assert.Same(deck.Discard, top.Area);
        Assert.True(top.FaceUp);
        Assert.Equal(4, deck.Deck.Cards.Count);
        Assert.Equal(before.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(before.Index, world.Random.Generator.GetState().Index);
        Assert.Empty(BeneathCards.Of(world, scheme));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnboundLookalikeAndForeignHostRefuseBeforeMutation(bool foreignHost)
    {
        var (world, _, scheme, runner) = Board();
        Reveal(world, scheme, runner, []);
        var host = scheme;
        if (foreignHost) host = Board().Scheme;
        else BeneathCards.Place(world, world.CreateCard("09036", world.Seats[1].Hand), scheme, false, "synthetic", []);
        string before = SpellMasteryFixture.Snapshot(world);
        var events = new List<GameEvent>();
        Assert.Throws<ArgumentException>(() => InvocationDeck.ShuffleFromBeneath(world, host, "return", events));
        Assert.Equal(before, SpellMasteryFixture.Snapshot(world));
        Assert.Empty(events);
    }

    [Fact]
    public void RepeatedQueriesLeaveTheUnprobedDefeatAndItsEventsUnchanged()
    {
        var (world, _, scheme, runner) = Board(last: true);
        var (control, _, other, otherRunner) = Board(last: true);
        Reveal(world, scheme, runner, []);
        Reveal(control, other, otherRunner, []);
        string before = SpellMasteryFixture.Snapshot(world);
        for (int i = 0; i < 3; i++)
        {
            _ = BeneathCards.Of(world, scheme);
            _ = runner.Actions(world, 0);
        }
        Assert.Equal(before, SpellMasteryFixture.Snapshot(world));
        var actual = new List<GameEvent>();
        var expected = new List<GameEvent>();
        Assert.Null(Defeat(world, scheme, runner, actual));
        Assert.Null(Defeat(control, other, otherRunner, expected));
        Assert.Equal(SpellMasteryFixture.Snapshot(control), SpellMasteryFixture.Snapshot(world));
        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual));
    }
}
