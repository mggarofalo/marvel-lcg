using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual-card nonpublic overlay; complete Kang production admission remains closed.
public sealed class KangLocalEliminationTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:player-elimination.step.5")]
    public void LastLocalPlayerCompletesStoresAndDepartsBeforeSurvivorReunion(bool expert)
    {
        // 11009b-11012b: "If all the players at this stage are defeated, this stage is complete."
        // "At the end of the phase, remove ... this stage ... and combine your game area".
        // RR: "When a player is eliminated, the remaining players continue to play the game."
        var (world, events) = Split(2, expert);
        var scheme = Local(world, 0, DeckType.MainSchemesArea);
        var villain = Local(world, 0, DeckType.VillainArea);
        var oldArea = Places.GameAreaOf(world, scheme);
        var survivor = world.GameAreaOf(PlayArea.Of(1));
        var side = world.CreateCard("01098", world.AreaOf(DeckType.SideSchemesArea, PlayArea.Of(0)));
        var attachment = world.CreateCard("01109", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), villain.ObjectId));

        Eliminate(world, 0, events);

        Assert.Null(world.GameAreaOf(PlayArea.Of(0)));
        Assert.Same(oldArea, Places.GameAreaOf(world, scheme));
        Assert.Same(oldArea, Places.GameAreaOf(world, attachment));
        Assert.False(Places.CanAffect(world, side, world.Seats[1].IdentityCard));
        Assert.Equal(1, scheme.Tokens.GetValueOrDefault("is_completed"));
        Assert.Equal(DeckType.VillainArea, villain.Area.Type);
        var host = Assert.Single(world.Cards, card => card.FaceId == "11013a");
        Assert.False(Assert.Single(BeneathCards.Of(world, host)).FaceUp);
        Assert.Single(world.Agenda.KangTransitions.Pending);
        Assert.Equal(Outcome.Unfinished, world.Result);

        KangPlayerCountFixture.Boundary(world, 2, events);
        var join = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(1, join.Player);
        Sequence.Answer(world, Cards, world.Abilities, join, Decision.Take(Assert.Single(join.Affordances).Id), events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.Equal(DeckType.RemovedArea, scheme.Area.Type);
        Assert.Equal(DeckType.RemovedArea, villain.Area.Type);
        Assert.Same(survivor, Places.GameAreaOf(world, side));
        Assert.Equal(PlayArea.Of(1), side.Area.PlayArea);
        Assert.Equal(DeckType.EncounterDiscardPile, attachment.Area.Type);
        Assert.Empty(world.Agenda.KangTransitions.Pending);

        KangPlayerCountFixture.Defeat(world, Local(world, 1, DeckType.VillainArea), events);
        KangPlayerCountFixture.Boundary(world, 3, events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.Equal("11013b", host.FaceId);
        Assert.Same(survivor, world.GameAreaOf(PlayArea.Villains));
        Assert.Single(world.Cards, card => card.FaceId == "11023" && card.Area.Type == DeckType.SideSchemesArea);
        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:player-elimination.6")]
    public void SurvivingLocalPlayerDoesNotCompleteSchemeAndCanFinishTheLastGroup()
    {
        // "Effects that refer to the players in the game ignore eliminated players".
        // 3B completes only "If all the players at this stage are defeated".
        var (world, events) = Split(2, false);
        KangPlayerCountFixture.Defeat(world, Local(world, 0, DeckType.VillainArea), events);
        KangPlayerCountFixture.Boundary(world, 2, events);
        var join = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Sequence.Answer(world, Cards, world.Abilities, join, Decision.Take(Assert.Single(join.Affordances).Id), events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        var scheme = Local(world, 1, DeckType.MainSchemesArea);
        var local = Local(world, 1, DeckType.VillainArea);

        Eliminate(world, 1, events);

        Assert.Equal(0, scheme.Tokens.GetValueOrDefault("is_completed"));
        Assert.Empty(world.Agenda.KangTransitions.Pending);
        Assert.Empty(BeneathCards.Of(world, Assert.Single(world.Cards, card => card.FaceId == "11013a")));
        KangPlayerCountFixture.Defeat(world, local, events);
        KangPlayerCountFixture.Boundary(world, 3, events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.Contains(world.Cards, card => card.FaceId == "11013b" && card.Area.Type == DeckType.MainSchemesArea);
        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:player-elimination.4")]
    public void AllPlayersEliminatedEndsWithoutCompletingTheFinalLocalScheme()
    {
        // "If all players are eliminated, the game ends and the players lose."
        var (world, events) = Split(2, false);
        Eliminate(world, 0, events);
        int stored = world.Cards.Count(card => card.Area.Type == DeckType.BeneathCardsPile);
        Eliminate(world, 1, events);
        Assert.Equal(Outcome.PlayersLose, world.Result);
        Assert.Equal(stored, world.Cards.Count(card => card.Area.Type == DeckType.BeneathCardsPile));
        Assert.Empty(world.Agenda.Outstanding);
        Assert.DoesNotContain(world.Cards, card => card.FaceId == "11013b");
    }

    private static (World World, List<GameEvent> Events) Split(int players, bool expert)
    {
        var world = KangPlayerCountFixture.Board(players, expert, completion: true);
        var events = new List<GameEvent>();
        KangPlayerCountFixture.Defeat(world, Assert.Single(world.AreaOf(DeckType.VillainArea).Cards), events);
        KangPlayerCountFixture.Boundary(world, 1, events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        return (world, events);
    }

    [Fact]
    public void ReenteredSideSchemeCannotRegainItsRetainedLocalAssociation()
    {
        // Synthetic leave/reentry isolates the incarnation boundary of retained placement.
        var (world, events) = Split(2, false);
        var side = world.CreateCard("01098", world.AreaOf(DeckType.SideSchemesArea, PlayArea.Of(0)));
        var original = side.Area;
        Eliminate(world, 0, events);
        Assert.NotNull(Places.GameAreaOf(world, side));
        Discard.Card(world, side, "synthetic departure", events);
        World.MoveToTop(side, original);
        Assert.Null(Places.GameAreaOf(world, side));
    }

    [Fact]
    public void EliminationDuringSuspendedJoinRaisesBeforeChangingPlayerOrCards()
    {
        // Engine boundary: a retained departure choice cannot be rewritten by elimination.
        var (world, events) = Split(2, false);
        KangPlayerCountFixture.Defeat(world, Local(world, 0, DeckType.VillainArea), events);
        KangPlayerCountFixture.Boundary(world, 2, events);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        string before = KangPlayerCountFixture.Snapshot(world, events);
        var error = Assert.Throws<RulesNotImplementedException>(() =>
            Elimination.Eliminate(world, Cards, 1, "synthetic suspended elimination", events));
        Assert.Contains("suspended departure choice", error.Message);
        Assert.Equal(before, KangPlayerCountFixture.Snapshot(world, events));
        Assert.False(world.Seats[1].Eliminated);
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(Assert.Single(prompt.Affordances).Id), events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void AlreadyCapturedCleanDepartureShrinksAfterRealPlayerElimination()
    {
        // 11002-11005: "At the end of the phase, combine your game area with another game area."
        // Eliminated players no longer participate; a clean II defeat adds no Dominion.
        var (world, events) = Split(2, false);
        KangPlayerCountFixture.Defeat(world, Local(world, 0, DeckType.VillainArea), events);
        Eliminate(world, 0, events);
        Assert.Empty(Assert.Single(world.Agenda.KangTransitions.Pending).Group);
        KangPlayerCountFixture.Boundary(world, 2, events);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(Assert.Single(prompt.Affordances).Id), events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.Empty(world.Agenda.KangTransitions.Pending);
        Assert.DoesNotContain(world.Cards, card => card.Area.Type == DeckType.BeneathCardsPile);
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    private static Card Local(World world, int player, DeckType type) =>
        Assert.Single(world.AreaOf(type, PlayArea.Of(player)).Cards);

    private static void Eliminate(World world, int player, List<GameEvent> events)
    {
        Agendas.Happening(world);
        Defeat.Character(world, Cards, world.Seats[player].IdentityCard, "actual identity defeat", events);
        Sequence.Finish(world, Cards, world.Abilities, events);
    }
}
