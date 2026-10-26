using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual research rows; public admission stays closed.
public sealed class MutagenFinalFaceTests : CoreCardTestBase
{
    [Fact]
    public void ReinforcementsCountsOnlyLiveGoblinMinionsAcrossPlayers()
    {
        var f = Create();
        f.World.CreateCard("02024", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        f.World.CreateCard("02023", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        f.World.CreateCard("01101", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        f.World.CreateCard("02024", f.World.AreaOf(DeckType.EncounterDiscardPile));
        var scheme = f.Reveal("02026", 1);
        f.Finish();
        // 02026: "1 additional threat here for each Goblin minion in play".
        Assert.Equal(4, scheme.Tokens["k_threat"]);
    }

    [Fact]
    public void NationBoostEntersAndItsLiveModifierIncludesTheVillain()
    {
        var f = Create();
        var goblin = f.World.CreateCard("02024", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var hydra = f.World.CreateCard("01101", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var nation = f.World.CreateCard("02027", f.World.AreaOf(DeckType.EncounterDeck));
        f.World.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: f.Villain.ObjectId, Seat: 1));
        var events = f.Finish();
        Assert.Single(events.OfType<AttackCompleted>());
        Assert.Equal(3, f.World.Seats[1].IdentityCard.Damage);
        Assert.Equal(DeckType.SideSchemesArea, nation.Area.Type);
        Assert.Equal(4, nation.Tokens["k_threat"]);
        Assert.Equal(2, StateFields.Modified(f.World, goblin, "attack", Cards, 2));
        Assert.Equal(1, StateFields.Modified(f.World, hydra, "attack", Cards, 2));
        World.MoveToTop(nation, f.World.AreaOf(DeckType.RemovedArea));
        Assert.Equal(1, StateFields.Modified(f.World, goblin, "attack", Cards, 2));
        Assert.Null(f.World.Activation);
        Assert.Equal(Outcome.Unfinished, f.World.Result);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DeathFromAboveUsesPrintedStageAndExpiresItsModifier(bool hero)
    {
        var f = Create(hero: hero, stage: "02015");
        f.Reveal("02029", 1);
        var events = f.Finish();
        Assert.Equal(hero ? 1 : 0, events.OfType<AttackCompleted>().Count());
        Assert.Equal(hero ? 5 : 0, f.World.Seats[1].IdentityCard.Damage);
        Assert.Equal(hero ? 1 : 5, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(2, StateFields.Modified(f.World, f.Villain, hero ? "attack" : "scheme", Cards, 2));
        Assert.Null(f.World.Activation);
        Assert.Equal(Outcome.Unfinished, f.World.Result);
    }

    [Fact]
    [Rule("rr:when-defeated-abilities.1")]
    public void OverrunDefeatAssignsEachPlayersExactDiscardedCopies()
    {
        // rr:when-defeated-abilities.1: "Forced Interrupt: When this card is defeated".
        var f = Create(fill: false);
        f.World.FirstPlayer = 1;
        var deck = f.World.AreaOf(DeckType.EncounterDeck);
        f.World.CreateCard("01186", deck);
        var zero = f.World.CreateCard("02024", deck);
        f.World.CreateCard("01101", deck);
        var oneSecond = f.World.CreateCard("02024", deck);
        var oneFirst = f.World.CreateCard("02024", deck);
        var scheme = f.Reveal("02028", 1);
        f.Finish();
        Threat.Remove(f.World, Cards, f.World.ThreatAbilities, new(scheme, 2, "test", "Remove_Threat") { RecordDefeatOn = new Occurrence(1, []) }, []);
        f.Finish();
        Assert.Equal(DeckType.EncounterDiscardPile, scheme.Area.Type);
        Assert.Equal(PlayArea.Of(0), zero.Area.PlayArea);
        Assert.All(new[] { oneFirst, oneSecond }, card => Assert.Equal(PlayArea.Of(1), card.Area.PlayArea));
        Assert.All(new[] { zero, oneFirst, oneSecond }, card => Assert.Equal(DeckType.EngagedEnemiesArea, card.Area.Type));
    }

    [Fact]
    public void WickedAmbitionsUsesStageAndSeparateChoicesForExactMinions()
    {
        var f = Create(stage: "02015", fill: false);
        var deck = f.World.AreaOf(DeckType.EncounterDeck);
        f.World.CreateCard("01186", deck);
        var second = f.World.CreateCard("02024", deck);
        f.World.CreateCard("01101", deck);
        f.World.CreateCard("02014", deck);
        var first = f.World.CreateCard("02024", deck);
        f.Reveal("02032", 1);
        int choices = 0;
        f.Finish(prompt =>
        {
            Assert.Equal(1, prompt.Player);
            int option = choices++;
            Assert.True(option < 2);
            return Decision.Take(prompt.Affordances[option].Id);
        });
        Assert.Equal(2, choices);
        Assert.Equal(3, f.World.Seats[1].IdentityCard.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, first.Area.Type);
        Assert.Equal(DeckType.EngagedEnemiesArea, second.Area.Type);
        Assert.Equal(PlayArea.Of(1), second.Area.PlayArea);
        Assert.Single(deck.Cards);
    }

    private static RedSkullFixture Create(bool hero = true, string stage = "02014", bool fill = true)
    {
        var world = Hero(hero ? "01001a" : "01001b", 2);
        World.MoveToTop(world.Seats[1].IdentityCard, world.AreaOf(DeckType.RemovedArea));
        world.Seats[1].IdentityCard = world.CreateCard(hero ? "01001a" : "01001b", world.Seats[1].Hero);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.CreateCard(stage, world.AreaOf(DeckType.VillainArea));
        world.CreateCard("02018b", world.AreaOf(DeckType.MainSchemesArea));
        if (fill)
            for (int i = 0; i < 8; i++) world.CreateCard("02029", world.AreaOf(DeckType.EncounterDeck));
        return new(world, runner);
    }
}

