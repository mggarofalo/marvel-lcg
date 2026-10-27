using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SupportThreatMovementTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:move.6")]
    [Rule("rr:move.7")]
    public void ExactSourceSupportReceivesOnlyRemovedTokensWithoutSchemePlacementWindows()
    {
        // "If threat is moved off a scheme" it is removed; only "to a scheme" is placement.
        var (world, runner, from, support) = Board("""
            {"card":"01091","abilities":[
              {"trigger":{"event":"WhenThreatWouldBePlaced","timing":"ForcedInterrupt","subject":"game"},
               "effect":{"preventThreat":99}},
              {"trigger":{"event":"WhenThreatPlaced","timing":"ForcedResponse","subject":"game"},
               "effect":{"exhaust":"this"}}]}
            """);
        support.PlaceTokens("k_threat", 4);
        var receipt = ThreatMovement.Schedule(world, new(from, support, 99, support, "synthetic", 1));
        var events = new List<GameEvent>();
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(2, receipt.Removed);
        Assert.Equal(2, receipt.Placed);
        Assert.Equal(6, support.Tokens["k_threat"]);
        Assert.True(support.Ready);
        Assert.False(receipt.DestinationForcedResponseTriggered);
        Assert.Equal(DeckType.EncounterDiscardPile, from.Area.Type);
        Assert.DoesNotContain(events, e => e.Verb == "Place_Threat");
        Assert.Contains(events.OfType<FieldSet>(), e => e.Card == support.ObjectId && e.Verb == "Move_Threat");
    }

    [Theory]
    [InlineData("different-support")]
    [InlineData("wrong-controller")]
    [InlineData("departed")]
    [InlineData("reentered")]
    [InlineData("upgrade")]
    [InlineData("transferred")]
    [Rule("rr:move.2")]
    [Rule("rr:leaves-play.1")]
    public void InvalidSupportDestinationDoesNotRemoveThreat(string reason)
    {
        // No valid "destination ... the move cannot be made"; reentry is "a new copy".
        var (world, runner, from, support) = Board();
        Card destination = reason == "different-support"
            ? world.CreateCard("01091", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1))) : support;
        if (reason == "upgrade")
            destination = world.CreateCard("01057", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1)));
        var receipt = ThreatMovement.Schedule(world,
            new(from, destination, 1, support, "synthetic", reason == "wrong-controller" ? 0 : 1));
        if (reason is "departed" or "reentered")
        {
            World.MoveToTop(destination, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
            if (reason == "reentered")
                World.MoveToTop(destination, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1)));
        }
        if (reason == "transferred")
            World.MoveToTop(destination, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0)));
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(2, from.Tokens["k_threat"]);
        Assert.Equal(0, destination.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, receipt.Removed);
        Assert.Equal(0, receipt.Placed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:leaves-play.1")]
    public void SupportDepartureDuringSourceDefeatCannotReceiveTokens(bool reenter)
    {
        // Leaving play and reentering makes "a new copy of the card".
        var (world, runner, from, support) = Board("""
            {"card":"01107","abilities":[{
              "trigger":{"event":"WhenCardDefeated","timing":"WhenDefeated","subject":"this"},
              "effect":{"choose":{"options":[{"placeCounters":{"card":"this","counter":"seen","count":2}},{"placeCounters":{"card":"this","counter":"seen","count":1}}]}}
            }]}
            """);
        var receipt = ThreatMovement.Schedule(world, new(from, support, 2, support, "synthetic", 1));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(2, receipt.Removed);
        Assert.Equal(0, support.Tokens.GetValueOrDefault("k_threat"));
        World.MoveToTop(support, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
        if (reenter) World.MoveToTop(support, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1)));
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(0), []);
        Assert.Null(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(0, receipt.Placed);
        Assert.Equal(0, support.Tokens.GetValueOrDefault("k_threat"));
    }

    [Theory]
    [InlineData("preventThreatRemoval", 0)]
    [InlineData("preventThwarting", 1)]
    [Rule("rr:move.6")]
    [Rule("rr:cannot.1")]
    public void MovementHonorsRemovalProhibitionButIsNotAThwart(string prohibition, int moved)
    {
        // Moved threat is "removed"; "cannot" is absolute. Movement is not a thwart.
        var (world, runner, from, support) = Board("""
            {"card":"01107","abilities":[{
              "trigger":{"timing":"Constant","subject":"this"},
              "effect":{"PROHIBITION":"this"}}]}
            """.Replace("PROHIBITION", prohibition, StringComparison.Ordinal));
        var receipt = ThreatMovement.Schedule(world, new(from, support, 1, support, "synthetic", 1));
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(moved, receipt.Removed);
        Assert.Equal(moved, receipt.Placed);
        Assert.Equal(2 - moved, from.Tokens["k_threat"]);
        Assert.Equal(moved, support.Tokens.GetValueOrDefault("k_threat"));
    }

    private static (World, AbilityRunner, Card, Card) Board(string row = "")
    {
        // Synthetic supports isolate the generic operation without admitting Beat Cop.
        var runner = new AbilityRunner(AbilityCatalog.Parse("{\"cards\":[" + row + "]}"));
        var world = Hero("01001a", 2);
        world.Abilities = runner;
        var from = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        from.PlaceTokens("k_threat", 2);
        var support = world.CreateCard("01091", world.AreaOf(DeckType.SupportsArea,
            PlayArea.Of(1), cardOwner: 0));
        return (world, runner, from, support);
    }
}
