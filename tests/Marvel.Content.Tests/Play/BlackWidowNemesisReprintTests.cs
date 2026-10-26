using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BlackWidowNemesisReprintTests
{
    [Theory]
    [InlineData(1, 4)]
    [InlineData(2, 5)]
    public void KillerForHireAddsToItsPrintedStartingThreat(int players, int threat)
    {
        var world = ThorActualFixture.Board("06001a", players);
        var fixture = new RedSkullFixture(world, AuthoredCards.Runner());
        var scheme = fixture.Reveal("08027", players - 1);
        fixture.Finish();
        Assert.Equal(threat, scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        Assert.Equal(1, StateFields.Modified(world, scheme, "acceleration_icon", world.Facts, players));
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        // This prepared main-scheme face has no escalation: the extra point is this icon alone.
        Assert.Equal(0, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, StateFields.Modified(world, main, "escalation_threat", world.Facts, players));
        world.Agenda.Add(new PhaseStep(new ThreatStageOperation(ThreatStageKind.PlaceThreat), 1, 1));
        fixture.Finish();
        Assert.Equal(1, main.Tokens["k_threat"]);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:guard.1")]
    public void ActualMercenaryGuardsOnlyItsEngagedSeatAndDoesNotBlockNonattackDamage()
    {
        // "The engaged player cannot attack any villain."
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var fixture = new RedSkullFixture(world, runner);
        var mercenary = fixture.Reveal("08028");
        fixture.Finish();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Assert.Equal(DeckType.EngagedEnemiesArea, mercenary.Area.Type);
        Assert.Contains("HYDRA", world.Facts.Traits(mercenary.FaceId));
        Assert.Contains("08028", AuthoredCards.Book.Authored);
        Assert.Empty(AuthoredCards.Book.On("08028"));
        string before = world.Digest().Canonical();
        Assert.Throws<RulesNotImplementedException>(() => BasicPowers.BasicAttack(world, world.Facts, 0, villain, []));
        Assert.Equal(before, world.Digest().Canonical());
        var kick = world.CreateCard("01005", world.Seats[0].Hand);
        world.CreateCard("08020", world.Seats[0].Hand);
        world.CreateCard("08021", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == kick.ObjectId);
        Assert.DoesNotContain(villain.ObjectId, runner.Describe(world, action).Targets!.Legal);
        BasicPowers.BasicAttack(world, world.Facts, 1, villain, []);
        fixture.Finish();
        Assert.Equal(2, villain.Damage);
        var stomp = world.CreateCard("01022", world.Seats[0].Hand);
        var energy = world.CreateCard("08022", world.Seats[0].Hand);
        runner.ExecuteAction(world, new PlayerAction(new PendingAbility(stomp.ObjectId, AbilityType.Action, 0),
            [energy.ObjectId], []));
        fixture.Finish();
        Assert.Equal(3, villain.Damage);
        Assert.Equal(1, mercenary.Damage);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData("08027", 3)]
    [InlineData("08028", 1)]
    [Rule("rr:boost-boost-icon")]
    public void ActualFacesSupplyOnlyTheirPrintedNumericBoost(string face, int boost)
    {
        // "The number of boost icons ... is added to the villain's SCH value."
        var world = ThorActualFixture.Board("06001b");
        var runner = AuthoredCards.Runner();
        var card = world.CreateCard(face, world.AreaOf(DeckType.EncounterDeck));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        world.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 2, Subject: villain.ObjectId, Seat: 0));
        new RedSkullFixture(world, runner).Finish();
        Assert.Equal(1 + boost, main.Tokens["k_threat"]);
        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
