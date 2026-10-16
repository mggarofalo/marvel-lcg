using System.Text.Json;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.View.Tests;

public sealed class PromptExecutionIsolationTests
{
    [Fact]
    public void ExportedPromptCannotAuthorizeExecutionAndOriginalRemainsUsable()
    {
        // Engineering boundary exercised on a prepared Core response: a visible
        // description carries choices, never the scheduler's execution authority.
        var cards = PersistentFixture.Cards;
        var runner = new AbilityRunner(PersistentFixture.Abilities);
        var world = new World(cards, 1) { Abilities = runner };
        var seat = world.CreateSeat("Spider-Man");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var minion = world.CreateCard("01101", world.AreaOf(
            DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        minion.TakeDamage(2);
        var room = world.CreateCard("01063", world.AreaOf(
            DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var scheme = world.CreateCard("01109", world.AreaOf(DeckType.SideSchemesArea));
        scheme.PlaceTokens("k_threat", 1);
        var events = new List<GameEvent>();
        BasicPowerInitiation.BasicAttack(world, cards, 0, minion, events);
        Prompt original = Assert.IsType<Prompt>(Sequence.Work(world, cards, runner, events));
        ViewScope scope = new PermissiveVisibilityPolicy().Authorize(null, world.Players);
        Prompt exported = Assert.IsType<Prompt>(WorldProjection.For(world, original, [], scope).Prompt);
        int offered = Assert.Single(original.Affordances, option => option.AnchorId == room.ObjectId).Id;
        string before = Snapshot(world, events);

        Assert.True(DecisionAssessment.Assess(exported, Decision.Take(offered)).IsAccepted);
        Assert.Throws<ArgumentException>(() =>
            Sequence.Answer(world, cards, runner, exported, Decision.Take(offered), events));
        Assert.Equal(before, Snapshot(world, events));
        Assert.Same(runner, world.Abilities);

        Sequence.Answer(world, cards, runner, original, Decision.Take(offered), events);
        Prompt choice = Assert.IsType<Prompt>(Sequence.Work(world, cards, runner, events));
        int target = Assert.Single(choice.Affordances, option => option.Id == scheme.ObjectId).Id;
        Sequence.Answer(world, cards, runner, choice, Decision.Take(target), events);
        Assert.Null(Sequence.Work(world, cards, runner, events));
        Assert.Equal(DeckType.EncounterDiscardPile, scheme.Area.Type);
        Assert.False(room.Ready);
        Assert.False(world.Agenda.IsBusy);
    }

    private static string Snapshot(World world, List<GameEvent> events) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(),
        Random = world.Random.Generator.GetState(), world.Random.Generator.WordsConsumed,
        world.Agenda.Stage,
        Frames = world.Agenda.Outstanding.Select(step => new { step.What, step.Index, step.Subject }),
        Events = events.Select(EventJson.Write),
    });
}
