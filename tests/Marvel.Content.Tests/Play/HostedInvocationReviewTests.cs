using Marvel.Core.Random;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;
using static Marvel.Content.Tests.Play.HostedInvocationFixture;

namespace Marvel.Content.Tests.Play;

/// <summary>Independent nonpublic lifecycle controls; extra hosts are synthetic.</summary>
public sealed class HostedInvocationReviewTests
{
    [Fact]
    public void PrintedReturnShufflesOnlyTheDrawPileAndStoredCardLeavingDiscardUntouched()
    {
        // 09029: "Shuffle the Invocation card under here into the Invocation deck."
        // pack:mc08:the-invocation-deck recycles the discard only when the deck is empty.
        var (world, deck, scheme, runner) = Board();
        var stored = deck.PublicTop!;
        Reveal(world, scheme, runner, []);
        deck.DiscardTop(deck.PublicTop!, "prepare discard", []);
        deck.DiscardTop(deck.PublicTop!, "prepare discard", []);
        var discard = deck.Discard.Cards.ToArray();
        Assert.Equal(2, discard.Length);
        var expected = deck.Deck.Cards.Append(stored).ToList();
        var random = new EngineRandom(0);
        random.Generator.SetState(world.Random.Generator.GetState());
        random.Shuffle(expected);
        var events = new List<GameEvent>();

        Assert.Null(Defeat(world, scheme, runner, events));

        Assert.Equal(expected, deck.Deck.Cards);
        Assert.Equal(discard, deck.Discard.Cards);
        Assert.Equal(5, deck.Deck.Cards.Concat(deck.Discard.Cards).Distinct().Count());
        Assert.Equal(random.Generator.GetState().Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Generator.GetState().Index, world.Random.Generator.GetState().Index);
        Assert.All(discard, card => Assert.True(card.FaceUp));
        Assert.Equal([deck.PublicTop], deck.Deck.Cards.Where(card => card.FaceUp));
        int detached = events.FindIndex(happened => happened is CardDetached card && card.Card == stored.ObjectId);
        int departed = events.FindIndex(happened => happened is CardsMoved moved
            && moved.Cards.Any(card => card.Card == scheme.ObjectId));
        Assert.True(detached >= 0 && departed > detached);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void DefeatingOneHostNeverReturnsTheInvocationBeneathAnotherHost()
    {
        // Synthetic second physical host isolates the host-keyed return operation.
        var (world, deck, first, runner) = Board();
        var firstCard = deck.PublicTop!;
        Reveal(world, first, runner, []);
        var secondCard = deck.PublicTop!;
        var second = world.CreateCard("09029", world.AreaOf(DeckType.EncounterDeck));
        Reveal(world, second, runner, []);

        Assert.Null(Defeat(world, first, runner, []));

        Assert.Same(deck.Deck, firstCard.Area);
        Assert.Equal([secondCard], BeneathCards.Of(world, second));
        Assert.False(secondCard.FaceUp);
        Assert.Equal(1, secondCard.Owner);
        Assert.Equal(DeckType.SideSchemesArea, second.Area.Type);
        Assert.Equal(4, deck.Deck.Cards.Count);
        Assert.Null(Defeat(world, second, runner, []));
        Assert.Equal(5, deck.Deck.Cards.Distinct().Count());
        Assert.Empty(BeneathCards.Of(world, first));
        Assert.Empty(BeneathCards.Of(world, second));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void ReturningToAnEmptyDeckRevealsItsSinglePhysicalCardWithoutRandomConsumption()
    {
        // Synthetic additional hosts exercise an empty deck with no discard to recycle.
        // pack:mc08:the-invocation-deck: "top card ... faceup at all times".
        var (world, deck, scheme, runner) = Board();
        var returned = deck.PublicTop!;
        Reveal(world, scheme, runner, []);
        var otherHosts = new List<Card>();
        while (deck.PublicTop is { } card)
        {
            var host = world.CreateCard("09029", world.AreaOf(DeckType.SideSchemesArea));
            otherHosts.Add(host);
            BeneathCards.Place(world, card, host, false, "synthetic storage", []);
        }
        Assert.Empty(deck.Deck.Cards);
        Assert.Empty(deck.Discard.Cards);
        var before = world.Random.Generator.GetState();
        var consumed = world.Random.Generator.WordsConsumed;

        Assert.Null(Defeat(world, scheme, runner, []));

        Assert.Same(returned, Assert.Single(deck.Deck.Cards));
        Assert.Same(returned, deck.PublicTop);
        Assert.True(returned.FaceUp);
        Assert.Equal(1, returned.Owner);
        Assert.Equal(before.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(before.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(consumed, world.Random.Generator.WordsConsumed);
        Assert.All(otherHosts, host => Assert.False(Assert.Single(BeneathCards.Of(world, host)).FaceUp));
        Assert.Empty(world.Agenda.Outstanding);
    }
}
