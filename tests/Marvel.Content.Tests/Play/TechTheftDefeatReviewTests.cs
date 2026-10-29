using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Canonical cards on a prepared, nonpublic board.
public sealed class TechTheftDefeatReviewTests
{
    [Fact]
    [Rule("rr:side-scheme.2")]
    [Rule("rr:defeat.1")]
    public void RemovingLastThreatDefeatsTheSchemeAndRestoresTechText()
    {
        // A side scheme with no threat is defeated and discarded.
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var armor = world.CreateCard("01036", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        long health = DamagePlacement.Health(world, world.Facts, world.Seats[1].IdentityCard);
        var scheme = world.CreateCard("12026", world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)));
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            Round: 1, Number: 4, Subject: scheme.ObjectId, Seat: 1));
        AntManNemesisCardFixture.Finish(world, runner);
        Assert.Equal(4, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.True(PrintedText.IsBlanked(world, armor));
        Assert.Equal(health - 6, DamagePlacement.Health(world, world.Facts, world.Seats[1].IdentityCard));

        Assert.Equal(3, Threat.Remove(world, world.Facts, world.ThreatAbilities,
            new(scheme, 3, "test", "Remove_Threat") { RecordDefeatOn = new Occurrence(1, []) }, []));
        AntManNemesisCardFixture.Finish(world, runner);
        Assert.Equal(1, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        Assert.True(PrintedText.IsBlanked(world, armor));
        Assert.Equal(health - 6, DamagePlacement.Health(world, world.Facts, world.Seats[1].IdentityCard));

        Assert.Equal(1, Threat.Remove(world, world.Facts, world.ThreatAbilities,
            new(scheme, 1, "test", "Remove_Threat") { RecordDefeatOn = new Occurrence(2, []) }, []));
        AntManNemesisCardFixture.Finish(world, runner);
        Assert.Equal(DeckType.EncounterDiscardPile, scheme.Area.Type);
        Assert.False(PrintedText.IsBlanked(world, armor));
        Assert.Equal(health, DamagePlacement.Health(world, world.Facts, world.Seats[1].IdentityCard));
        Assert.False(world.IsOver);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
