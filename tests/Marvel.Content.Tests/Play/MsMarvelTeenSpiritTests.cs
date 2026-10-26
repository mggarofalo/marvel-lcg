using Marvel.Content.Tests.Cards;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MsMarvelTeenSpiritTests
{
    private static AbilityRunner Runner() => AuthoredCards.Runner();

    private static void Identity(World world, int player)
    {
        var seat = world.Seats[player];
        World.MoveToTop(seat.IdentityCard, world.AreaOf(DeckType.RemovedArea, PlayArea.Of(player)));
        seat.IdentityCard = world.CreateCard("05001a,05001b", seat.Hero);
        seat.IdentityCard.TurnTo("05001b");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:identity-specific-card.3")]
    public void DiscardsInTopOrderAndRecoversOnlyTheExactSignatureCard(int player)
    {
        // "An identity-specific card is designated by the identity icon".
        var runner = Runner();
        Card? match = null;
        Card? duplicate = null;
        Card? below = null;
        Card? first = null;
        Card? second = null;
        var (game, world) = Playing(board =>
        {
            Identity(board, player);
            var seat = board.Seats[player];
            duplicate = board.CreateCard("05003", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player));
            below = board.CreateCard("05004", seat.Deck);
            match = board.CreateCard("05003", seat.Deck);
            second = board.CreateCard("05024", seat.Deck);
            first = board.CreateCard("05012", seat.Deck);
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == world.Seats[player].IdentityCard.ObjectId && offer.Verb != "Change_Form");
        var resolution = game.Resolve(Decision.Take(action.Id));
        Assert.Equal([first!.ObjectId, second!.ObjectId, match!.ObjectId], resolution.Events
            .OfType<CardsMoved>().Where(move => move.From.Zone == "PlayerDeck" && move.To.Zone == "DiscardPile")
            .SelectMany(move => move.Cards).Select(card => card.Card));
        Assert.Same(world.Seats[player].Hand, match.Area);
        Assert.Equal(player, match.Owner);
        Assert.Equal(DeckType.DiscardPile, duplicate!.Area.Type);
        Assert.Same(below, world.Seats[player].Deck.Cards[^1]);
        Assert.DoesNotContain(runner.Actions(world, player), action => action.Card == world.Seats[player].IdentityCard.ObjectId);
        string before = world.Digest().Canonical();
        Assert.ThrowsAny<Exception>(() => game.Resolve(Decision.Take(action.Id)));
        Assert.Equal(before, world.Digest().Canonical());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:player-deck.1")]
    [Rule("rr:player-deck.3")]
    public void ExhaustionResetsExactlyOnceAndNeverDiscardsTheReplacementDeck(bool matchLast)
    {
        // "no further cards are discarded from the newly shuffled deck";
        // "immediately deals themself one facedown encounter card".
        var (game, world) = Prepared(matchLast);
        var (_, control) = Prepared(matchLast);
        var seat = world.Seats[1];
        var expected = seat.Deck.Cards.Reverse().ToArray();
        int dealt = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards.Count;
        var events = new List<GameEvent>();
        var controlDiscarded = PlayerDeck.DiscardTop(control, 1, long.MaxValue, "control", events);
        if (matchLast)
        {
            var exact = controlDiscarded[^1];
            World.MoveToTop(exact, control.Seats[1].Hand);
        }
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == seat.IdentityCard.ObjectId);
        var resolution = game.Resolve(Decision.Take(action.Id));
        Assert.Equal(expected.Select(card => card.ObjectId), resolution.Events.OfType<CardsMoved>()
            .Where(move => move.From.Zone == "PlayerDeck" && move.To.Zone == "DiscardPile")
            .SelectMany(move => move.Cards).Select(card => card.Card));
        Assert.Equal(dealt + 1, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards.Count);
        Assert.Single(resolution.Events.OfType<AreaReordered>(), item => item.Verb == "Shuffle");
        Assert.Equal(control.Seats[1].Deck.Cards.Select(card => card.ObjectId), seat.Deck.Cards.Select(card => card.ObjectId));
        Assert.Equal(control.Random.Generator.GetState().Words, world.Random.Generator.GetState().Words);
        Assert.Equal(control.Random.Generator.GetState().Index, world.Random.Generator.GetState().Index);
        Assert.Equal(matchLast ? DeckType.HandsArea : DeckType.PlayerDeck, expected[^1].Area.Type);
        Assert.Equal(DeckType.PlayerDeck, expected[0].Area.Type);
    }

    [Fact]
    public void QueriesPreserveHiddenOrderRandomnessAgendaAndFollowingOutcome()
    {
        var (game, world) = Prepared(false);
        var (controlGame, control) = Prepared(false);
        var runner = Runner();
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        bool busy = world.Agenda.IsBusy;
        for (int query = 0; query < 3; query++) Assert.Single(runner.Actions(world, 1), action => action.Card == world.Seats[1].IdentityCard.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(busy, world.Agenda.IsBusy);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == world.Seats[1].IdentityCard.ObjectId).Id));
        controlGame.Resolve(Decision.Take(Assert.Single(controlGame.Pending!.Affordances,
            offer => offer.AnchorId == control.Seats[1].IdentityCard.ObjectId).Id));
        Assert.Equal(control.Digest().Canonical(), world.Digest().Canonical());
        Assert.Equal(control.Random.Generator.GetState().Words, world.Random.Generator.GetState().Words);
    }

    [Fact]
    public void HeroFormAndEmptyDeckCannotInitiateButHiddenMatchesAreNotRequired()
    {
        var runner = Runner();
        var (_, world) = Prepared(false);
        Assert.Single(runner.Actions(world, 1), action => action.Card == world.Seats[1].IdentityCard.ObjectId);
        world.Seats[1].IdentityCard.TurnTo("05001a");
        Assert.DoesNotContain(runner.Actions(world, 1), action => action.Card == world.Seats[1].IdentityCard.ObjectId);
        world.Seats[1].IdentityCard.TurnTo("05001b");
        foreach (var card in world.Seats[1].Deck.Cards.ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea, PlayArea.Of(1)));
        Assert.DoesNotContain(runner.Actions(world, 1), action => action.Card == world.Seats[1].IdentityCard.ObjectId);
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == world.Seats[1].IdentityCard.ObjectId);
    }

    [Fact]
    [Rule("rr:limit")]
    public void ExhaustedAlterEgoSearchesOnceAndOnlyRoundEndRestoresItsUse()
    {
        // rr:limit: "A limit of 'X per [period]' is a limit that appears on
        // cards that remain in play". Teen Spirit prints no exhaust cost.
        var runner = Runner();
        var (_, world) = Prepared(false);
        var identity = world.Seats[1].IdentityCard;
        identity.Exhaust();
        var action = Assert.Single(runner.Actions(world, 1), item => item.Card == identity.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.False(identity.Ready);
        identity.TurnTo("05001a");
        identity.TurnTo("05001b");
        Assert.DoesNotContain(runner.Actions(world, 1), item => item.Card == identity.ObjectId);
        PhaseEnd.EndPlayerPhase(world, []);
        Assert.DoesNotContain(runner.Actions(world, 1), item => item.Card == identity.ObjectId);
        PhaseEnd.EndVillainPhase(world, world.Facts, []);
        Assert.Contains(runner.Actions(world, 1), item => item.Card == identity.ObjectId);
    }

    private static (Game Game, World World) Prepared(bool matchLast) => Playing(board =>
    {
        Identity(board, 1);
        var seat = board.Seats[1];
        foreach (var card in seat.Deck.Cards.ToArray())
            World.MoveToTop(card, board.AreaOf(DeckType.RemovedArea, PlayArea.Of(1)));
        board.CreateCard("05003", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
        board.CreateCard(matchLast ? "05004" : "05024", seat.Deck);
        board.CreateCard("05012", seat.Deck);
    }, heroes: ["spider_man", "captain_marvel"], abilities: Runner());
}
