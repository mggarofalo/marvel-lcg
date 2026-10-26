using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual authored research faces; public Green Goblin admission stays closed.
public sealed class MutagenEncounterTests : CoreCardTestBase
{
    [Theory]
    [InlineData("02023")]
    [InlineData("02024")]
    [Rule("rr:boost-boost-icon.2")]
    public void BoostEntryUsesExactPhysicalCardAndActivationPlayer(string face)
    {
        // Printed Boost: "Put Goblin Soldier [Thrall] into play engaged with you."
        // rr:boost-boost-icon.2: "resolve that ability" when the boost is turned faceup.
        var f = Create();
        var source = f.World.CreateCard(face, f.World.AreaOf(DeckType.EncounterDeck));
        var other = f.World.CreateCard(face, f.World.AreaOf(DeckType.EncounterDiscardPile));
        Attack(f, f.Villain, 1);
        Assert.Equal(DeckType.EngagedEnemiesArea, source.Area.Type);
        Assert.Equal(PlayArea.Of(1), source.Area.PlayArea);
        Assert.Equal(DeckType.EncounterDiscardPile, other.Area.Type);
        Assert.Empty(f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards);
    }

    [Theory]
    [InlineData("02022")]
    [InlineData("02025")]
    [Rule("rr:delayed-effect.1")]
    public void BoostShufflesOnlyItsPhysicalSourceAfterActivation(string face)
    {
        // Printed: "After this activation ends, shuffle this card into the encounter deck."
        // rr:delayed-effect.1: resolves after the "specified timing point" occurs.
        var f = Create();
        var deck = f.World.AreaOf(DeckType.EncounterDeck);
        var other = f.World.CreateCard(face, f.World.AreaOf(DeckType.EncounterDiscardPile));
        var source = f.World.CreateCard(face, deck);
        Attack(f, f.Villain, 1);
        Assert.Same(deck, source.Area);
        Assert.Equal(DeckType.EncounterDiscardPile, other.Area.Type);
        Assert.Null(f.World.Activation);
        Assert.Equal(0, f.World.Seats[0].IdentityCard.Damage);
        Assert.True(f.World.Seats[1].IdentityCard.Damage > 0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void KnightAttackRecruitsFirstDiscardedGoblinEvenAcrossReset(bool reset)
    {
        // 02022: "discard 1 card ... If that card is a Goblin minion, put it into play engaged with you."
        var f = Create(fill: false);
        var deck = f.World.AreaOf(DeckType.EncounterDeck);
        if (!reset) f.World.CreateCard("01186", deck);
        var target = f.World.CreateCard("02024", deck);
        var unrelated = f.World.CreateCard("02023", f.World.AreaOf(DeckType.EncounterDiscardPile));
        var knight = f.Reveal("02022", 1);
        f.Finish();
        Attack(f, knight, 1);
        Assert.Equal(DeckType.EngagedEnemiesArea, target.Area.Type);
        Assert.Equal(PlayArea.Of(1), target.Area.PlayArea);
        Assert.NotEqual(DeckType.EngagedEnemiesArea, unrelated.Area.Type);
        Assert.Single(deck.Cards);
    }

    [Fact]
    public void KnightDoesNotRecruitANonGoblinOrAnOldDiscard()
    {
        var f = Create(fill: false);
        var old = f.World.CreateCard("02024", f.World.AreaOf(DeckType.EncounterDiscardPile));
        f.World.CreateCard("01101", f.World.AreaOf(DeckType.EncounterDeck));
        var knight = f.Reveal("02022", 1);
        f.Finish();
        Attack(f, knight, 1);
        Assert.DoesNotContain(f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)).Cards,
            card => card.FaceId is "01101" or "02024");
        Assert.NotEqual(DeckType.EngagedEnemiesArea, old.Area.Type);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MonsterRevealStunsOrDamagesTheRevealingIdentity(bool alreadyStunned)
    {
        // 02025: "You are stunned. If you are already stunned, take 2 damage."
        var f = Create();
        if (alreadyStunned) Statuses.Give(f.World, f.World.Seats[1].IdentityCard, Statuses.Stunned);
        var monster = f.Reveal("02025", 1);
        f.Finish();
        Assert.Equal(DeckType.EngagedEnemiesArea, monster.Area.Type);
        Assert.Equal(PlayArea.Of(1), monster.Area.PlayArea);
        Assert.Equal(alreadyStunned ? 2 : 0, f.World.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, f.World.Seats[0].IdentityCard.Damage);
        Assert.True(Statuses.Has(f.World, f.World.Seats[1].IdentityCard, Statuses.Stunned));
    }

    [Fact]
    [Rule("rr:when-defeated-abilities.1")]
    public void SoldierDefeatDamagesEngagedPlayerRatherThanAttackingPlayer()
    {
        // rr:when-defeated-abilities.1: "Forced Interrupt: When this card is defeated".
        // 02023: "Deal 1 damage to the engaged player."
        var f = Create();
        var soldier = f.Reveal("02023", 1);
        f.Finish();
        Damage.Attack(f.World, Cards, f.World.Seats[0].IdentityCard, soldier, 5, "test", "Attack", []);
        f.Finish();
        Assert.Equal(DeckType.EncounterDiscardPile, soldier.Area.Type);
        Assert.Equal(1, f.World.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, f.World.Seats[0].IdentityCard.Damage);
    }

    private static RedSkullFixture Create(bool fill = true)
    {
        var world = Hero("01001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        if (fill)
            for (int i = 0; i < 6; i++) world.CreateCard("01186", world.AreaOf(DeckType.EncounterDeck));
        return new(world, runner);
    }

    private static void Attack(RedSkullFixture f, Card enemy, int player)
    {
        f.World.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: enemy.ObjectId, Seat: player));
        f.Finish();
    }
}
