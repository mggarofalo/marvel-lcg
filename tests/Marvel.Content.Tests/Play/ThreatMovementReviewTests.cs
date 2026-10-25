using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThreatMovementReviewTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:when-defeated-abilities.2.1")]
    public void SourceDefeatChoiceFinishesBeforeDestinationPlacement()
    {
        // A defeated card leaves play "after its When Defeated ability is resolved".
        var (world, runner, from, to) = Board(true);
        var receipt = ThreatMovement.Schedule(world, new(from, to, 2, null, "synthetic", 0));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(Question.Option, prompt.Asking);
        Assert.Equal(2, receipt.Removed);
        Assert.Equal(3, to.Tokens["k_threat"]);
        Assert.Equal(0, receipt.Placed);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(0), []);
        Assert.Null(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(DeckType.EncounterDiscardPile, from.Area.Type);
        Assert.Equal(5, to.Tokens["k_threat"]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:leaves-play.1")]
    public void ScheduledMoveDoesNotRetargetAReenteredCopy(bool replaceSource)
    {
        // "When a card leaves play ... it is considered to be a new copy".
        var (world, runner, from, to) = Board();
        var receipt = ThreatMovement.Schedule(world, new(from, to, 1, null, "synthetic", 0));
        var replaced = replaceSource ? from : to;
        World.MoveToTop(replaced, world.AreaOf(DeckType.EncounterDiscardPile));
        World.MoveToTop(replaced, world.AreaOf(DeckType.SideSchemesArea));
        replaced.PlaceTokens("k_threat", replaceSource ? 2 : 3);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(2, from.Tokens["k_threat"]);
        Assert.Equal(3, to.Tokens["k_threat"]);
        Assert.Equal(0, receipt.Removed);
        Assert.Equal(0, receipt.Placed);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:move.2")]
    public void MissingSourceNeverRemovesDestinationThreat(bool missing)
    {
        // Without a valid "source or destination ... the move cannot be made".
        var (world, runner, from, to) = Board();
        if (!missing) World.MoveToTop(from, world.AreaOf(DeckType.EncounterDiscardPile));
        var receipt = ThreatMovement.Schedule(world, new(missing ? null : from, to, 1, null, "synthetic", 0));
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(3, to.Tokens["k_threat"]);
        Assert.Equal(0, receipt.Removed);
        Assert.Equal(0, receipt.Placed);
    }

    [Fact]
    [Rule("rr:move.6")]
    [Rule("rr:move.7")]
    public void EmptySourceDoesNotCreateDestinationPlacement()
    {
        // Moved threat is "removed from that scheme" and "placed on that scheme".
        var (world, runner, from, to) = Board();
        from.PlaceTokens("k_threat", -2);
        var receipt = ThreatMovement.Schedule(world, new(from, to, 2, null, "synthetic", 0));
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(0, receipt.Removed);
        Assert.Equal(0, receipt.Placed);
        Assert.Equal(3, to.Tokens["k_threat"]);
        Assert.False(receipt.DestinationForcedResponseTriggered);
    }

    [Fact]
    [Rule("rr:move.2")]
    public void DestinationLeavingDuringPlacementInterruptDoesNotReceiveThreatInDiscard()
    {
        // Without a valid "source or destination ... the move cannot be made".
        var (world, _, from, to) = Board();
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01108","abilities":[{
              "trigger":{"event":"WhenThreatWouldBePlaced","timing":"ForcedInterrupt","subject":"this"},
              "effect":{"discard":"this"}
            }]}]}
            """));
        world.Abilities = runner;
        var receipt = ThreatMovement.Schedule(world, new(from, to, 1, null, "synthetic", 0));
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(DeckType.EncounterDiscardPile, to.Area.Type);
        Assert.Equal(0, to.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, receipt.Placed);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(99, 0)]
    [Rule("rr:move.6")]
    [Rule("rr:move.7")]
    [Rule("rr:prevent.2")]
    public void DestinationPreventionKeepsExactRemovedAndPlacedAmounts(int prevention, int placed)
    {
        // Moved threat is removed and placed; prevention reduces threat "before it is placed".
        var (world, _, from, to) = Board();
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01108","abilities":[{
              "trigger":{"event":"WhenThreatWouldBePlaced","timing":"ForcedInterrupt","subject":"this"},
              "effect":{"preventThreat":PREVENT}
            }]}]}
            """.Replace("PREVENT", prevention.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)));
        world.Abilities = runner;
        var receipt = ThreatMovement.Schedule(world, new(from, to, 99, null, "synthetic", 0));
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(2, receipt.Removed);
        Assert.Equal(placed, receipt.Placed);
        Assert.Equal(3 + placed, to.Tokens["k_threat"]);
        Assert.Equal(DeckType.EncounterDiscardPile, from.Area.Type);
        Assert.False(receipt.DestinationForcedResponseTriggered);
    }
    private static (World, AbilityRunner, Card, Card) Board(bool defeatChoice = false)
    {
        // Synthetic contracts isolate scheduler behavior without admitting alternate card text.
        string row = defeatChoice ? """
            {"card":"01107","abilities":[{
              "trigger":{"event":"WhenCardDefeated","timing":"WhenDefeated","subject":"this"},
              "effect":{"choose":{"options":[{"placeCounters":{"card":"this","counter":"seen","count":2}},{"placeCounters":{"card":"this","counter":"seen","count":1}}]}}
            }]}
            """ : "";
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
