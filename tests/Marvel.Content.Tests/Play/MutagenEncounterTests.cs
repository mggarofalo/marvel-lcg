using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
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

    [Fact]
    [Rule("rr:guard.1")]
    public void ThrallBoostBlocksOnlyItsEngagedPlayersAttackUntilItLeaves()
    {
        // rr:guard.1: "cannot attack the villain" while this minion is engaged.
        var f = Create();
        Assert.Contains(f.Villain, BasicPowers.Attackable(f.World, Cards, 1));
        var source = f.World.CreateCard("02024", f.World.AreaOf(DeckType.EncounterDeck));
        Attack(f, f.Villain, 1);
        Assert.Same(source, Assert.Single(BasicPowers.Attackable(f.World, Cards, 1)));
        Assert.Contains(f.Villain, BasicPowers.Attackable(f.World, Cards, 0));
        World.MoveToTop(source, f.World.AreaOf(DeckType.RemovedArea));
        Assert.Contains(f.Villain, BasicPowers.Attackable(f.World, Cards, 1));
    }

    [Theory]
    [InlineData("02022")]
    [InlineData("02025")]
    public void SchemeBoostShufflesOnlyItsExactSourceAfterPlacingThreat(string face)
    {
        var f = Create();
        var main = f.World.CreateCard("02018b", f.World.AreaOf(DeckType.MainSchemesArea));
        var source = f.World.CreateCard(face, f.World.AreaOf(DeckType.EncounterDeck));
        var other = f.World.CreateCard(face, f.World.AreaOf(DeckType.EncounterDiscardPile));
        f.World.Agenda.Add(new PhaseStep(new EnemySchemeOperation(), 1, 2,
            Subject: f.Villain.ObjectId, Seat: 1));
        f.Finish();
        Assert.Equal(DeckType.EncounterDeck, source.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, other.Area.Type);
        Assert.Equal(3, main.Tokens["k_threat"]);
        Assert.All(f.World.Seats, seat => Assert.Equal(0, seat.IdentityCard.Damage));
        Assert.Null(f.World.Activation);
    }

    [Theory]
    [InlineData("02022")]
    [InlineData("02025")]
    [Rule("rr:activation.8")]
    public void DeferredShuffleBelongsToContainingActivationBeforeQueuedBoostlessAttack(string face)
    {
        // 04146 queues a boostless attack after this activation. Both actual boost rows
        // must keep their own source and the non-first activation player.
        // rr:activation.8: additional activation "resolves after the current activation".
        var world = Hero("01001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var villain = world.CreateCard("02014", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("02018b", world.AreaOf(DeckType.MainSchemesArea));
        world.CreateCard("02020", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, villain.ObjectId));
        var deck = world.AreaOf(DeckType.EncounterDeck);
        for (int i = 0; i < 8; i++) world.CreateCard("01186", deck);
        var source = world.CreateCard(face, deck);
        var queued = world.CreateCard("04146", deck);
        var other = world.CreateCard(face, world.AreaOf(DeckType.EncounterDiscardPile));
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2,
            Subject: villain.ObjectId, Seat: 1));
        var events = new List<GameEvent>();
        int defenses = 0;
        for (int i = 0; i < 20; i++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null) break;
            if (prompt.Asking == Question.Defender && prompt.Player == 1 && ++defenses == 2)
            {
                Assert.Equal(1, prompt.Player);
                Assert.Same(deck, source.Area);
                Assert.Equal(DeckType.EncounterDiscardPile, other.Area.Type);
                Assert.Single(events.OfType<AttackCompleted>());
            }
            Assert.True(prompt.Cancellable, prompt.Description);
            Sequence.Answer(world, Cards, runner, prompt, Decision.Decline, events);
        }
        Assert.Equal(2, defenses);
        Assert.Equal(2, events.OfType<AttackCompleted>().Count());
        Assert.All(events.OfType<AttackCompleted>(), attack =>
            Assert.Equal(world.Seats[1].IdentityCard.ObjectId, attack.Target));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, queued.Area.Type);
        Assert.Null(world.Activation);
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
