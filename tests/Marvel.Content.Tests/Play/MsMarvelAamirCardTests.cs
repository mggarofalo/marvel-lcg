using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.ActionAbilitySupportFixtures;
using static Marvel.Content.Tests.Play.ActionAbilitySupportFixtures6;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual authored Aamir row in prepared nonpublic card-execution scenes.</summary>
public sealed class MsMarvelAamirCardTests
{
    [Rule("rr:then")]
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void SelectedDiscardGoesBelowExistingDeckBeforeDrawing(int existing)
    {
        // Printed 05006: "place 1 card from your discard pile on the bottom of your deck, then draw 1 card."
        // rr:then: "the text preceding the word 'then' must be fully true or resolved".
        Card? source = null;
        Card? untouched = null;
        Card? selected = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, "05006");
            var pile = board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
            untouched = board.CreateCard("01003", pile);
            selected = board.CreateCard("01004", pile);
        }, hero: false, abilities: AuthoredCards.Runner());
        var deck = world.Seats[0].Deck;
        foreach (var card in deck.Cards.ToArray()) World.MoveToTop(card, world.Seats[0].Hand);
        var originals = Enumerable.Range(0, existing).Select(_ => world.CreateCard("01002", deck)).ToArray();
        var discard = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
        int handCount = world.Seats[0].Hand.Cards.Count;
        long words = world.Random.Generator.WordsConsumed;
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == source!.ObjectId).Id));
        Assert.False(source!.Ready);
        var choice = game.Pending!;
        Assert.Equal(new[] { untouched!.ObjectId, selected!.ObjectId }.Order(), choice.Affordances.Select(a => a.Id).Order());
        var result = game.Resolve(Decision.Take(selected!.ObjectId));
        Assert.Equal(handCount + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(existing == 0 ? selected : originals[^1], world.Seats[0].Hand.Cards[^1]);
        if (existing == 0)
        {
            // Drawing the sole placed card empties the deck; normal reset shuffles the remaining discard.
            Assert.Equal(new[] { untouched }, deck.Cards);
            Assert.Empty(discard.Cards);
        }
        else
        {
            Assert.Equal(new[] { selected }.Concat(originals.SkipLast(1)), deck.Cards);
            Assert.Equal(new[] { untouched }, discard.Cards);
        }
        Assert.Equal(0, selected!.Owner);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        var movement = Assert.Single(result.Events.OfType<CardsMoved>(), e => e.Verb == "Place_On_Bottom");
        Assert.Equal(new Landing(selected!.ObjectId, 0), Assert.Single(movement.Cards));
    }

    [Fact]
    public void RejectedForeignSelectionPreservesChoiceStateRandomAndPendingWork()
    {
        Card? source = null;
        Card? own = null;
        Card? foreign = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, "05006");
            own = board.CreateCard("01003", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
            foreign = board.CreateCard("01004", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
        }, heroes: ["spider_man", "captain_marvel"], hero: false, abilities: AuthoredCards.Runner());
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances, a => a.AnchorId == source!.ObjectId).Id));
        var prompt = game.Pending;
        var digest = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Current;
        Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(foreign!.ObjectId)));
        Assert.Same(prompt, game.Pending);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Current);
        game.Resolve(Decision.Take(own!.ObjectId));
        Assert.Same(world.Seats[0].Deck, own!.Area);
        Assert.Same(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1), foreign!.Area);
    }

    [Fact]
    public void EmptyDiscardDoesNotOfferTheActionOrExhaustItsSource()
    {
        Card? source = null;
        var (game, _) = Playing(board => source = InPlay(board, "05006"), hero: false, abilities: AuthoredCards.Runner());
        Assert.DoesNotContain(game.Pending!.Affordances, a => a.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void HeroFormOrExhaustionPreventsTheAction(bool hero, bool exhausted)
    {
        Card? source = null;
        var (game, _) = Playing(board =>
        {
            source = InPlay(board, "05006");
            if (exhausted) source.Exhaust();
            board.CreateCard("01003", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        }, hero: hero, abilities: AuthoredCards.Runner());
        Assert.DoesNotContain(game.Pending!.Affordances, a => a.AnchorId == source!.ObjectId);
        Assert.Equal(!exhausted, source!.Ready);
    }
    [Fact]
    public void NonFirstControllersDiscardDeckAndHandAreUsed()
    {
        Card? source = null;
        Card? selected = null;
        Card? foreign = null;
        var (_, world) = Playing(board =>
        {
            source = board.CreateCard("05006", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
            selected = board.CreateCard("01004", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
            foreign = board.CreateCard("01003", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        }, heroes: ["spider_man", "captain_marvel"], abilities: AuthoredCards.Runner());
        var runner = AuthoredCards.Runner();
        var original = world.Seats[1].Deck.Cards.ToArray();
        int[] held = world.Seats.Select(seat => seat.Hand.Cards.Count).ToArray();
        var action = Assert.Single(runner.Actions(world, 1), a => a.Card == source!.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        var pending = Assert.Single(world.Agenda.Outstanding);
        runner.Chose(world, pending, Decision.Take(selected!.ObjectId));
        Assert.Equal(new[] { selected }.Concat(original.SkipLast(1)), world.Seats[1].Deck.Cards);
        Assert.Same(world.Seats[1].Hand, original[^1].Area);
        Assert.Equal(held[0], world.Seats[0].Hand.Cards.Count);
        Assert.Equal(held[1] + 1, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, foreign!.Area.Type);
        Assert.Equal(1, selected.Owner);
        Assert.False(source!.Ready);
    }

}
