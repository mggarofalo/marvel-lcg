using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SupportThreatMovementReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false, 2)]
    [InlineData(true, 0)]
    [Rule("rr:move.2")]
    [Rule("rr:when-defeated-abilities.2.1")]
    public void StorageRechecksControllerAfterSourceDefeatChoice(bool transfer, int received)
    {
        // Without a valid "destination ... the move cannot be made"; a defeated
        // scheme leaves play "after its When Defeated ability is resolved".
        // Engine boundary: the support must still belong to the resolving controller's area.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01107","abilities":[{
              "trigger":{"event":"WhenCardDefeated","timing":"WhenDefeated","subject":"this"},
              "effect":{"choose":{"options":[{"placeCounters":{"card":"this","counter":"seen","count":2}},
                {"placeCounters":{"card":"this","counter":"seen","count":1}}]}}
            }]}]}
            """));
        var world = Hero("01001a", 2);
        world.Abilities = runner;
        var from = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        from.PlaceTokens("k_threat", 2);
        var support = world.CreateCard("01091", world.AreaOf(DeckType.SupportsArea,
            PlayArea.Of(1), cardOwner: 0));
        int incarnation = support.Incarnation;
        var events = new List<GameEvent>();
        var receipt = ThreatMovement.Schedule(world, new(from, support, 2, support, "synthetic", 1));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(2, receipt.Removed);
        Assert.Equal(0, receipt.Placed);
        Assert.Equal(0, support.Tokens.GetValueOrDefault("k_threat"));
        if (transfer)
            World.MoveToTop(support, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0)));
        Assert.Equal(incarnation, support.Incarnation);
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(0), events);
        Assert.Null(Sequence.Work(world, Cards, runner, events));
        Assert.Empty(world.Agenda.Outstanding);
        Assert.Equal(DeckType.EncounterDiscardPile, from.Area.Type);
        Assert.Equal(2, receipt.Removed);
        Assert.Equal(received, receipt.Placed);
        Assert.Equal(received, support.Tokens.GetValueOrDefault("k_threat"));
        Assert.False(receipt.DestinationForcedResponseTriggered);
        Assert.Equal(transfer ? 0 : 1,
            events.OfType<FieldSet>().Count(e => e.Card == support.ObjectId && e.Verb == "Move_Threat"));
    }
}
