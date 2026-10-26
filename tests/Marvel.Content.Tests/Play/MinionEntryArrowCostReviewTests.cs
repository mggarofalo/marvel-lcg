using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic cost packets challenge payment ownership, not printed 06003 admission.</summary>
public sealed class MinionEntryArrowCostReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cost-arrow-icon.1")]
    [Rule("rr:confuse-confused")]
    public void NestedResponseCanBeDeclinedOrApplyConfusedBeforeEffect(bool accept)
    {
        // "Responses to the text preceding the cost arrow icon resolve before"
        // the following text. Confused replaces the next thwart after payment.
        var world = Board();
        var runner = Runner("""{"discardUntilMinionEngaged":"you"}""", """
            {"choose":{"descriptions":["Become confused","Become tough"],"options":[
              {"giveStatus":{"card":"you","status":"confused"}},
              {"giveStatus":{"card":"you","status":"tough"}}
            ]}}
            """);
        var source = world.CreateCard("06003", world.Seats[0].Hand);
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var scheme = world.AreaOf(DeckType.MainSchemesArea).Cards.Single();
        var action = Assert.Single(runner.Actions(world, 0));
        var events = new List<GameEvent>(runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events);
        int responses = 0, choices = 0;
        Finish(world, runner, events, prompt =>
        {
            Assert.Equal(6, scheme.Tokens["k_threat"]);
            var response = prompt.Affordances.SingleOrDefault(a => a.Timing == AbilityType.Response);
            if (response is not null)
            {
                responses++;
                return accept ? Decision.Take(response.Id) : Decision.Decline;
            }
            Assert.Equal(Question.Option, prompt.Asking);
            choices++;
            string before = world.Digest().Canonical();
            var random = world.Random.Generator.GetState();
            var agenda = world.Agenda.Outstanding.ToArray();
            Assert.Throws<ArgumentException>(() => Sequence.Answer(world, Cards, runner,
                prompt, Decision.Take(int.MaxValue), events));
            Assert.Equal(before, world.Digest().Canonical());
            Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
            Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(agenda, world.Agenda.Outstanding);
            return Decision.Take(prompt.Affordances[0].Id);
        });
        Assert.Equal(1, responses);
        Assert.Equal(accept ? 1 : 0, choices);
        Assert.Equal(accept ? 6 : 3, scheme.Tokens["k_threat"]);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Confused));
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cost.5")]
    public void OtherPhysicalCostsArePaidBeforeEntryResponses(bool entryFirst)
    {
        // "If multiple costs ... require payment, those costs must be paid simultaneously."
        var world = Board();
        string entry = """{"discardUntilMinionEngaged":"you"}""";
        string exhaust = """{"exhaust":"you"}""";
        var runner = Runner("{\"seq\":[" + (entryFirst ? entry + "," + exhaust : exhaust + "," + entry) + "]}",
            """{"draw":{"player":"you","count":1}}""");
        world.CreateCard("06003", world.Seats[0].Hand);
        world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var action = Assert.Single(runner.Actions(world, 0));
        var events = new List<GameEvent>(runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events);
        int responses = 0;
        Finish(world, runner, events, prompt =>
        {
            responses++;
            Assert.False(world.Seats[0].IdentityCard.Ready);
            return Decision.Decline;
        });
        Assert.Equal(1, responses);
        Assert.Equal(3, world.AreaOf(DeckType.MainSchemesArea).Cards.Single().Tokens["k_threat"]);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:cost.6")]
    public void RepeatedOfferQueriesPreserveTheUnprobedExecution()
    {
        // Admission must not pay a cost or inspect hidden identities to find a target.
        var queried = Board();
        var control = Board();
        var runner = Runner("""{"discardUntilMinionEngaged":"you"}""", """{"draw":{"player":"you","count":1}}""");
        foreach (var world in new[] { queried, control })
        {
            world.CreateCard("06003", world.Seats[0].Hand);
            world.CreateCard("01100", world.AreaOf(DeckType.EncounterDeck));
            world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        }
        string before = queried.Digest().Canonical();
        var random = queried.Random.Generator.GetState();
        var agenda = queried.Agenda.Outstanding.ToArray();
        for (int index = 0; index < 5; index++) Assert.Single(runner.Actions(queried, 0));
        Assert.Equal(before, queried.Digest().Canonical());
        Assert.Equal(random.Index, queried.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, queried.Random.Generator.GetState().Words);
        Assert.Equal(agenda, queried.Agenda.Outstanding);
        foreach (var world in new[] { queried, control })
        {
            var action = Assert.Single(runner.Actions(world, 0));
            var events = new List<GameEvent>(runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events);
            Finish(world, runner, events, _ => Decision.Decline);
        }
        Assert.Equal(control.Digest().Canonical(), queried.Digest().Canonical());
        Assert.Equal(control.Random.Generator.GetState().Index, queried.Random.Generator.GetState().Index);
        Assert.Equal(control.Random.Generator.GetState().Words, queried.Random.Generator.GetState().Words);
        Assert.Empty(queried.Agenda.Outstanding);
        Assert.Empty(control.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:you-your")]
    public void EngagementBindsTheActingPlayersSourceRatherThanTheThorSeat()
    {
        // "you" refers to the player controlling the card, rather than another hero.
        var world = Board(players: 2);
        var runner = Runner("""{"discardUntilMinionEngaged":"you"}""", """{"draw":{"player":"you","count":1}}""");
        var source = world.CreateCard("06003", world.Seats[1].Hand);
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        Assert.Empty(runner.Actions(world, 0));
        var action = Assert.Single(runner.Actions(world, 1));
        var events = new List<GameEvent>(runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events);
        Finish(world, runner, events, _ => throw new InvalidOperationException("Thor cannot respond to another player's engagement"));
        Assert.Equal(PlayArea.Of(1), minion.Area.PlayArea);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(PlayArea.Of(1), source.Area.PlayArea);
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.Equal(3, world.AreaOf(DeckType.MainSchemesArea).Cards.Single().Tokens["k_threat"]);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static World Board(int players = 1)
    {
        var world = Hero("06001a", players);
        foreach (var seat in world.Seats)
            for (int index = 0; index < 4; index++) world.CreateCard("01088", seat.Deck);
        world.CreateCard("01107", world.AreaOf(DeckType.MainSchemesArea)).PlaceTokens("k_threat", 6);
        world.CreateCard("01100", world.AreaOf(DeckType.EncounterDeck));
        return world;
    }

    private static AbilityRunner Runner(string cost, string response) => new(AbilityCatalog.Parse("""
        {"cards":[
          {"card":"06003","abilities":[{
            "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
            "cost":COST,
            "labels":["thwart"],
            "effect":{"thwart":{"target":{"query":"mainScheme"},"effect":{"removeThreat":{"scheme":{"query":"mainScheme"},"amount":3}}}}
          }]},
          {"card":"06001a","abilities":[{
            "trigger":{"event":"WhenMinionEngaged","timing":"Response","subject":"you","form":"hero"},
            "effect":RESPONSE
          }]}
        ]}
        """.Replace("COST", cost, StringComparison.Ordinal).Replace("RESPONSE", response, StringComparison.Ordinal)));

    private static void Finish(World world, AbilityRunner runner, List<GameEvent> events, Func<Prompt, Decision> answer)
    {
        for (int index = 0; index < 30; index++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null) return;
            Sequence.Answer(world, Cards, runner, prompt, answer(prompt), events);
        }
        Assert.Fail("cost continuation did not settle");
    }
}
