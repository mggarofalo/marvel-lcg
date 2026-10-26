using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using System.Text.Json.Nodes;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic book exercises the complete 06014 body without admitting the Thor pack.
public sealed class GetOverHereLivingTargetTests
{
    [Theory]
    [InlineData(2, false, false, true, 1, false)]
    [InlineData(0, false, false, true, 1, true)]
    [InlineData(2, true, false, true, 1, true)]
    [InlineData(2, false, true, true, 1, true)]
    [InlineData(0, false, false, false, 1, false)]
    [InlineData(0, false, false, true, 0, false)]
    [Rule("rr:in-play-and-out-of-play.4")]
    [Rule("rr:engage.3")]
    public void ActualAttackEngagesOnlyTheSurvivingChosenMinion(
        int damage, bool tough, bool prevention, bool aerial, int engaged, bool transfers)
    {
        // Abilities "can only target, cards that are in play"; a minion already
        // engaged cannot "engage with the same player again" (also FAQ 06014).
        var (world, runner, source, target) = Board(engaged, aerial);
        target.TakeDamage(damage);
        if (tough) Statuses.Give(world, target, Statuses.Tough);
        if (prevention) world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: "preventDamage", Amount: 1, Affects: target.ObjectId, Lasts: new Duration(Uses: 1)));
        int incarnation = target.Incarnation;
        var decoy = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        Start(world, runner, source, target);
        int responses = Finish(world, runner);
        AssertTarget(target, damage, tough, prevention, engaged, transfers, incarnation);
        Assert.Equal(transfers ? 1 : 0, responses);
        Assert.Equal(transfers ? 2 : 0, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(0, decoy.Damage);
        Assert.Equal(PlayArea.Of(1), decoy.Area.PlayArea);
        Assert.False(Statuses.Has(world, target, Statuses.Tough));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static void AssertTarget(Card target, int damage, bool tough, bool prevention,
        int engaged, bool transfers, int incarnation)
    {
        bool lethal = damage == 2 && !tough && !prevention;
        Assert.Equal(lethal ? DeckType.EncounterDiscardPile : DeckType.EngagedEnemiesArea, target.Area.Type);
        if (lethal) return;
        Assert.Equal(PlayArea.Of(transfers ? 0 : engaged), target.Area.PlayArea);
        Assert.Equal(incarnation, target.Incarnation);
        Assert.Equal(damage + (tough || prevention ? 0 : 1), target.Damage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:leaves-play.1")]
    [Rule("rr:in-play-and-out-of-play.4")]
    public void SuspendedFollowUpCannotEngageADepartedOrReenteredCopy(bool reenter)
    {
        // "When a card leaves play ... it is considered to be a new copy of the card."
        var (world, runner, source, target) = Board(1, true, suspend: true);
        // A synthetic unlabelled continuation suspends the retained binding after
        // the attack. The exact labelled actual-card body is exercised above.
        Start(world, runner, source, target);
        var nested = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Equal(Question.Option, nested.Asking);
        Assert.Equal(1, target.Damage);
        int incarnation = target.Incarnation;
        Discard.Card(world, target, "Discard", []);
        if (reenter) World.MoveToTop(target, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        Sequence.Answer(world, world.Facts, runner, nested, Decision.Take(nested.Affordances[0].Id), []);
        Assert.Equal(0, Finish(world, runner));
        Assert.Equal(reenter ? DeckType.EngagedEnemiesArea : DeckType.EncounterDiscardPile, target.Area.Type);
        if (reenter)
        {
            Assert.True(target.Incarnation > incarnation);
            Assert.Equal(PlayArea.Of(1), target.Area.PlayArea);
        }
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void ProjectionQueriesPreserveRandomPendingWorkAndSubsequentResolution()
    {
        var (probed, runner, source, target) = Board(1, true);
        var (control, controlRunner, controlSource, controlTarget) = Board(1, true);
        var before = probed.Digest().Canonical();
        var random = probed.Random.Generator.GetState();
        var pending = probed.Agenda.Outstanding.ToArray();
        for (int i = 0; i < 3; i++)
        {
            var action = Assert.Single(runner.Actions(probed, 0), a => a.Card == source.ObjectId);
            runner.Describe(probed, action);
        }
        Assert.Equal(before, probed.Digest().Canonical());
        Assert.Equal(random.Words, probed.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, probed.Random.Generator.GetState().Index);
        Assert.Equal(pending, probed.Agenda.Outstanding);
        Start(probed, runner, source, target);
        Start(control, controlRunner, controlSource, controlTarget);
        Assert.Equal(Finish(control, controlRunner), Finish(probed, runner));
        Assert.Equal(control.Digest().Canonical(), probed.Digest().Canonical());
        Assert.Equal(control.Random.Generator.GetState().Words, probed.Random.Generator.GetState().Words);
        Assert.Equal(control.Random.Generator.GetState().Index, probed.Random.Generator.GetState().Index);
        Assert.Equal(control.Agenda.Outstanding, probed.Agenda.Outstanding);
    }

    private static (World World, AbilityRunner Runner, Card Source, Card Target) Board(
        int engaged, bool aerial, bool suspend = false)
    {
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = new AbilityRunner(AbilityCatalog.Parse(Book(suspend)));
        world.Abilities = runner;
        if (aerial) world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: Traits.Granted + "AERIAL", Affects: world.Seats[0].IdentityCard.ObjectId));
        var source = world.CreateCard("06014", world.Seats[0].Hand);
        var target = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(engaged)));
        return (world, runner, source, target);
    }

    private static void Start(World world, AbilityRunner runner, Card source, Card target)
    {
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Contains(prompt.Affordances, a => a.AnchorId == target.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(target.ObjectId), []);
    }

    private static int Finish(World world, AbilityRunner runner)
    {
        int responses = 0;
        for (int i = 0; i < 40; i++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) { Assert.Empty(world.Agenda.Outstanding); return responses; }
            var response = prompt.Affordances.SingleOrDefault(a =>
                a.AnchorId == world.Seats[0].IdentityCard.ObjectId && a.Timing == AbilityType.Response);
            if (response is not null) responses++;
            Sequence.Answer(world, world.Facts, runner, prompt,
                response is not null ? Decision.Take(response.Id) : Decision.Decline, []);
        }
        Assert.Fail("Actual Get Over Here composition did not complete");
        return responses;
    }

    private static string Book(bool suspend)
    {
        const string json = """
        {"cards":[
          {"card":"06001a","abilities":[{"limitPerPhase":1,
            "trigger":{"event":"WhenMinionEngaged","timing":"Response","subject":"you","form":"hero"},
            "effect":{"draw":{"player":"you","count":2}}}]},
          {"card":"06014","abilities":[{
            "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
            "labels":["attack"],
            "effect":{"chooseCard":{"from":{"query":"attackableMinions"},"effect":{
              "attack":{"target":"chosen","effect":{"seq":[
                {"dealAttackDamage":{"cards":"chosen","amount":1}},
                {"if":{"test":{"and":[{"hasTrait":{"card":"you","trait":"AERIAL"}},
                  {"isInPlay":"chosen"}]},
                  "then":{"putIntoPlay":{"card":"chosen","where":"engagedWithYou"}}}}
              ]}}}}}}]},
          {"card":"01101","abilities":[]}
        ]}
        """;
        if (!suspend) return json;
        var book = JsonNode.Parse(json)!;
        var choice = book["cards"]![1]!["abilities"]![0]!["effect"]!["chooseCard"]!;
        var steps = choice["effect"]!["attack"]!["effect"]!["seq"]!.AsArray();
        choice["effect"] = new JsonObject
        {
            ["seq"] = new JsonArray(
                new JsonObject { ["attack"] = new JsonObject
                    { ["target"] = "chosen", ["effect"] = steps[0]!.DeepClone() } },
                JsonNode.Parse("""
                    {"choose":{"options":[{"draw":{"player":"you","count":1}},
                        {"draw":{"player":"you","count":2}}]}}
                    """),
                steps[1]!.DeepClone()),
        };
        return book.ToJsonString();
    }
}
