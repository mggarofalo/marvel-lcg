using System.Text.Json;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SimultaneousPowerReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:damage.step.3")]
    public void ActualMagicBlastDamagesTheSamePhysicalGoblinAfterNormanFlips(bool discardFirst)
    {
        // Norman's printed replacement removes infamy instead of taking damage.
        // Ruling e4ea1bf855e41aac confirms the later separate 2 damages Goblin;
        // current damage step 3 still precedes placement and preserves this answer.
        var f = Prepared(goblin: true);
        var id = f.Enemy.ObjectId;
        var environment = f.World.TheCardIn(DeckType.EnvironmentArea)!;
        var events = Finish(f, discardFirst, []);
        Assert.Same(f.Enemy, f.World.TheCardIn(DeckType.VillainArea));
        Assert.Equal(id, f.Enemy.ObjectId);
        Assert.Equal("02001b", f.Enemy.FaceId);
        Assert.Equal("02006b", environment.FaceId);
        Assert.Equal(2, f.Enemy.Damage);
        Assert.Equal(3, f.World.Seats[0].IdentityCard.Damage);
        Assert.Single(events.OfType<CardsFlipped>(), e => e.Cards.Contains(id));
        Assert.Single(events.OfType<AttackBoundary>(), e => e.Started);
        Assert.Single(events.OfType<AttackBoundary>(), e => !e.Started);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:villain-defeat.2")]
    [Rule("rr:villain-defeat.3.1")]
    public void ActualMagicBlastFinishesSeparateDamageAndStatusesOnTheSameTitleNextStage(bool discardFirst)
    {
        // "Excess damage ... does not carry over"; the same-title next stage is
        // "the same character ... for purposes of card abilities". The separate
        // conditional 2 and statuses therefore apply to Rhino II after reveal.
        var f = Prepared();
        f.Enemy.TakeDamage(10);
        var next = f.World.CreateCard("01095", f.World.AreaOf(DeckType.VillainDeck));
        var scheme = f.World.CreateCard("01107", f.World.AreaOf(DeckType.EncounterDeck));
        var events = Finish(f, discardFirst, []);
        Assert.Equal(DeckType.RemovedArea, f.Enemy.Area.Type);
        Assert.Same(next, f.World.TheCardIn(DeckType.VillainArea));
        Assert.Equal(2, next.Damage);
        Assert.True(Statuses.Has(f.World, next, Statuses.Stunned));
        Assert.True(Statuses.Has(f.World, next, Statuses.Confused));
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        Assert.Equal(3, scheme.Tokens["k_threat"]);
        int revealed = events.FindIndex(e => e is FieldSet field
            && field.Card == scheme.ObjectId && field.Field == "k_threat" && field.To == 3);
        int laterDamage = events.FindIndex(e => e is FieldSet field
            && field.Card == next.ObjectId && field.Field == "health" && field.AttackIdentity is not null);
        Assert.True(revealed >= 0 && laterDamage > revealed);
        var attack = Assert.Single(events.OfType<AttackBoundary>(), e => e.Started);
        Assert.Single(events.OfType<AttackBoundary>(), e => !e.Started);
        Assert.All(events.OfType<FieldSet>().Where(e => e.AttackIdentity is not null),
            e => Assert.Equal(attack.Identity, e.AttackIdentity));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SameActualStageFixtureOffersAndCompletesWhenTheChosenEnemySurvives(bool discardFirst)
    {
        var f = Prepared();
        var next = f.World.CreateCard("01095", f.World.AreaOf(DeckType.VillainDeck));
        Finish(f, discardFirst, []);
        Assert.Same(f.Enemy, f.World.TheCardIn(DeckType.VillainArea));
        Assert.Equal(7, f.Enemy.Damage);
        Assert.True(Statuses.Has(f.World, f.Enemy, Statuses.Stunned));
        Assert.True(Statuses.Has(f.World, f.Enemy, Statuses.Confused));
        Assert.Equal(DeckType.VillainDeck, next.Area.Type);
        Assert.Equal(0, next.Damage);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void SerializedDecisionTraceReconstructsThePendingPowerThenContinues(bool discardFirst, bool promoted)
    {
        // Saving a trace rather than live execution handles is the repository's
        // persistence choice. Reconstruct after the first execution completes.
        var original = Prepared(goblin: !promoted, promoted: promoted);
        var trace = new List<Decision>();
        var originalEvents = Finish(original, discardFirst, trace);
        string saved = JsonSerializer.Serialize(trace);
        var replay = Prepared(goblin: !promoted, promoted: promoted);
        var decisions = JsonSerializer.Deserialize<List<Decision>>(saved)!;
        var replayEvents = Start(replay);
        int orders = 0;
        foreach (var decision in decisions)
        {
            var prompt = Assert.IsType<Prompt>(Sequence.Work(
                replay.World, replay.World.Facts, replay.Runner, replayEvents));
            if (prompt.Asking == Question.Order)
            {
                orders++;
                Assert.True(replay.World.Agenda.IsBusy);
                Assert.Equal(promoted ? 10 : 0, replay.Enemy.Damage);
                Assert.Equal(promoted ? "01094" : "02001a", replay.Enemy.FaceId);
            }
            Sequence.Answer(replay.World, replay.World.Facts, replay.Runner,
                prompt, decision, replayEvents);
        }
        Assert.Null(Sequence.Work(replay.World, replay.World.Facts, replay.Runner, replayEvents));
        Assert.Equal(1, orders);
        Assert.Equal(original.World.Digest().Canonical(), replay.World.Digest().Canonical());
        Assert.Equal(original.World.Random.Generator.WordsConsumed,
            replay.World.Random.Generator.WordsConsumed);
        Assert.Equal(originalEvents.Select(EventJson), replayEvents.Select(EventJson));
        Assert.False(replay.World.Agenda.IsBusy);
        Assert.Equal(2, replay.World.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    private static string EventJson(GameEvent value) => JsonSerializer.Serialize(value, value.GetType());

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:leaves-play.1")]
    public void SyntheticDepartureAndReentryWhileOrderIsPendingDoesNotRetargetThePower(bool discardFirst)
    {
        // A returned physical card is "a new copy of the card". This adversarial
        // fixture changes the board at a suspension; it is not a played card row.
        var f = Prepared();
        var minion = f.World.CreateCard("01101",
            f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        f = f with { Enemy = minion };
        var events = Start(f);
        var target = Assert.IsType<Prompt>(Sequence.Work(f.World, f.World.Facts, f.Runner, events));
        Sequence.Answer(f.World, f.World.Facts, f.Runner, target,
            Decision.Take(target.Affordances.Single(a => a.AnchorId == minion.ObjectId).Id), events);
        var order = Assert.IsType<Prompt>(Sequence.Work(f.World, f.World.Facts, f.Runner, events));
        Assert.Equal(Question.Order, order.Asking);
        int incarnation = minion.Incarnation;
        Discard.Card(f.World, minion, "SyntheticDeparture", events);
        World.MoveToTop(minion, f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.NotEqual(incarnation, minion.Incarnation);
        Sequence.Answer(f.World, f.World.Facts, f.Runner, order,
            Decision.Take(Assert.Single(order.Affordances).Id, discardFirst ? [1, 0] : [0, 1], []), events);
        Sequence.Finish(f.World, f.World.Facts, f.Runner, events);
        Assert.Equal(0, minion.Damage);
        Assert.False(Statuses.Has(f.World, minion, Statuses.Stunned));
        Assert.False(Statuses.Has(f.World, minion, Statuses.Confused));
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.Equal(DeckType.DiscardPile, f.Source.Area.Type);
        Assert.False(f.World.Agenda.IsBusy);
    }

    private static List<GameEvent> Finish(Fixture f, bool discardFirst, List<Decision> trace)
    {
        var events = Start(f);
        bool selected = false;
        int orderCount = 0;
        for (int count = 0; count < 40; count++)
        {
            var prompt = Sequence.Work(f.World, f.World.Facts, f.Runner, events);
            if (prompt is null) break;
            Decision answer;
            if (prompt.Asking == Question.Order)
            {
                orderCount++;
                answer = Decision.Take(Assert.Single(prompt.Affordances).Id,
                    discardFirst ? [1, 0] : [0, 1], []);
            }
            else if (!selected && prompt.Affordances.Any(a => a.AnchorId == f.Enemy.ObjectId))
            {
                selected = true;
                answer = Decision.Take(prompt.Affordances.Single(a => a.AnchorId == f.Enemy.ObjectId).Id);
            }
            else if (prompt.Affordances.Count == 1 && prompt.Affordances[0].Targets is { Min: 3 })
                answer = Decision.Take(prompt.Affordances[0].Id,
                    Enumerable.Repeat(f.World.Seats[0].IdentityCard.ObjectId, 3).ToArray(), []);
            else answer = Decision.Decline;
            trace.Add(answer);
            Sequence.Answer(f.World, f.World.Facts, f.Runner, prompt, answer, events);
        }
        Assert.True(selected);
        Assert.Equal(1, orderCount);
        Assert.False(f.World.Agenda.IsBusy);
        Assert.Equal(DeckType.DiscardPile, f.Source.Area.Type);
        return events;
    }

    private static List<GameEvent> Start(Fixture f)
    {
        var action = Assert.Single(f.Runner.Actions(f.World, 0), a => a.Card == f.Source.ObjectId);
        return f.Runner.ExecuteAction(f.World, new PlayerAction(action, f.Payment, [])).Events.ToList();
    }

    private static Fixture Prepared(bool goblin = false, bool promoted = false)
    {
        var world = ThorActualFixture.Board("06001a");
        var runner = DoctorStrangeMagicBlastFixture.Runner();
        world.Abilities = runner;
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        if (goblin)
        {
            World.MoveToTop(enemy, world.AreaOf(DeckType.RemovedArea));
            enemy = world.CreateCard("02001a,02001b", world.AreaOf(DeckType.VillainArea));
            world.CreateCard("02006a,02006b", world.AreaOf(DeckType.EnvironmentArea))
                .PlaceTokens("c_infamy", 5);
        }
        if (promoted)
        {
            enemy.TakeDamage(10);
            world.CreateCard("01095", world.AreaOf(DeckType.VillainDeck));
            world.CreateCard("01107", world.AreaOf(DeckType.EncounterDeck));
        }
        world.CreateCard("01055", world.Seats[0].Deck);
        var source = world.CreateCard("09004", world.Seats[0].Hand);
        var first = world.CreateCard("01088", world.Seats[0].Hand);
        var second = world.CreateCard("01089", world.Seats[0].Hand);
        return new(world, runner, enemy, source, [first.ObjectId, second.ObjectId]);
    }

    private sealed record Fixture(World World, AbilityRunner Runner, Card Enemy, Card Source, int[] Payment);
}

