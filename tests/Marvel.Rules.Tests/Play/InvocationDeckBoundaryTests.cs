using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>Synthetic topology and physical-lifetime controls for the Invocation binding.</summary>
public sealed class InvocationDeckBoundaryTests
{
    [Theory]
    [InlineData(DeckType.AdditionalDeck)]
    [InlineData(DeckType.AdditionalDiscardPile)]
    public void AnExistingAdditionalAreaRejectsSetupBeforeAnyMutation(DeckType type)
    {
        var (world, cards) = Board();
        world.CreateArea(type, 0, PlayArea.Of(0));
        var digest = world.Digest().Canonical();
        var areas = world.Areas.ToArray();
        var rng = world.Random.Generator.GetState();
        var events = new List<GameEvent>();

        Assert.Throws<InvalidOperationException>(() =>
            InvocationDeck.Initialize(world, 0, cards, "setup", events));

        Assert.Null(world.Seats[0].InvocationDeck);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(areas, world.Areas);
        Assert.Empty(events);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(0, world.Random.Generator.WordsConsumed);
    }

    [Rule("rr:leaves-play.1")]
    [Fact]
    public void PhysicalMembershipSurvivesDepartureAndReentryWhileCopyStateDoesNot()
    {
        // "When a card leaves play, there is no memory of its previous state".
        // This synthetic in-play detour checks physical binding, not Invocation play legality.
        var (world, cards) = Board();
        var invocation = InvocationDeck.Initialize(world, 0, cards, "setup", []);
        var card = cards[0];
        var play = world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0);
        World.MoveToTop(card, play);
        int incarnation = card.Incarnation;
        card.TakeDamage(3);
        card.Exhaust();
        var oldCopy = card.InstanceState;

        Discard.Card(world, card, "departure", []);

        Assert.Same(invocation.Discard, card.Area);
        Assert.NotSame(oldCopy, card.InstanceState);
        Assert.Equal(0, card.Damage);
        Assert.True(card.Ready);
        World.MoveToTop(card, play);
        Assert.Equal(incarnation + 1, card.Incarnation);
        Discard.Card(world, card, "reentry-departure", []);
        Assert.Same(invocation.Discard, card.Area);
        Assert.Equal(0, card.Owner);
        Assert.Single(invocation.Discard.Cards);
        Assert.DoesNotContain(world.Areas, area =>
            area.Type == DeckType.DiscardPile && area.Cards.Contains(card));
    }

    [Theory]
    [InlineData("below")]
    [InlineData("discarded")]
    [InlineData("unbound")]
    [InlineData("setup")]
    [Rule("rr:referential-ability.step.1")]
    public void AResolvingInvocationPlacesItselfFromAnywhereInItsDrawPileAndNowhereElse(string where)
    {
        // "The card on which the referential ability is printed": an Invocation's
        // "Place this card in the Invocation deck discard pile" follows that physical
        // card after an interrupt shuffles it below the public top.
        var (world, cards) = Board();
        var invocation = InvocationDeck.Initialize(world, 0, cards, "setup", []);
        var game = Game.Begin(world, world.Facts, new NoCardAbilities());
        if (where != "setup")
            while (game.Phase == GamePhase.Mulligan)
                game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));
        var top = invocation.Deck.Cards[^1];
        var card = where switch
        {
            "unbound" => world.Seats[0].IdentityCard!,
            "discarded" => top,
            _ => invocation.Deck.Cards[0],
        };
        if (where == "discarded") invocation.DiscardTop(top, "prepare", []);
        string before = world.Digest().Canonical();
        var events = new List<GameEvent>();

        if (where == "below")
        {
            invocation.DiscardResolved(card, "resolve", events);
            Assert.Equal([card], invocation.Discard.Cards);
            Assert.True(card.FaceUp);
            Assert.Same(top, invocation.PublicTop);
            Assert.Equal([top], invocation.Deck.Cards.Where(item => item.FaceUp));
            Assert.Single(events.OfType<CardsMoved>(), move => move.Cards.Single().Card == card.ObjectId);
            return;
        }
        if (where == "discarded")
            Assert.Throws<RulesNotImplementedException>(() => invocation.DiscardResolved(card, "resolve", events));
        else
            Assert.Throws<ArgumentException>(() => invocation.DiscardResolved(card, "resolve", events));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Empty(events);
    }

    private static (World World, Card[] Cards) Board()
    {
        var world = new World(new Printed(), 1, 5489);
        var seat = world.CreateSeat("Strange");
        seat.IdentityCard = world.CreateCard("09001b,09001a", seat.Hero);
        var source = world.CreateArea(DeckType.AsideDeck, 0, PlayArea.Of(0));
        return (world, Enumerable.Range(32, 5)
            .Select(number => world.CreateCard($"090{number}", source)).ToArray());
    }

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId == "09001b" ? CardKind.AlterEgo : CardKind.Event;
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
