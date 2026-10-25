using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
namespace Marvel.Content.Tests.Play;

public sealed class ThreatMovementDslTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:move.6")]
    [Rule("rr:move.7")]
    public void SyntheticMoveResumesAfterDestinationResponseWithoutMovingTwice()
    {
        // Moved threat is "removed from that scheme" and "placed on that scheme".
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01168","abilities":[{"trigger":{"event":"WhenCardRevealed","subject":"this","timing":"WhenRevealed"},
                "effect":{"seq":[
                  {"moveThreat":{"from":{"titled":"Breakin' & Takin'"},"to":{"titled":"Crowd Control"},"amount":1}},
                  {"placeThreat":{"scheme":{"query":"mainScheme"},"amount":{"add":[1,
                    {"result":"threatMoved"},{"result":"threatRemovedByMove"},
                    {"result":"destinationForcedResponseTriggered"}]}}}
                ]}}]},
              {"card":"01108","abilities":[{"trigger":{"event":"WhenThreatPlaced","timing":"ForcedResponse","subject":"this"},
                "effect":{"choose":{"options":[{"draw":{"player":"you","count":1}},{"draw":{"player":"you","count":2}}]}}}]}
            ]}
            """));
        var world = Hero("01001a");
        world.Abilities = runner;
        world.CreateCard("01087", world.Seats[0].Deck);
        world.CreateCard("01088", world.Seats[0].Deck);
        var mainScheme = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        var from = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        var to = world.CreateCard("01108", world.AreaOf(DeckType.SideSchemesArea));
        from.PlaceTokens("k_threat", 2);
        to.PlaceTokens("k_threat", 3);
        var source = world.CreateCard("01168", world.AreaOf(DeckType.RevealingArea));
        runner.ExecuteReveal(world, source, 0);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(1, from.Tokens["k_threat"]);
        Assert.Equal(4, to.Tokens["k_threat"]);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(0), []);
        Assert.Null(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(1, from.Tokens["k_threat"]);
        Assert.Equal(4, to.Tokens["k_threat"]);
        Assert.Equal(4, mainScheme.Tokens["k_threat"]);
    }

    [Theory]
    [InlineData("then", 0, 2, 1)]
    [InlineData("then", 1, 2, 0)]
    [InlineData("then", 2, 2, 0)]
    [InlineData("then", 0, 1, 0)]
    [InlineData("otherwise", 2, 2, 0)]
    [InlineData("otherwise", 0, 0, 1)]
    [Rule("rr:then.1")]
    [Rule("rr:otherwise.1.2")]
    public void SyntheticDependentMoveUsesBothCommittedHalves(
        string branch, int prevention, int available, int followup)
    {
        // "Then" requires the preceding effect to resolve in full; "otherwise"
        // requires that it "cannot at least partially resolve".
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01168","abilities":[{"trigger":{"event":"WhenCardRevealed","subject":"this","timing":"WhenRevealed"},
                "effect":{"BRANCH":{"effect":
                  {"moveThreat":{"from":{"titled":"Breakin' & Takin'"},"to":{"titled":"Crowd Control"},"amount":2}},
                  "BRANCH":{"placeThreat":{"scheme":{"query":"mainScheme"},"amount":1}}}}}]},
              {"card":"01108","abilities":[{"trigger":{"event":"WhenThreatWouldBePlaced","timing":"ForcedInterrupt","subject":"this"},
                "effect":{"preventThreat":PREVENT}}]}
            ]}
            """.Replace("BRANCH", branch, StringComparison.Ordinal)
                .Replace("PREVENT", prevention.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)));
        var world = Hero("01001a");
        world.Abilities = runner;
        var main = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        var from = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        var to = world.CreateCard("01108", world.AreaOf(DeckType.SideSchemesArea));
        from.PlaceTokens("k_threat", available);
        to.PlaceTokens("k_threat", 3);
        var source = world.CreateCard("01168", world.AreaOf(DeckType.RevealingArea));
        runner.ExecuteReveal(world, source, 0);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(0, from.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(3 + Math.Max(0, available - prevention), to.Tokens["k_threat"]);
        Assert.Equal(followup, main.Tokens.GetValueOrDefault("k_threat"));
    }

    [Theory]
    [InlineData(false, 3)]
    [InlineData(true, 0)]
    [Rule("rr:move.6")]
    [Rule("rr:move.7")]
    public void SyntheticResultsDescribeOnlyTheMostRecentMove(bool repeat, int expected)
    {
        // Each move removes from its source and places on its destination.
        // Engine choice: named result slots describe the latest operation.
        const string move = """{"moveThreat":{"from":{"titled":"Breakin' & Takin'"},"to":{"titled":"Crowd Control"},"amount":1}}""";
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01168","abilities":[{"trigger":{"event":"WhenCardRevealed","subject":"this","timing":"WhenRevealed"},
                "effect":{"seq":[MOVE,REPEAT
                  {"placeThreat":{"scheme":{"query":"mainScheme"},"amount":{"add":[
                    {"result":"threatMoved"},{"result":"threatRemovedByMove"},{"result":"destinationForcedResponseTriggered"}]}}}
                ]}}]},
              {"card":"01108","abilities":[{"trigger":{"event":"WhenThreatPlaced","timing":"ForcedResponse","subject":"this"},
                "effect":{"draw":{"player":"you","count":1}}}]}
            ]}
            """.Replace("MOVE", move, StringComparison.Ordinal)
                .Replace("REPEAT", repeat ? move + "," : "", StringComparison.Ordinal)));
        var world = Hero("01001a");
        world.Abilities = runner;
        world.CreateCard("01087", world.Seats[0].Deck);
        var main = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        var from = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        var to = world.CreateCard("01108", world.AreaOf(DeckType.SideSchemesArea));
        from.PlaceTokens("k_threat", 1);
        to.PlaceTokens("k_threat", 3);
        var source = world.CreateCard("01168", world.AreaOf(DeckType.RevealingArea));
        runner.ExecuteReveal(world, source, 0);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(expected, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(4, to.Tokens["k_threat"]);
        Assert.Single(world.Seats[0].Hand.Cards);
    }

    [Theory]
    [InlineData("Crowd Control", 1, 1, true)]
    [InlineData("Crowd Control", 0, 1, false)]
    [InlineData("Crowd Control", 1, 0, false)]
    [InlineData("Breakin' & Takin'", 1, 1, false)]
    [InlineData("Missing Scheme", 1, 1, false)]
    [Rule("rr:move.1")]
    [Rule("rr:move.2")]
    public void SyntheticActionRequiresDistinctLiveEndpointsAndPositiveThreat(
        string destination, int available, int amount, bool offered)
    {
        // An element "cannot move to its same (current) placement";
        // "If there is no valid source or destination ... the move cannot be made."
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "effect":{"moveThreat":{"from":{"titled":"Breakin' & Takin'"},
                "to":{"titled":"DESTINATION"},"amount":AMOUNT}}
            }]}]}
            """.Replace("DESTINATION", destination, StringComparison.Ordinal)
                .Replace("AMOUNT", amount.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)));
        var (_, world) = Playing(_ => { }, abilities: runner);
        var source = InPlay(world, "01006");
        var from = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        world.CreateCard("01108", world.AreaOf(DeckType.SideSchemesArea));
        from.PlaceTokens("k_threat", available);
        Assert.Equal(offered, runner.Actions(world, 0).Any(action => action.Card == source.ObjectId));
        Assert.Equal(available, from.Tokens.GetValueOrDefault("k_threat"));
    }

}
