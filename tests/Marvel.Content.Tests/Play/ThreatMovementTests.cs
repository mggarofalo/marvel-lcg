using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThreatMovementTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:move.6")]
    [Rule("rr:move.7")]
    public void MoveRemovesThreatDefeatsSourceAndPlacesOnExactDestination()
    {
        // Moved threat is "removed from that scheme" and "placed on that scheme".
        var (world, runner, from, to) = Board();
        var receipt = ThreatMovement.Schedule(world, new(from, to, 99, null, "synthetic", 0));
        Assert.Equal(2, from.Tokens["k_threat"]);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(2, receipt.Removed);
        Assert.Equal(2, receipt.Placed);
        Assert.Equal(DeckType.EncounterDiscardPile, from.Area.Type);
        Assert.Equal(5, to.Tokens["k_threat"]);
        Assert.False(receipt.DestinationForcedResponseTriggered);
    }

    [Theory]
    [InlineData("same")]
    [InlineData("missing")]
    [InlineData("departed")]
    [InlineData("zero")]
    [Rule("rr:move.1")]
    [Rule("rr:move.2")]
    public void InvalidOrEmptyMovesDoNotRemoveAnyThreat(string problem)
    {
        // "cannot move to its same (current) placement"; without a valid
        // "source or destination ... the move cannot be made".
        var (world, runner, from, to) = Board();
        if (problem == "departed") World.MoveToTop(to, world.AreaOf(DeckType.EncounterDiscardPile));
        var receipt = ThreatMovement.Schedule(world, new(from,
            problem == "same" ? from : problem == "missing" ? null : to,
            problem == "zero" ? 0 : 1, null, "synthetic", 0));
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(2, from.Tokens["k_threat"]);
        Assert.Equal(problem == "departed" ? 0 : 3, to.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, receipt.Removed);
        Assert.False(receipt.DestinationForcedResponseTriggered);
    }

    [Fact]
    public void ExactDestinationTriggerSurvivesNestedChoiceAndThreatReset()
    {
        var (world, runner, from, to) = Board("""
            {"card":"01108","abilities":[{
              "trigger":{"event":"WhenThreatPlaced","timing":"ForcedResponse","subject":"this"},
              "effect":{"choose":{"options":[
                {"removeThreat":{"scheme":"this","amount":99}},
                {"placeThreat":{"scheme":{"query":"mainScheme"},"amount":1}}]}}
            }]}
            """);
        var receipt = ThreatMovement.Schedule(world, new(from, to, 1, null, "synthetic", 0));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Assert.True(receipt.DestinationForcedResponseTriggered);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(0), []);
        Assert.Null(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(0, to.Tokens["k_threat"]);
        Assert.True(receipt.DestinationForcedResponseTriggered);
    }

    [Fact]
    public void TriggerCountsWhenEveryDestinationResponseEffectIsPrevented()
    {
        var (world, runner, from, to) = Board("""
            {"card":"01108","abilities":[{
              "trigger":{"event":"WhenThreatPlaced","timing":"ForcedResponse","subject":"this"},
              "effect":{"placeThreat":{"scheme":{"query":"mainScheme"},"amount":1}}
            }]},
            {"card":"01097b","abilities":[{
              "trigger":{"event":"WhenThreatWouldBePlaced","timing":"ForcedInterrupt","subject":"this"},
              "effect":{"preventThreat":99}
            }]}
            """);
        var main = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        var receipt = ThreatMovement.Schedule(world, new(from, to, 1, null, "synthetic", 0));
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(4, to.Tokens["k_threat"]);
        Assert.Equal(0, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.True(receipt.DestinationForcedResponseTriggered);
    }

    [Fact]
    public void AnotherCardsForcedResponseDoesNotCountAsDestinationResponse()
    {
        var (world, runner, from, to) = Board("""
            {"card":"01107","abilities":[{
              "trigger":{"event":"WhenThreatPlaced","timing":"ForcedResponse","subject":"game"},
              "effect":{"exhaust":"this"}
            }]}
            """);
        var receipt = ThreatMovement.Schedule(world, new(from, to, 1, null, "synthetic", 0));
        Sequence.Finish(world, Cards, runner, []);
        Assert.False(from.Ready);
        Assert.False(receipt.DestinationForcedResponseTriggered);
    }

    [Fact]
    public void DecliningDestinationOptionalResponseDoesNotCountAsForcedTrigger()
    {
        var (world, runner, from, to) = Board("""
            {"card":"01108","abilities":[{
              "trigger":{"event":"WhenThreatPlaced","timing":"Response","subject":"this"},
              "effect":{"exhaust":"this"}
            }]}
            """);
        var receipt = ThreatMovement.Schedule(world, new(from, to, 1, null, "synthetic", 0));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Assert.False(receipt.DestinationForcedResponseTriggered);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, []);
        Assert.Null(Sequence.Work(world, Cards, runner, []));
        Assert.True(to.Ready);
        Assert.False(receipt.DestinationForcedResponseTriggered);
    }

    private static (World, AbilityRunner, Card, Card) Board(string row = "")
    {
        // Synthetic contracts isolate the move; they do not admit these alternate abilities.
        var runner = new AbilityRunner(AbilityCatalog.Parse("{\"cards\":[" + row + "]}"));
        var world = Hero("01001a");
        world.Abilities = runner;
        var from = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        var to = world.CreateCard("01108", world.AreaOf(DeckType.SideSchemesArea));
        from.PlaceTokens("k_threat", 2);
        to.PlaceTokens("k_threat", 3);
        return (world, runner, from, to);
    }
}
