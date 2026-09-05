using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed partial class EliminationTests
{
    [Rule("rr:player-elimination.step.1")]
    [Rule("rr:player-elimination.6")]
    [Theory]
    [InlineData(0, 1, 2)]
    [InlineData(2, 0, 1)]
    public void LayoutAndLiveEliminationSkipPreviouslyEliminatedSeats(
        int player, int alreadyEliminated, int next)
    {
        // "The next clockwise player" ignores eliminated players (.6).
        var facts = Cards();
        var world = Board(facts, players: 3);
        Elimination.Eliminate(world, facts, alreadyEliminated, "test", []);
        world.FirstPlayer = player;
        var minion = world.CreateCard("minion", world.AreaOf(
            DeckType.EngagedEnemiesArea, PlayArea.Of(player)));
        int areas = world.Areas.Count;
        string before = world.Digest().Canonical();

        var layout = EliminationLayout.Calculate(new WorldEliminationLayout(world), player);

        Assert.Equal(next, layout.NextPlayer);
        Assert.Equal([minion.ObjectId], layout.RelocatedCards);
        Assert.Contains(world.Seats[player].IdentityCard.ObjectId, layout.Leaving);
        Assert.Equal(areas, world.Areas.Count);
        Assert.Equal(before, world.Digest().Canonical());

        Elimination.Eliminate(world, facts, player, "test", []);

        Assert.Equal(next, world.FirstPlayer);
        Assert.Equal(PlayArea.Of(next), minion.Area.PlayArea);
        Assert.Equal(DeckType.RemovedArea, world.Seats[player].IdentityCard.Area.Type);
    }

    [Rule("rr:player-elimination.step.2")]
    [Fact]
    public void LayoutAndLiveMovementRetainTheHostedTreeInAreaAndPileOrder()
    {
        // "Retaining any tokens, attached cards, boost cards, tucked cards,
        // and status cards on them." Parent-before-child, area/pile sibling
        // order is the engine's deterministic event-order choice.
        var facts = Cards().With("attachment", ("Permanent", "1"));
        var world = Board(facts, players: 2);
        var minion = world.CreateCard("minion", world.AreaOf(
            DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var attachments = world.AreaOf(
            DeckType.UpgradesArea, PlayArea.Of(0), minion.ObjectId, cardOwner: -1);
        var firstCreated = world.CreateCard("attachment", attachments);
        var secondCreated = world.CreateCard("attachment", attachments);
        World.MoveToTop(firstCreated, attachments);
        var nested = world.CreateCard("attachment", world.AreaOf(
            DeckType.UpgradesArea, PlayArea.Of(0), secondCreated.ObjectId, cardOwner: -1));
        nested.TurnFaceDown();
        var tough = Statuses.Give(world, minion, Statuses.Tough);
        minion.TakeDamage(2);

        var layout = EliminationLayout.Calculate(new WorldEliminationLayout(world), 0);

        Assert.Equal(
            [minion.ObjectId, secondCreated.ObjectId, nested.ObjectId,
                firstCreated.ObjectId, tough.ObjectId], layout.RelocatedCards);
        Assert.DoesNotContain(firstCreated.ObjectId, layout.Leaving);
        Assert.DoesNotContain(nested.ObjectId, layout.Leaving);
        var events = new List<GameEvent>();

        Elimination.Eliminate(world, facts, 0, "test", events);

        Assert.Equal(
            [minion.ObjectId, secondCreated.ObjectId, nested.ObjectId,
                firstCreated.ObjectId, tough.ObjectId],
            events.OfType<CardsMoved>().Where(moved => moved.Verb == "Engage")
                .SelectMany(moved => moved.Cards.Select(card => card.Card)));
        Assert.Equal(PlayArea.Of(1), nested.Area.PlayArea);
        Assert.Equal(secondCreated.ObjectId, nested.Area.Host);
        Assert.False(nested.FaceUp);
        Assert.Equal(2, minion.Damage);
        Assert.True(Statuses.Has(world, minion, Statuses.Tough));
    }

    [Rule("rr:player-elimination.step.3")]
    [Rule("rr:player-elimination.3")]
    [Rule("rr:player-elimination.step.5")]
    [Fact]
    public void LayoutMembershipDoesNotConfuseDepartureWithRemovalFromGame()
    {
        // "Place each other card in its owner's discard pile." A borrowed
        // card leaves this play area but survives step 5 in its owner's pile.
        var facts = Cards();
        var world = Board(facts, players: 2);
        var own = world.CreateCard("ally", world.AreaOf(
            DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var borrowed = world.CreateCard("ally", world.AreaOf(
            DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        CardPlay.TakeControl(world, facts, borrowed, 0);

        var layout = EliminationLayout.Calculate(new WorldEliminationLayout(world), 0);

        Assert.Contains(borrowed.ObjectId, layout.Leaving);
        Assert.Contains(own.ObjectId, layout.Leaving);
        Assert.Empty(layout.Relocations);

        Elimination.Eliminate(world, facts, 0, "test", []);

        Assert.Equal(DeckType.DiscardPile, borrowed.Area.Type);
        Assert.Equal(PlayArea.Of(1), borrowed.Area.PlayArea);
        Assert.Equal(DeckType.RemovedArea, own.Area.Type);
    }

    [Rule("rr:player-elimination.4")]
    [Rule("rr:player-elimination.step.5")]
    [Fact]
    public void TheLastPlayersMinionsHaveNoRelocationDestination()
    {
        // "If all players are eliminated, the game ends and the players lose."
        // Step 5 removes the last play area; no next player can retain a minion.
        var facts = Cards();
        var world = Board(facts, players: 1);
        var minion = world.CreateCard("minion", world.AreaOf(
            DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

        var layout = EliminationLayout.Calculate(new WorldEliminationLayout(world), 0);

        Assert.Null(layout.NextPlayer);
        Assert.Empty(layout.Relocations);
        Assert.Contains(minion.ObjectId, layout.Leaving);

        Elimination.Eliminate(world, facts, 0, "test", []);

        Assert.Equal(Outcome.PlayersLose, world.Result);
        Assert.False(DeckTypes.IsInPlay(minion.Area.Type));
        Assert.Null(world.GameAreaOf(PlayArea.Of(0)));
    }
}
