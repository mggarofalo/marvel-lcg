using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Prepared nonpublic content through the public Game decision loop. This does
// not exercise the production content catalog, public setup, saves or replay.
public sealed class KangPlayerCountGameTests : CoreCardTestBase
{
    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [Rule("rr:delayed-effect.1")]
    public void GameRetainsTheEndingPlayerPhaseWhileFirstPlayerChoosesTheJoin(int players)
    {
        // "Delayed effects resolve automatically and immediately ... before responses".
        var world = KangPlayerCountFixture.Board(players, false);
        var events = new List<GameEvent>();
        KangPlayerCountFixture.Defeat(world, Assert.Single(world.AreaOf(DeckType.VillainArea).Cards), events);
        KangPlayerCountFixture.Boundary(world, 1, events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        var game = Game.Begin(world, Cards, world.Abilities);
        for (int seat = 0; seat < players; seat++)
        {
            Assert.Equal(GamePhase.Mulligan, game.Phase);
            game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
                offer => offer.Verb == Game.ResolveMulligans).Id));
        }
        Assert.Equal(world.FirstPlayer, game.Active);
        var local = Assert.Single(world.AreaOf(DeckType.VillainArea, PlayArea.Of(world.FirstPlayer)).Cards);
        KangPlayerCountFixture.Defeat(world, local, events);
        var localThreat = world.Cards.Where(card => KangLocalCompletion.IsLocal(card))
            .Select(card => card.Tokens.GetValueOrDefault("k_threat")).ToArray();
        for (int decision = 0; decision < players * 3; decision++)
        {
            var prompt = Assert.IsType<Prompt>(game.Pending);
            if (prompt.Trigger == KangJoinOperation.Join) break;
            if (game.Phase == GamePhase.PlayerTurn) game.Resolve(Decision.Decline);
            else
            {
                var end = Assert.Single(prompt.Affordances, offer => offer.Verb == Game.EndPhaseVerb);
                game.Resolve(Decision.Take(end.Id));
            }
        }
        var join = Assert.IsType<Prompt>(game.Pending);
        Assert.Equal(KangJoinOperation.Join, join.Trigger);
        Assert.Equal(players - 1, join.Player);
        Assert.Equal(players - 1, join.Affordances.Count);
        Assert.False(game.IsRootPrompt);
        Assert.Equal(localThreat, world.Cards.Where(card => KangLocalCompletion.IsLocal(card))
            .Select(card => card.Tokens.GetValueOrDefault("k_threat")));
        Assert.Contains(world.Agenda.Outstanding, step => step.Operation is PhaseTransitionOperation
            { Kind: PhaseTransitionKind.EndPlayerPhase });
        Assert.Null(world.GameAreaOf(PlayArea.Villains));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void EliminatedLocalDepartureRefusesBeforeItsPhaseMutates(int players)
    {
        var world = KangPlayerCountFixture.Board(players, false);
        var events = new List<GameEvent>();
        KangPlayerCountFixture.Defeat(world, Assert.Single(world.AreaOf(DeckType.VillainArea).Cards), events);
        KangPlayerCountFixture.Boundary(world, 1, events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        var local = Assert.Single(world.AreaOf(DeckType.VillainArea, PlayArea.Of(world.FirstPlayer)).Cards);
        KangPlayerCountFixture.Defeat(world, local, events);
        world.Seats[0].Eliminated = true;
        KangPlayerCountFixture.Boundary(world, 2, events);
        string before = KangPlayerCountFixture.Snapshot(world, events);
        var error = Assert.Throws<RulesNotImplementedException>(() => Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Contains("eliminated players", error.Message);
        Assert.Equal(before, KangPlayerCountFixture.Snapshot(world, events));
        Assert.Single(world.Agenda.KangTransitions.Pending);
    }

    [Fact]
    public void DetachedParticipatingSeatCannotDisappearFromTheLastGroup()
    {
        // Synthetic malformed topology isolates central admission: a live seat
        // is detached before the final local defeat captures its smaller group.
        var world = KangPlayerCountFixture.Board(2, false);
        var events = new List<GameEvent>();
        KangPlayerCountFixture.Defeat(world, Assert.Single(world.AreaOf(DeckType.VillainArea).Cards), events);
        KangPlayerCountFixture.Boundary(world, 1, events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        var last = Assert.Single(world.AreaOf(DeckType.VillainArea, PlayArea.Of(1)).Cards);
        var other = Assert.Single(world.AreaOf(DeckType.VillainArea, PlayArea.Of(0)).Cards);
        World.MoveToTop(other, world.AreaOf(DeckType.RemovedArea));
        var otherScheme = Assert.Single(world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(0)).Cards);
        World.MoveToTop(otherScheme, world.AreaOf(DeckType.RemovedArea));
        world.Detach(PlayArea.Of(0), "synthetic missing participating seat", events);
        KangPlayerCountFixture.Defeat(world, last, events);
        KangPlayerCountFixture.Boundary(world, 2, events);
        string before = KangPlayerCountFixture.Snapshot(world, events);
        var error = Assert.Throws<RulesNotImplementedException>(() => Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Contains("complete participating last group", error.Message);
        Assert.Equal(before, KangPlayerCountFixture.Snapshot(world, events));
        Assert.Single(world.Agenda.KangTransitions.Pending);
    }

}
