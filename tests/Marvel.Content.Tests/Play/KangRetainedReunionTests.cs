using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual-card nonpublic board; Kang's public admission stays closed.
public sealed class KangRetainedReunionTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:first-player.3")]
    [Rule("rr:player-elimination.step.5")]
    public void AnEliminatedPlayersAreaRemainsUntilItsDepartureWhicheverOrderTheFirstPlayerChooses(
        bool expert, bool survivorFirst)
    {
        // 11008b: "Players cannot join this game area unless there are no other game areas
        // remaining." The eliminated player's Kang and completed stage stay in their area
        // until the stage's phase-end removal ("At the end of the phase, remove Kang (...)
        // and this stage from the game and combine your game area with another game area"),
        // so a co-due departure of the last players joins that area rather than 2B. Either
        // order the first player picks (rr:first-player.3) ends in one central reunion.
        var events = new List<GameEvent>();
        var world = KangStageFlowFixture.Split(2, expert, events);
        var eliminatedArea = world.GameAreaOf(PlayArea.Of(0))!;
        var retainedKang = KangStageFlowFixture.LocalKang(world, 0);
        Eliminate(world, 0, events);
        Assert.Same(eliminatedArea, Places.GameAreaOf(world, retainedKang));
        var survivorKang = KangStageFlowFixture.LocalKang(world, 1);
        KangStageFlowFixture.Defeat(world, survivorKang, events);

        var order = Assert.IsType<Prompt>(KangStageFlowFixture.EndPlayerPhase(world, 2, events));
        Assert.Equal(KangDepartureOrderOperation.Order, order.Trigger);
        var first = survivorFirst ? survivorKang : retainedKang;
        Sequence.Answer(world, Cards, world.Abilities, order, Decision.Take(first.ObjectId), events);
        var join = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(KangJoinOperation.Join, join.Trigger);
        var destination = Assert.Single(join.Affordances);
        Assert.Equal(survivorFirst ? eliminatedArea.Id : world.GameAreaOf(PlayArea.Of(1))!.Id, destination.Id);
        Sequence.Answer(world, Cards, world.Abilities, join, Decision.Take(destination.Id), events);
        var prompt = Sequence.Work(world, Cards, world.Abilities, events);
        for (int step = 0; prompt is not null; step++)
        {
            Assert.True(step < 40, prompt.Description);
            Assert.NotEqual(KangJoinOperation.Join, prompt.Trigger);
            Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(prompt.Affordances[0].Id), events);
            prompt = Sequence.Work(world, Cards, world.Abilities, events);
        }

        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.NotNull(world.GameAreaOf(PlayArea.Villains));
        Assert.Equal("11013b", Assert.Single(world.AreaOf(DeckType.MainSchemesArea).Cards).FaceId);
        Assert.Equal(expert ? "11039" : "11006", Assert.Single(world.AreaOf(DeckType.VillainArea).Cards).FaceId);
        Assert.Equal(DeckType.RemovedArea, retainedKang.Area.Type);
        Assert.Equal(DeckType.RemovedArea, survivorKang.Area.Type);
        Assert.Empty(world.Agenda.KangTransitions.Pending);
    }

    private static Marvel.Content.CardCatalog Cards { get; } = Marvel.Content.CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    private static void Eliminate(World world, int player, List<GameEvent> events)
    {
        Agendas.Happening(world);
        Defeat.Character(world, Cards, world.Seats[player].IdentityCard, "actual identity defeat", events);
        Sequence.Finish(world, Cards, world.Abilities, events);
    }
}
