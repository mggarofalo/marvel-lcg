using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class QuakeActorIncarnationTests
{
    [Fact]
    [Rule("rr:leaves-play.1")]
    public void EarlierAcceptedResponseReentersActorBeforeQuakeOpportunity()
    {
        // Synthetic same-window response exercises normal accepted engine ordering.
        // A departed card "is considered to be a new copy of the card".
        var world = WidowEncounterFixture.Board(false);
        var runner = Runner();
        world.Abilities = runner;
        var quake = world.CreateCard("08012", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var source = world.CreateCard("01007", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var minion = world.CreateCard("08026", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        int incarnation = minion.Incarnation;
        world.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 2, Subject: minion.ObjectId, Seat: 1));
        var response = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(1, response.Player);
        Assert.Contains(response.Affordances, a => a.AnchorId == quake.ObjectId);
        string initial = world.Digest().Canonical();
        string initialRng = System.Text.Json.JsonSerializer.Serialize(world.Random.Generator.GetState());
        var initialPending = world.Agenda.Current;
        var initialWindow = world.Windows.Current;
        Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, world.Facts, runner,
            response, Decision.Take(int.MaxValue), []));
        Assert.Equal(initial, world.Digest().Canonical());
        Assert.Equal(initialRng, System.Text.Json.JsonSerializer.Serialize(world.Random.Generator.GetState()));
        Assert.Equal(initialPending, world.Agenda.Current);
        Assert.Equal(initialWindow, world.Windows.Current);
        Assert.True(quake.Ready);
        var replace = Assert.Single(response.Affordances, a => a.AnchorId == source.ObjectId);
        var occurrence = world.Windows.Current!.Value.Occurrence;
        Assert.Equal(incarnation, occurrence.ActorFacts!.Incarnation);
        Assert.DoesNotContain("Incarnation", System.Text.Json.JsonSerializer.Serialize(occurrence.ActorFacts));
        Sequence.Answer(world, world.Facts, runner, response, Decision.Take(replace.Id), []);
        string before = world.Digest().Canonical();
        string rng = System.Text.Json.JsonSerializer.Serialize(world.Random.Generator.GetState());
        var pending = world.Agenda.Current;
        var stage = world.Agenda.Stage;
        for (int query = 0; query < 3; query++)
            _ = runner.Waiting(world, occurrence, WindowKind.Response);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(rng, System.Text.Json.JsonSerializer.Serialize(world.Random.Generator.GetState()));
        Assert.Equal(pending, world.Agenda.Current);
        Assert.Equal(stage, world.Agenda.Stage);
        var next = Sequence.Work(world, world.Facts, runner, []);
        Assert.True(minion.Incarnation > incarnation);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.True(quake.Ready);
        Assert.True(next is null || next.Affordances.All(a => a.AnchorId != quake.ObjectId));
        before = world.Digest().Canonical();
        rng = System.Text.Json.JsonSerializer.Serialize(world.Random.Generator.GetState());
        pending = world.Agenda.Current;
        stage = world.Agenda.Stage;
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, world.Facts, runner, response,
            Decision.Take(Assert.Single(response.Affordances, a => a.AnchorId == quake.ObjectId).Id), []));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(rng, System.Text.Json.JsonSerializer.Serialize(world.Random.Generator.GetState()));
        Assert.Equal(pending, world.Agenda.Current);
        Assert.Equal(stage, world.Agenda.Stage);
        Assert.True(quake.Ready);
        new RedSkullFixture(world, runner).Finish();
        Assert.Equal(0, minion.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void OrdinaryZeroSchemeDamagesExactActorAtEitherSeat(int seat)
    {
        // FAQ 08012: "An enemy with 0 SCH still schemes" in alter-ego form.
        var world = WidowEncounterFixture.Board(false);
        var runner = Runner();
        var quake = world.CreateCard("08012", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var minion = world.CreateCard("08026", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(seat)));
        var other = world.CreateCard("08026", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1 - seat)));
        world.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 2, Subject: minion.ObjectId, Seat: seat));
        var response = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(1, response.Player);
        var offer = Assert.Single(response.Affordances);
        Sequence.Answer(world, world.Facts, runner, response, Decision.Take(offer.Id), []);
        new RedSkullFixture(world, runner).Finish();
        Assert.Equal(2, minion.Damage);
        Assert.Equal(0, other.Damage);
        Assert.False(quake.Ready);
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
    }

    [Fact]
    [Rule("rr:leaves-play.1")]
    public void RetainedAnswerRejectsChangedActorBeforeExhaustingQuake()
    {
        // Defensive stale-state probe; normal accepted ordering is proved separately.
        // A departed card "is considered to be a new copy of the card".
        var world = WidowEncounterFixture.Board(false);
        var runner = Runner();
        var quake = world.CreateCard("08012", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var minion = world.CreateCard("08026", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        world.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 2, Subject: minion.ObjectId, Seat: 1));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var offer = Assert.Single(prompt.Affordances);
        Discard.Card(world, minion, "stale-state probe", []);
        World.MoveToTop(minion, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        string before = world.Digest().Canonical();
        string rng = System.Text.Json.JsonSerializer.Serialize(world.Random.Generator.GetState());
        var pending = world.Agenda.Current;
        var stage = world.Agenda.Stage;
        var window = world.Windows.Current;
        Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, world.Facts, runner,
            prompt, Decision.Take(offer.Id), []));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(rng, System.Text.Json.JsonSerializer.Serialize(world.Random.Generator.GetState()));
        Assert.Equal(pending, world.Agenda.Current);
        Assert.Equal(stage, world.Agenda.Stage);
        Assert.Equal(window, world.Windows.Current);
        Assert.True(quake.Ready);
        Assert.Equal(0, minion.Damage);
        new RedSkullFixture(world, runner).Finish();
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:leaves-play.1")]
    public void AcceptedQuakePaysCostWhenDamageInterruptReentersItsTarget()
    {
        // A departed card "is considered to be a new copy of the card".
        // This synthetic child interrupt runs after Quake has been accepted and exhausted.
        var world = WidowEncounterFixture.Board(false);
        var runner = Runner(interrupt: true);
        var quake = world.CreateCard("08012", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var source = world.CreateCard("01007", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var minion = world.CreateCard("08026", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        int incarnation = minion.Incarnation;
        world.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 2, Subject: minion.ObjectId, Seat: 1));
        var response = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, response, Decision.Take(Assert.Single(response.Affordances).Id), []);
        Assert.False(quake.Ready);
        new RedSkullFixture(world, runner).Finish();
        Assert.True(minion.Incarnation > incarnation);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.Equal(0, minion.Damage);
        Assert.False(quake.Ready);
        Assert.False(world.Agenda.IsBusy);
    }

    private static AbilityRunner Runner(bool interrupt = false)
    {
        string trigger = interrupt
            ? "\"event\":\"WhenDamageWouldBeDealt\",\"timing\":\"ForcedInterrupt\",\"subject\":\"game\""
            : "\"event\":\"WhenSchemeEnds\",\"timing\":\"Response\",\"actor\":\"minion\"";
        string target = interrupt ? "trigger.target" : "trigger.actor";
        return ActualQuake($$$$"""
            {"cards":[
              {"card":"08012","abilities":[{
                "trigger":{"event":"WhenSchemeEnds","timing":"Response","actor":"minion"},
                "cost":{"exhaust":"this"},
                "effect":{"dealDamage":{"cards":"trigger.actor","amount":2}}
              }]},
              {"card":"01007","abilities":[{
                "trigger":{ {{{{trigger}}}} },
                "effect":{"seq":[
                  {"discard":"{{{{target}}}}"},
                  {"putIntoPlay":{"card":{"cardsIn":{"areas":["encounterDiscardPile"],"title":"Taskmaster"}},"where":"engagedWithYou"}}
                ]}
              }]}
            ]}
            """);
    }
    private static AbilityRunner ActualQuake(string synthetic)
    {
        var root = System.Text.Json.Nodes.JsonNode.Parse(synthetic)!;
        var rows = root["cards"]!.AsArray();
        rows.Remove(rows.Single(row => (string?)row!["card"] == "08012"));
        var actual = System.Text.Json.Nodes.JsonNode.Parse(Marvel.Content.Tests.Cards.AuthoredCards.Text)!["cards"]!.AsArray()
            .Single(row => (string?)row!["card"] == "08012");
        rows.Add(actual!.DeepClone());
        return new(AbilityCatalog.Parse(root.ToJsonString()));
    }

}

