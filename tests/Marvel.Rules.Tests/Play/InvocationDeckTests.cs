using Marvel.Core.Random;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>Nonpublic Doctor Strange lifecycle probes against the mc08 insert.</summary>
public sealed class InvocationDeckTests
{
    [Rule("rr:special")]
    [Fact]
    public void AnInvocationSpecialWithoutAnImplementationRaisesAtTheExistingContinuationBoundary()
    {
        // "Special abilities may only be resolved through the explicit instruction
        // of another card ability." A synthetic instruction reaches the normal
        // Special procedure; this foundation does not admit any Special body.
        var (world, cards) = Board();
        var invocation = InvocationDeck.Initialize(world, 0, cards, "setup", []);
        CompleteMulligan(Game.Begin(world, world.Facts, new NoCardAbilities()));
        var top = invocation.PublicTop!;
        var digest = world.Digest().Canonical();
        world.Agenda.Add(new PhaseStep(
            new AbilityContinuationAgendaOperation(AbilityContinuationStage.Special),
            1, 1, Subject: top.ObjectId, Seat: 0, Plan: true));

        var error = Assert.Throws<RulesNotImplementedException>(() => Sequence.Work(world, world.Facts, new NoCardAbilities(), []));

        Assert.Contains(top.FaceId, error.Message);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Same(top, invocation.PublicTop);
    }

    [Fact]
    public void FivePhysicalCardsShuffleSeparatelyOnTheWorldStreamAndUseExistingDigestZones()
    {
        // pack:mc08:the-invocation-deck: "shuffle all five ... Invocation cards together".
        var (world, cards) = Board();
        var playerCards = world.Seats[0].Deck.Cards.ToArray();
        var expected = cards.ToList();
        var random = new EngineRandom(5489);
        random.Shuffle(expected);
        var events = new List<GameEvent>();

        var invocation = InvocationDeck.Initialize(world, 0, cards, "setup", events);

        Assert.Same(invocation, world.Seats[0].InvocationDeck);
        Assert.Equal(expected, invocation.Deck.Cards);
        Assert.Equal(playerCards, world.Seats[0].Deck.Cards);
        Assert.Equal(random.Generator.GetState().Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Generator.GetState().Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Generator.WordsConsumed, world.Random.Generator.WordsConsumed);
        Assert.All(cards, card => { Assert.Equal(0, card.Owner); Assert.False(card.FaceUp); });
        Assert.Null(invocation.PublicTop);
        Assert.Empty(invocation.Discard.Cards);
        Assert.Equal(5, events.OfType<CardsMoved>().Count());
        Assert.Single(events.OfType<AreaReordered>());
        var digest = world.Digest().Canonical();
        foreach (var card in cards)
            Assert.Contains($"\"id\":{card.ObjectId},\"card\":\"{card.FaceId}\",\"zone\":\"AdditionalDeck\",\"owner\":0", digest);
        Assert.StartsWith("{\"v\":3,\"cards\":[", digest);
    }

    [Fact]
    public void TopStaysHiddenThroughEveryPlayersMulliganThenOnlyTheTopTurnsFaceup()
    {
        // pack:mc08:the-invocation-deck: "During the game ... top card ... faceup".
        // ruling:70055fca52b1ae18 corroborates that this begins after mulligan.
        var (world, cards) = Board(2);
        var invocation = InvocationDeck.Initialize(world, 0, cards, "setup", []);
        var game = Game.Begin(world, world.Facts, new NoCardAbilities());
        var top = invocation.Deck.Cards[^1];
        var before = world.Digest().Canonical();
        Assert.Throws<ArgumentException>(() => invocation.DiscardTop(top, "early", []));
        Assert.Equal(before, world.Digest().Canonical());

        CompleteMulligan(game);
        Assert.Equal(GamePhase.Mulligan, game.Phase);
        Assert.Null(invocation.PublicTop);
        Assert.All(cards, card => Assert.False(card.FaceUp));
        var result = CompleteMulligan(game);

        Assert.Same(top, invocation.PublicTop);
        Assert.Equal([top], cards.Where(card => card.FaceUp));
        var flip = Assert.Single(result.Events.OfType<CardsFlipped>());
        Assert.Equal([top.ObjectId], flip.Cards);
        Assert.True(flip.FaceUp);
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
    }

    [Fact]
    public void DiscardUpdatesTopAndImmediatelyRecyclesTheLastCardWithoutPlayerDeckPenalty()
    {
        // pack:mc08:the-invocation-deck: "special discard pile"; "If ... ever empty,
        // shuffle ... back ... There is no penalty for doing this."
        var (world, cards) = Board();
        var invocation = InvocationDeck.Initialize(world, 0, cards, "setup", []);
        var game = Game.Begin(world, world.Facts, new NoCardAbilities());
        CompleteMulligan(game);
        var ordinary = world.CreateCard("ordinary", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        // An empty ordinary deck plus a populated ordinary discard is a control:
        // an accidental PlayerDeck.Reset would move it and deal an encounter card.
        foreach (var card in world.Seats[0].Deck.Cards.ToArray()) World.MoveToTop(card, world.Seats[0].Hand);
        var order = invocation.Deck.Cards.Reverse().ToArray();
        var events = new List<GameEvent>();
        for (int i = 0; i < 4; i++)
        {
            var top = invocation.PublicTop!;
            Discard.Card(world, top, "invocation", events);
            Assert.Same(invocation.Discard, top.Area);
            Assert.True(top.FaceUp);
            Assert.Equal([invocation.PublicTop], invocation.Deck.Cards.Where(card => card.FaceUp));
        }
        var expected = order.ToList();
        var random = new EngineRandom(0);
        random.Generator.SetState(world.Random.Generator.GetState());
        random.Shuffle(expected);
        invocation.DiscardTop(invocation.PublicTop!, "invocation", events);

        Assert.Equal(expected, invocation.Deck.Cards);
        Assert.Empty(invocation.Discard.Cards);
        Assert.Equal([invocation.PublicTop], invocation.Deck.Cards.Where(card => card.FaceUp));
        Assert.Equal(random.Generator.GetState().Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Generator.GetState().Index, world.Random.Generator.GetState().Index);
        Assert.Equal([ordinary], ordinary.Area.Cards);
        Assert.Empty(world.Seats[0].Deck.Cards);
        Assert.DoesNotContain(world.Areas, area => area.Type == DeckType.DealtEncounterCardsDeck && area.Cards.Count > 0);
        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.Single(events.OfType<AreaReordered>());
        Assert.Equal(5, events.Count(happened => happened.Verb == "Reset"));
    }

    [Theory]
    [InlineData("identity")]
    [InlineData("foreign-identity")]
    [InlineData("owner")]
    [InlineData("foreign-world")]
    [InlineData("duplicate-physical")]
    [InlineData("duplicate-face")]
    [InlineData("missing")]
    [InlineData("player-deck")]
    public void InvalidSetupLeavesCardsAreasEventsAndRngUntouched(string invalid)
    {
        var (world, original) = Board(2);
        var cards = original.ToArray();
        switch (invalid)
        {
            case "identity": world.Seats[0].IdentityCard = world.CreateCard("01001b,01001a", world.Seats[0].Hero); break;
            case "foreign-identity": world.Seats[0].IdentityCard = Board().World.Seats[0].IdentityCard; break;
            case "owner": cards[0].TransferLinkedOwnership(1); break;
            case "foreign-world": cards[0] = Board().Cards[0]; break;
            case "duplicate-physical": cards[0] = cards[1]; break;
            case "duplicate-face": cards[0] = world.CreateCard("09033", original[0].Area); break;
            case "missing": cards = cards[..4]; break;
            case "player-deck": World.MoveToTop(cards[0], world.Seats[0].Deck); break;
        }
        var digest = world.Digest().Canonical();
        int areas = world.Areas.Count;
        var rng = world.Random.Generator.GetState();
        var events = new List<GameEvent>();

        Assert.Throws<ArgumentException>(() => InvocationDeck.Initialize(world, 0, cards, "setup", events));

        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(areas, world.Areas.Count);
        Assert.Null(world.Seats[0].InvocationDeck);
        Assert.Empty(events);
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(0, world.Random.Generator.WordsConsumed);
    }

    [Fact]
    public void ASecondSeatsInvocationDeckRetainsItsOwnerAndRejectsForeignSeatCards()
    {
        var (world, firstCards) = Board(2);
        var first = InvocationDeck.Initialize(world, 0, firstCards, "setup", []);
        var secondSeat = world.Seats[1];
        secondSeat.IdentityCard = world.CreateCard("09001b,09001a", secondSeat.Hero);
        var source = world.CreateArea(DeckType.AsideDeck, 1, PlayArea.Of(1));
        var cards = Enumerable.Range(32, 5).Select(number => world.CreateCard($"090{number}", source)).ToArray();
        var second = InvocationDeck.Initialize(world, 1, cards, "setup", []);
        var game = Game.Begin(world, world.Facts, new NoCardAbilities());
        CompleteMulligan(game);
        CompleteMulligan(game);
        var firstState = first.Deck.Cards.ToArray();
        var top = second.PublicTop!;
        Assert.Throws<ArgumentException>(() => first.DiscardTop(top, "wrong-seat", []));

        second.DiscardTop(top, "second-seat", []);

        Assert.Same(second.Discard, top.Area);
        Assert.Equal(1, top.Owner);
        Assert.Equal(PlayArea.Of(1), top.Area.PlayArea);
        Assert.Equal(firstState, first.Deck.Cards);
        Assert.Empty(first.Discard.Cards);
        Assert.Equal([second.PublicTop], second.Deck.Cards.Where(card => card.FaceUp));
    }

    [Fact]
    public void QueriesAndRejectedCopiesPreservePendingGameAndFutureSeededDecisions()
    {
        var (world, cards) = Board();
        var invocation = InvocationDeck.Initialize(world, 0, cards, "setup", []);
        var game = Game.Begin(world, world.Facts, new NoCardAbilities());
        CompleteMulligan(game);
        var (control, controlCards) = Board();
        var unprobed = InvocationDeck.Initialize(control, 0, controlCards, "setup", []);
        CompleteMulligan(Game.Begin(control, control.Facts, new NoCardAbilities()));
        var digest = world.Digest().Canonical();
        var rng = world.Random.Generator.GetState();
        long consumed = world.Random.Generator.WordsConsumed;
        var pending = game.Pending;
        var events = new List<GameEvent>();

        for (int i = 0; i < 3; i++) Assert.Same(invocation.Deck.Cards[^1], invocation.PublicTop);
        Assert.Throws<ArgumentException>(() => invocation.DiscardTop(unprobed.PublicTop!, "foreign", events));
        Assert.Throws<ArgumentException>(() => invocation.DiscardTop(invocation.Deck.Cards[0], "deep", events));
        Assert.Throws<InvalidOperationException>(() => InvocationDeck.Initialize(world, 0, cards, "again", events));

        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(consumed, world.Random.Generator.WordsConsumed);
        Assert.Same(pending, game.Pending);
        Assert.Empty(events);
        for (int i = 0; i < 10; i++)
        {
            invocation.DiscardTop(invocation.PublicTop!, "probe", events);
            unprobed.DiscardTop(unprobed.PublicTop!, "probe", []);
            Assert.Equal(control.Digest().Canonical(), world.Digest().Canonical());
        }
        Assert.Equal(control.Random.Generator.NextUInt32(), world.Random.Generator.NextUInt32());
    }

    private static Resolution CompleteMulligan(Game game) =>
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));

    private static (World World, Card[] Cards) Board(int players = 1)
    {
        var world = new World(new Printed(), players, 5489);
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat($"player {player}");
            seat.IdentityCard = world.CreateCard(player == 0 ? "09001b,09001a" : "other", seat.Hero);
            world.CreateCard("ordinary", seat.Deck);
        }
        var source = world.CreateArea(DeckType.AsideDeck, 0, PlayArea.Of(0));
        return (world, Enumerable.Range(32, 5).Select(number => world.CreateCard($"090{number}", source)).ToArray());
    }

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId is "09001b" or "other" ? CardKind.AlterEgo : CardKind.Event;
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
