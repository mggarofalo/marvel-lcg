using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class StealthStrikeContinuationTests
{
    [Theory]
    [InlineData("01167", true)]
    [InlineData("01182", true)]
    [InlineData("01181", false)]
    [InlineData("06028", false)]
    [Rule("rr:damage.step.6")]
    [Rule("rr:damage.step.8")]
    public void PrintedAttackRetainsActualDefeatForFollowingSchemeChoice(string enemyFace, bool defeated)
    {
        // Printed 08013 requires defeating that enemy by this attack before removing threat.
        // Damage steps 6/8: "when [character] would be defeated" precedes "Discarding of a defeated character."
        var runner = Runner();
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var enemy = world.CreateCard(enemyFace, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("01186", world.AreaOf(DeckType.EncounterDeck));
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 5);
        var card = world.CreateCard("08013", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        var extra = world.CreateCard("01089", world.Seats[0].Hand);
        var events = runner.ExecuteAction(world, new PlayerAction(
            Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId), [payment.ObjectId, extra.ObjectId], [])).Events.ToList();
        var target = Assert.IsType<Marvel.Rules.Prompts.Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Sequence.Answer(world, world.Facts, runner, target, Decision.Take(enemy.ObjectId), events);
        var followup = Sequence.Work(world, world.Facts, runner, events);
        Assert.Equal(defeated, followup is not null);
        if (followup is not null)
            Sequence.Answer(world, world.Facts, runner, followup, Decision.Take(scheme.ObjectId), events);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(defeated ? 3 : 5, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(defeated ? DeckType.EncounterDiscardPile : DeckType.EngagedEnemiesArea, enemy.Area.Type);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [Rule("rr:damage.step.9")]
    public void SettledDefeatSurvivesResponseChoiceAndRejectsForeignScheme(bool suspended, bool chooseSide)
    {
        // Step 9: abilities "after [character] defeats/is defeated" finish before the following instruction.
        var runner = Runner(suspended);
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var enemy = world.CreateCard("01167", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        int incarnation = enemy.Incarnation;
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 5);
        var side = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        side.PlaceTokens("k_threat", 5);
        var foreign = world.CreateCard("01107", world.AreaOf(DeckType.EncounterDeck));
        if (suspended) world.CreateCard("08023", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var (card, events) = Start(world, runner);
        Answer(world, runner, Work(world, runner, events), enemy.ObjectId, events);
        var prompt = Work(world, runner, events);
        if (suspended)
        {
            Assert.Equal(DeckType.EncounterDiscardPile, enemy.Area.Type);
            Answer(world, runner, prompt, Assert.Single(prompt.Affordances).Id, events);
            prompt = Work(world, runner, events);
            Assert.Equal(5, main.Tokens["k_threat"]);
            // A synthetic response can put the physical card back into play while
            // the attack retains the defeated copy's causal identity.
            if (chooseSide) World.MoveToTop(enemy, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            Answer(world, runner, prompt, prompt.Affordances[1].Id, events);
            prompt = Work(world, runner, events);
        }
        Assert.Equal(new[] { main.ObjectId, side.ObjectId }, prompt.Affordances.Select(a => a.Id).Order());
        string before = Snapshot(world);
        var current = world.Agenda.Current!.Value;
        var memory = Assert.IsType<AbilityContinuationState>(current.Require<AbilityContinuationAgendaOperation>().Continuation).Memory;
        Assert.Contains((enemy.ObjectId, incarnation), memory.DamageDefeats);
        if (enemy.Incarnation != incarnation)
            Assert.DoesNotContain((enemy.ObjectId, enemy.Incarnation), memory.DamageDefeats);
        for (int query = 0; query < 3; query++) Assert.Equal(2, runner.Choosing(world, current)!.Affordances.Count);
        Assert.Equal(before, Snapshot(world));
        Assert.Throws<ArgumentException>(() => Answer(world, runner, prompt, foreign.ObjectId, events));
        Assert.Equal(before, Snapshot(world));
        Assert.Equal(memory, Assert.IsType<AbilityContinuationState>(world.Agenda.Current!.Value.Require<AbilityContinuationAgendaOperation>().Continuation).Memory);
        Answer(world, runner, prompt, chooseSide ? side.ObjectId : main.ObjectId, events);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(chooseSide ? 5 : 3, main.Tokens["k_threat"]);
        Assert.Equal(chooseSide ? 3 : 5, side.Tokens["k_threat"]);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void DefeatWithNoThreatToRemoveFinishesWithoutSchemeChoice()
    {
        var runner = Runner();
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var enemy = world.CreateCard("01167", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var (card, events) = Start(world, runner);
        Answer(world, runner, Work(world, runner, events), enemy.ObjectId, events);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(DeckType.EncounterDiscardPile, enemy.Area.Type);
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void RepeatedSchemeQueriesMatchAnUnprobedResolution()
    {
        Assert.Equal(ResolveWithQueries(false), ResolveWithQueries(true));
    }

    private static string ResolveWithQueries(bool probe)
    {
        var runner = Runner();
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var enemy = world.CreateCard("01167", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 5);
        var (_, events) = Start(world, runner);
        Answer(world, runner, Work(world, runner, events), enemy.ObjectId, events);
        var prompt = Work(world, runner, events);
        var current = world.Agenda.Current!.Value;
        string before = Snapshot(world);
        if (probe)
            for (int i = 0; i < 4; i++) Assert.Single(runner.Choosing(world, current)!.Affordances);
        Assert.Equal(before, Snapshot(world));
        Answer(world, runner, prompt, scheme.ObjectId, events);
        Assert.Null(Sequence.Work(world, world.Facts, runner, events));
        return Snapshot(world) + System.Text.Json.JsonSerializer.Serialize(events);
    }

    [Fact]
    public void ArbitrarySchemeChoiceInsideLabelledAttackRemainsUnsupportedBeforePayment()
    {
        var runner = Runner(inside: true);
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        world.CreateCard("01167", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
        world.CreateCard("08013", world.Seats[0].Hand);
        world.CreateCard("01088", world.Seats[0].Hand);
        world.CreateCard("01089", world.Seats[0].Hand);
        string before = Snapshot(world);
        var error = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0).ToArray());
        Assert.Contains("suspends inside a labelled power", error.Message);
        Assert.Equal(before, Snapshot(world));
    }

    private static (Card Card, List<GameEvent> Events) Start(World world, AbilityRunner runner)
    {
        var card = world.CreateCard("08013", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        var extra = world.CreateCard("01089", world.Seats[0].Hand);
        var events = runner.ExecuteAction(world, new PlayerAction(
            Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId), [payment.ObjectId, extra.ObjectId], [])).Events.ToList();
        return (card, events);
    }

    private static Prompt Work(World world, AbilityRunner runner, List<GameEvent> events) =>
        Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));

    private static void Answer(World world, AbilityRunner runner, Prompt prompt, int choice, List<GameEvent> events) =>
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(choice), events);

    private static string Snapshot(World world) => System.Text.Json.JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, world.Agenda.Stage, world.Agenda.Current,
        Frames = world.Agenda.Outstanding.Select(step => new { step.What, step.Index, step.Subject }),
    });

    internal static AbilityRunner Runner(bool suspended = false, bool inside = false)
    {
        var root = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        root["cards"]!.AsArray().Add(JsonNode.Parse("""
            {"card":"08013","abilities":[{
              "trigger":{"event":"TurnAction","timing":"Action","subject":"you","form":"hero"},
              "labels":["attack"],
              "effect":{"chooseCard":{"from":{"query":"enemies"},"effect":{"seq":[
                {"attack":{"target":"chosen","effect":{"dealAttackDamage":{"cards":"chosen","amount":4}}}},
                {"if":{"test":{"wasDefeatedByDamage":"chosen"},"then":
                  {"chooseCard":{"from":{"query":"schemes"},"effect":{"removeThreat":{"scheme":"chosen","amount":2}}}}}}
              ]}}}
            }]}
            """));
        if (inside)
        {
            var sequence = root["cards"]!.AsArray()[^1]!["abilities"]![0]!["effect"]!["chooseCard"]!["effect"]!["seq"]!.AsArray();
            var attack = sequence[0]!["attack"]!;
            attack["effect"] = new JsonObject { ["seq"] = new JsonArray(attack["effect"]!.DeepClone(), sequence[1]!.DeepClone()) };
            sequence.RemoveAt(1);
        }
        if (suspended) root["cards"]!.AsArray().Add(JsonNode.Parse("""
            {"card":"08023","abilities":[{
              "trigger":{"event":"WhenCardDefeated","timing":"Response","subject":"game"},
              "effect":{"choose":{"options":[
                {"giveStatus":{"card":"you","status":"confused"}},
                {"giveStatus":{"card":"you","status":"stunned"}}
              ]}}
            }]}
            """));
        return new AbilityRunner(AbilityCatalog.Parse(root.ToJsonString()));
    }
}
