using System.Text.Json;
using System.Text.Json.Nodes;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DeferredDefeatReceiptBoundaryTests
{
    [Theory]
    [InlineData("{\"query\":\"mainScheme\"}", "{\"result\":\"boostIconsLookedAt\"}", false)]
    [InlineData("\"chosen\"", "{\"count\":{\"query\":\"minions\"}}", false)]
    [InlineData("\"that\"", "{\"result\":\"boostIconsLookedAt\"}", false)]
    [InlineData("\"chosen\"", "{\"result\":\"boostIconsLookedAt\"}", true)]
    public void BoardReadingTailsStillRaiseWithoutChangingCardsAgendaOrRandom(string target, string amount, bool live)
    {
        // Synthetic controls: opaque defeat work cannot predict another target,
        // or a board-dependent amount even for the departed chosen target.
        var runner = Runner("""
            {"chooseCard":{"from":CHOICE,"effect":{
              "thwart":{"target":"chosen","effect":{"seq":[
                {"removeThreat":{"scheme":INITIAL,"amount":3}},
                {"removeThreat":{"scheme":TARGET,"amount":AMOUNT}}
              ]}}}}}
            """.Replace("TARGET", target, StringComparison.Ordinal).Replace("AMOUNT", amount, StringComparison.Ordinal)
                .Replace("CHOICE", live ? """{"query":"mainScheme"}""" : """{"titled":"Hydra Patrol"}""", StringComparison.Ordinal)
                .Replace("INITIAL", live ? """{"titled":"Hydra Patrol"}""" : "\"chosen\"", StringComparison.Ordinal));
        var world = Board(runner);
        string before = Observe(world);
        long random = world.Random.Generator.WordsConsumed;
        string digest = world.Digest().Canonical();
        var failure = Assert.Throws<RulesNotImplementedException>(() =>
        {
            var action = Assert.Single(runner.Actions(world, 0));
            Assert.Equal(before, Observe(world));
            var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
            var choose = Sequence.Work(world, world.Facts, runner, events)!;
            Sequence.Answer(world, world.Facts, runner, choose, Decision.Take(choose.Affordances[0].Id), events);
        });
        Assert.Contains("unprojected defeat window", failure.Message);
        Assert.Equal(2, world.TheCardIn(DeckType.SideSchemesArea)!.Tokens["k_threat"]);
        Assert.Equal(random, world.Random.Generator.WordsConsumed);
        Assert.Equal(digest, world.Digest().Canonical());
    }

    [Fact]
    public void InspectionReceiptSurvivesHydraPatrolSearchShuffleAndItsDifferentCurrentTop()
    {
        // Synthetic enclosing action observes the production inspection/removal
        // shape's numeric receipt after the real Hydra Patrol defeat procedure.
        var runner = Runner("""
            {"chooseCard":{"from":{"titled":"Hydra Patrol"},"effect":{"seq":[
              {"thwart":{"target":"chosen","effect":{"seq":[
                {"and":[{"removeThreat":{"scheme":"chosen","amount":3}},
                  {"inspectEncounterTop":{"count":1,"result":"boostIconsLookedAt"}}]},
                {"removeThreat":{"scheme":"chosen","amount":{"result":"boostIconsLookedAt"}}}
              ]}}},
              {"if":{"test":{"atLeast":{"value":{"result":"boostIconsLookedAt"},"count":2}},
                "then":{"draw":{"player":"you","count":1}}}}
            ]}}}
            """);
        var world = Board(runner);
        var top = world.CreateCard("04152", world.AreaOf(DeckType.EncounterDeck));
        Assert.Equal(2, world.Facts.PrintedValue(top.FaceId, "Boost", world.Players));
        var draw = world.Seats[0].Deck.Cards[^1];
        var action = Assert.Single(runner.Actions(world, 0));
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        var choose = Sequence.Work(world, world.Facts, runner, events)!;
        Sequence.Answer(world, world.Facts, runner, choose, Decision.Take(choose.Affordances[0].Id), events);
        var order = Sequence.Work(world, world.Facts, runner, events)!;
        Sequence.Answer(world, world.Facts, runner, order,
            Decision.Take(order.Affordances[0].Id, [1, 0], []), events);
        var look = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(PublicDecisionKind.CardLook, look.PublicKind);
        Assert.Equal(new[] { top.ObjectId }, look.ContextCardIds);
        Sequence.Answer(world, world.Facts, runner, look, Decision.Take(look.Affordances[0].Id), events);
        var search = Sequence.Work(world, world.Facts, runner, events)!;
        var minion = Assert.Single(search.Affordances, item => item.AnchorId == top.ObjectId);
        Sequence.Answer(world, world.Facts, runner, search, Decision.Take(minion.Id), events);
        var optional = Sequence.Work(world, world.Facts, runner, events);
        if (optional is not null)
            Sequence.Answer(world, world.Facts, runner, optional, Decision.Decline, events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(1, world.Facts.PrintedValue(
            world.AreaOf(DeckType.EncounterDeck).Cards[^1].FaceId, "Boost", world.Players));
        Assert.Same(world.Seats[0].Hand, draw.Area);
        Assert.False(world.Agenda.IsBusy);
    }

    private static AbilityRunner Runner(string effect) => DoctorStrangeAbilityFixture.WithReplacements(
        JsonNode.Parse($$$$"""
        [{"card":"01006","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
          "effect":{{{{effect}}}}
        }]}]
        """)!.AsArray());

    private static World Board(AbilityRunner runner)
    {
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 8);
        world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("04154", world.AreaOf(DeckType.SideSchemesArea)).PlaceTokens("k_threat", 2);
        world.CreateCard("01182", world.AreaOf(DeckType.EncounterDeck));
        return world;
    }

    private static string Observe(World world) => JsonSerializer.Serialize(new
    {
        Digest = world.Digest().Canonical(), Random = world.Random.Generator.GetState(),
        world.Random.Generator.WordsConsumed, world.Agenda.Current,
        Outstanding = world.Agenda.Outstanding.ToArray(),
    });
}
