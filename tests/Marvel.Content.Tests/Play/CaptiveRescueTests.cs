using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Rules.Campaign;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CaptiveRescueTests
{
    [Fact]
    public void RescueRejectsAnOrdinaryPlayerAllyWithoutChangingItsOwnershipOrZone()
    {
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        var scheme = world.CreateCard("04107", world.AreaOf(DeckType.SideSchemesArea));
        var ordinary = world.CreateCard("01002", seat.Hand);
        BeneathCards.Place(world, ordinary, scheme, false, "setup", []);
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"04107","abilities":[{
              "trigger":{"timing":"WhenDefeated","event":"WhenCardDefeated","subject":"this"},
              "effect":{"releaseBeneath":{"removeHost":"true","rescue":"true"}}
            }]}]}
            """));
        world.Abilities = runner;
        Defeat.Scheme(world, facts, scheme, "defeat", [], by: 0, recordOn: new Occurrence(1, []));
        Assert.Throws<RulesNotImplementedException>(() => Sequence.Finish(world, facts, runner, []));
        Assert.Equal(0, ordinary.Owner);
        Assert.Equal(scheme.ObjectId, ordinary.Area.Host);
        Assert.False(ordinary.FaceUp);
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [Rule("rr:ownership-and-control.2.2")]
    [Rule("rr:when-defeated-abilities.2.1")]
    public void DefeatingPlayerOwnsThePhysicalCaptiveBeforeTheSchemeLeaves(bool campaign, int rescuer)
    {
        // A player taking control of a scenario-specific player card becomes its owner.
        // A When Defeated ability resolves before the defeated card leaves play.
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 2);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat("p" + player);
            seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        }
        var scheme = world.CreateCard("04107", world.AreaOf(DeckType.SideSchemesArea));
        var captive = world.CreateCard("04097", world.AreaOf(DeckType.AsideDeck));
        var untouched = world.CreateCard("04098", world.AreaOf(DeckType.AsideDeck));
        BeneathCards.Place(world, captive, scheme, false, "setup", []);
        if (campaign)
        {
            var setup = new CampaignGameSetup("synthetic", false, CampaignStage.Taskmaster, 1,
                [new CampaignSeatSetup(0, [], null, false, [], false),
                 new CampaignSeatSetup(1, [], null, false, [], false)], [], 0);
            new CampaignAttemptState(setup,
                [new CampaignCardBinding(captive.ObjectId, -1,
                    new CampaignCard("captive:04097", "04097", CampaignCardRole.RescuedAlly))]).Bind(world);
        }
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"04107","abilities":[{
              "trigger":{"timing":"WhenDefeated","event":"WhenCardDefeated","subject":"this"},
              "effect":{"releaseBeneath":{"removeHost":"true","rescue":"true"}}
            }]}]}
            """));
        world.Abilities = runner;

        Defeat.Scheme(world, facts, scheme, "defeat", [], by: rescuer, recordOn: new Occurrence(1, []));
        Sequence.Finish(world, facts, runner, []);

        Assert.Equal(rescuer, captive.Owner);
        Assert.Same(world.Seats[rescuer].Hand, captive.Area);
        Assert.True(captive.FaceUp);
        Assert.Equal(DeckType.RemovedArea, scheme.Area.Type);
        Assert.Equal(World.Scenario, untouched.Owner);
        Assert.Equal(DeckType.AsideDeck, untouched.Area.Type);
        if (campaign)
        {
            var progress = Assert.Single(world.Campaign!.Cards);
            Assert.True(progress.Rescued);
            Assert.Equal("captive:04097", progress.Binding.Card.Key);
            Assert.Equal(rescuer, progress.Binding.Seat);
        }
    }
}
