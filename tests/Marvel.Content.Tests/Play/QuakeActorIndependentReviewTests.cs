using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class QuakeActorIndependentReviewTests
{
    [Fact]
    [Rule("rr:leaves-play.1")]
    public void MovingActorWithinPlayKeepsRetainedQuakeAnswerValid()
    {
        // Only leaving play makes a card "a new copy of the card".
        // This defensive relocation is a positive control for the stale-state probe.
        var (world, runner, quake, minion, prompt) = Response();
        int incarnation = minion.Incarnation;
        World.MoveToTop(minion, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.Equal(incarnation, minion.Incarnation);
        var occurrence = world.Windows.Current!.Value.Occurrence;
        Assert.Equal(incarnation, occurrence.ActorFacts!.Incarnation);
        var offer = Assert.Single(prompt.Affordances);
        Assert.Equal(quake.ObjectId, offer.AnchorId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(offer.Id), []);
        new RedSkullFixture(world, runner).Finish();
        Assert.Equal(2, minion.Damage);
        Assert.False(quake.Ready);
        Assert.Equal(0, minion.Area.PlayArea.Player);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:leaves-play.1")]
    public void CapturedAndLegacyActorBindingsHaveDistinctReentrySemantics()
    {
        // A departed card is "a new copy of the card". Missing incarnation is the
        // engine's deliberate legacy-construction fallback, not a replayed live window.
        var (world, runner, quake, minion, _) = Response();
        var captured = world.Windows.Current!.Value.Occurrence;
        var legacy = new Occurrence(captured.Id, captured.Conditions,
            Player: captured.Player, Actor: captured.Actor,
            ActorFacts: captured.ActorFacts! with { Incarnation = null });
        Assert.Equal(quake.ObjectId, Assert.Single(runner.Waiting(world, captured, WindowKind.Response)).Card);
        Assert.Equal(quake.ObjectId, Assert.Single(runner.Waiting(world, legacy, WindowKind.Response)).Card);
        Discard.Card(world, minion, "independent defensive reentry probe", []);
        World.MoveToTop(minion, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        Assert.NotEqual(captured.ActorFacts!.Incarnation, minion.Incarnation);
        Assert.Empty(runner.Waiting(world, captured, WindowKind.Response));
        Assert.Equal(quake.ObjectId, Assert.Single(runner.Waiting(world, legacy, WindowKind.Response)).Card);
        Assert.True(captured.ActorFacts.IsMinion);
        Assert.Equal(World.Scenario, captured.ActorFacts.Controller);
        Assert.True(quake.Ready);
        Assert.Equal(0, minion.Damage);
    }

    [Fact]
    public void RuntimeIncarnationDoesNotChangeSerializedParticipantOrOccurrence()
    {
        // JSON spelling is an engine choice. Live windows are rebuilt by accepted
        // decisions; deserialized historical facts intentionally lack the runtime copy.
        var (world, _, _, minion, _) = Response();
        var captured = world.Windows.Current!.Value.Occurrence;
        var facts = captured.ActorFacts!;
        Assert.Equal(minion.Incarnation, facts.Incarnation);
        string actual = JsonSerializer.Serialize(facts);
        Assert.Equal(JsonSerializer.Serialize(facts with { Incarnation = null }), actual);
        Assert.Equal(JsonSerializer.Serialize(facts with { Incarnation = minion.Incarnation + 1 }), actual);
        var restored = Assert.IsType<OccurrenceCard>(JsonSerializer.Deserialize<OccurrenceCard>(actual));
        Assert.Null(restored.Incarnation);
        Assert.Equal(facts.Card, restored.Card);
        Assert.Equal(facts.Kind, restored.Kind);
        Assert.Equal(facts.Owner, restored.Owner);
        Assert.Equal(facts.Controller, restored.Controller);
        var historical = new Occurrence(captured.Id, captured.Conditions,
            captured.Subject, captured.Player, captured.Actor, captured.Target,
            restored, captured.TargetFacts, captured.Threat, captured.Resolutions);
        Assert.Equal(JsonSerializer.Serialize(historical), JsonSerializer.Serialize(captured));
    }

    private static (World World, AbilityRunner Runner, Card Quake, Card Minion, Prompt Prompt) Response()
    {
        // Test-only Quake interpretation of printed 08012; no ability-book admission.
        var world = WidowEncounterFixture.Board(false);
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"08012","abilities":[{
              "trigger":{"event":"WhenSchemeEnds","timing":"Response","actor":"minion"},
              "cost":{"exhaust":"this"},
              "effect":{"dealDamage":{"cards":"trigger.actor","amount":2}}
            }]}]}
            """));
        world.Abilities = runner;
        var quake = world.CreateCard("08012", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var minion = world.CreateCard("08026", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        world.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 2, Subject: minion.ObjectId, Seat: 1));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(1, prompt.Player);
        Assert.True(prompt.Cancellable);
        return (world, runner, quake, minion, prompt);
    }
}
