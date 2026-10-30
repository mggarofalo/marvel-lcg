using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual rows on a prepared board; Ant-Man public setup remains closed.
public sealed class TechTheftActualCardTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:reveal.step.4")]
    [Rule("rr:ability.8.2")]
    public void FullRevealBlanksLiveTechAcrossPlayersAndDepartureRestoresIt(bool throughPlan)
    {
        // Card-text reveals use the same resolution procedure; a constant becomes
        // active as soon as its card enters play and remains active while in play.
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var armor0 = world.CreateCard("01036", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var armor1 = world.CreateCard("01036", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        var ordinary = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        long health0 = DamagePlacement.Health(world, world.Facts, world.Seats[0].IdentityCard);
        long health1 = DamagePlacement.Health(world, world.Facts, world.Seats[1].IdentityCard);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        Card scheme;
        Card? plan = null;
        Card? skipped = null;
        if (throughPlan)
        {
            scheme = world.CreateCard("12026", deck);
            skipped = world.CreateCard("01103", deck);
            plan = world.CreateCard("12029", world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)));
        }
        else
            scheme = world.CreateCard("12026", world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)));
        var revealed = plan ?? scheme;
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            Round: 1, Number: 4, Subject: revealed.ObjectId, Seat: 1));
        AntManNemesisCardFixture.Finish(world, runner);
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        Assert.Equal(4, scheme.Tokens.GetValueOrDefault("k_threat"));
        if (throughPlan)
        {
            Assert.Equal(DeckType.EncounterDiscardPile, plan!.Area.Type);
            Assert.Equal(DeckType.EncounterDiscardPile, skipped!.Area.Type);
        }
        Assert.True(PrintedText.IsBlanked(world, armor0));
        Assert.True(PrintedText.IsBlanked(world, armor1));
        Assert.False(PrintedText.IsBlanked(world, ordinary));
        Assert.Equal(health0 - 6, DamagePlacement.Health(world, world.Facts, world.Seats[0].IdentityCard));
        Assert.Equal(health1 - 6, DamagePlacement.Health(world, world.Facts, world.Seats[1].IdentityCard));
        var late = world.CreateCard("01035", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        Assert.True(PrintedText.IsBlanked(world, late));
        World.MoveToTop(late, world.Seats[1].Hand);
        Assert.False(PrintedText.IsBlanked(world, late));
        Discard.Card(world, scheme, "test", []);
        Assert.False(PrintedText.IsBlanked(world, armor0));
        Assert.False(PrintedText.IsBlanked(world, armor1));
        Assert.Equal(health0, DamagePlacement.Health(world, world.Facts, world.Seats[0].IdentityCard));
        Assert.Equal(health1, DamagePlacement.Health(world, world.Facts, world.Seats[1].IdentityCard));
        Assert.False(world.IsOver);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
