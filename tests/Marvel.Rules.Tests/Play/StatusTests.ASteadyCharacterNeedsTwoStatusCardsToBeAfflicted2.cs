using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public sealed class StatusASteadyCharacterNeedsTwoStatusCardsToBeAfflictedTests : StatusTestBase
{
    [Rule("rr:steady")]
    [Rule("rr:steady.1")]
    [Rule("rr:confuse-confused.3.1")]
    [Rule("rr:stun-stunned.3.1")]
    [Theory]
    [InlineData(Statuses.Stunned)]
    [InlineData(Statuses.Confused)]
    public void ASteadyCharacterNeedsTwoStatusCardsToBeAfflicted(string status)
    {
        // A steady character is stunned or confused "only if it has two"
        // corresponding status cards, and `rr:status-cards.1.1` lets it hold
        // that second card.
        var printed = Cards().With("minion", ("HP", "3"), ("Steady", "1"));
        var world = Board(printed);
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.NotNull(Statuses.Inflict(world, printed, minion, status));
        Assert.False(Statuses.Afflicted(world, printed, minion, status));
        Assert.NotNull(Statuses.Inflict(world, printed, minion, status));
        Assert.True(Statuses.Afflicted(world, printed, minion, status));
        // And no third.
        Assert.Null(Statuses.Inflict(world, printed, minion, status));
    }

    [Rule("rr:steady")]
    [Fact]
    public void ASteadyCharacterLosesBothCardsWhenItsAttackIsCancelled()
    {
        // "After that character's attack, scheme, or thwart is canceled by a
        // status card effect, remove **all** status cards of the corresponding
        // type" -- which is `rr:stun-stunned.1`'s "remove **each**", and the
        // opposite of `rr:tough.2.1`'s one at a time.
        var printed = Cards().With("hero", ("ATK", "2"), ("HP", "10"), ("Steady", "1"));
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Statuses.Give(world, hero, Statuses.Stunned);
        Statuses.Give(world, hero, Statuses.Stunned);
        BasicPowerInitiation.BasicAttack(world, printed, 0, villain, []);
        Agendas.Finish(world, printed);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(0, Statuses.Count(world, hero, Statuses.Stunned));
    }

    [Rule("rr:ally.3")]
    [Fact]
    public void AStunnedAllyTakesNoConsequentialDamage()
    {
        // `rr:ally.3`'s parenthesis: "if an ally attempts to attack or thwart
        // while stunned or confused, respectively, that ally will **not** take
        // consequential damage."
        var printed = Cards().With("ally", ("HP", "4"), ("ATK", "2"), ("AtkIcons", "1"));
        var world = Board(printed);
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Statuses.Give(world, ally, Statuses.Stunned);
        AllyBasicPowers.AllyPower(world, printed, ally, world.TheCardIn(DeckType.VillainArea)!, BasicPowers.AttackVerb, []);
        Assert.False(ally.Ready);
        Assert.Equal(0, ally.Damage);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:piercing")]
    [Rule("rr:piercing.1")]
    [Fact]
    public void PiercingDiscardsToughBeforeTheDamageLands()
    {
        // "Before this attack deals damage to a character, discard each tough
        // status card from that character." So the damage lands rather than
        // being eaten -- which is the whole point, and the opposite of what
        // `rr:tough.2` does on its own.
        var printed = Cards().With("minion", ("HP", "9"));
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Statuses.Give(world, minion, Statuses.Tough);
        Grant(world, hero, Marvel.Rules.Timing.Keywords.Piercing);
        BasicPowerInitiation.BasicAttack(world, printed, 0, minion, []);
        Agendas.Finish(world, printed);
        Assert.False(Statuses.Has(world, minion, Statuses.Tough));
        Assert.Equal(2, minion.Damage);
    }

    [Rule("rr:piercing.2")]
    [Fact]
    public void PiercingDiscardsNothingWhenTheAttackDealsNoDamage()
    {
        // "If an attack with the piercing keyword would deal no damage to the
        // attacked character, it does not discard tough status cards."
        var printed = Cards().With("hero", ("ATK", "0"), ("HP", "10")).With("minion", ("HP", "9"));
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Statuses.Give(world, minion, Statuses.Tough);
        Grant(world, hero, Marvel.Rules.Timing.Keywords.Piercing);
        BasicPowerInitiation.BasicAttack(world, printed, 0, minion, []);
        Agendas.Finish(world, printed);
        Assert.True(Statuses.Has(world, minion, Statuses.Tough));
    }

    [Rule("rr:piercing.1")]
    [Fact]
    public void PiercingDiscardsEveryToughCard()
    {
        // "Discard **each** tough status card from that character" -- all of
        // them, which is the opposite of `rr:tough.2.1`'s one at a time. Two
        // cards would otherwise eat two attacks.
        var printed = Cards().With("minion", ("HP", "9"));
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Statuses.Give(world, minion, Statuses.Tough);
        Statuses.Give(world, minion, Statuses.Tough);
        Grant(world, hero, Marvel.Rules.Timing.Keywords.Piercing);
        BasicPowerInitiation.BasicAttack(world, printed, 0, minion, []);
        Agendas.Finish(world, printed);
        Assert.Equal(0, Statuses.Count(world, minion, Statuses.Tough));
        Assert.Equal(2, minion.Damage);
    }

    [Rule("rr:overkill")]
    [Fact]
    public void AnAttackWithoutOverkillSpillsNothing()
    {
        // The excess simply goes away. Six damage against two hit points and
        // the villain is untouched.
        var printed = Cards().With("hero", ("ATK", "6"), ("HP", "10")).With("minion", ("HP", "2"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        BasicPowerInitiation.BasicAttack(world, printed, 0, minion, []);
        Agendas.Finish(world, printed);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(0, villain.Damage);
    }

    [Rule("rr:ranged")]
    [Fact]
    public void RangedIgnoresRetaliate()
    {
        var printed = Cards().With("minion", ("HP", "9"), ("Retaliate", "3"));
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Grant(world, hero, Marvel.Rules.Timing.Keywords.Ranged);
        BasicPowerInitiation.BasicAttack(world, printed, 0, minion, []);
        Agendas.Finish(world, printed);
        Assert.Equal(2, minion.Damage);
        Assert.Equal(0, hero.Damage);
    }

    [Rule("rr:overkill")]
    [Rule("rr:overkill.1")]
    [Rule("rr:overkill.2")]
    [Rule("rr:overkill.3")]
    [Rule("rr:excess-damage")]
    [Fact]
    public void OverkillCarriesTheExcessFromADefeatedMinionToTheVillain()
    {
        // "Excess damage is any amount of damage [...] beyond that character's
        // remaining hit points." If overkill defeats a minion, that excess is
        // dealt to the villain: six against two is four beyond.
        var printed = Cards().With("hero", ("ATK", "6"), ("HP", "10")).With("villain", ("Retaliate", "2")).With("minion", ("HP", "2"));
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Grant(world, hero, Marvel.Rules.Timing.Keywords.Overkill);
        Agendas.Happening(world);
        var result = DamageAttacks.Attack(world, printed, hero, minion, 6, "test", "Attack", []);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(4, villain.Damage);
        Assert.Equal(0, hero.Damage);
        Assert.Equal(4, result.Excess);
    }

    [Rule("rr:overkill.1")]
    [Rule("rr:overkill.2")]
    [Fact]
    public void OverkillTreatsAFacedownPlayerCardAsTheDroneThatWasAttacked()
    {
        // A facedown Drone is a minion while it is attacked. Its defeat turns
        // the underlying ally faceup in its owner's discard pile, but excess
        // damage still goes to the villain rather than that ally's controller.
        var printed = Cards().With("hero", ("ATK", "4"), ("HP", "10")).With("villain", ("HP", "10")).With("underlying-ally", ("HP", "4"), ("Kind", "Ally"));
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.CreateCard("underlying-ally", world.Seats[0].Deck);
        var drone = Assert.IsType<Card>(FacedownDrones.EngageTop(world, 0, "test", "Create_Drone", []));
        Grant(world, hero, Marvel.Rules.Timing.Keywords.Overkill);
        Agendas.Happening(world);
        DamageAttacks.Attack(world, printed, hero, drone, 4, "test", "Attack", []);
        Assert.Equal(DeckType.DiscardPile, drone.Area.Type);
        Assert.Equal(3, villain.Damage);
        Assert.Equal(0, hero.Damage);
    }

    [Rule("rr:overkill.1")]
    [Fact]
    public void OverkillCarriesTheExcessFromADefeatedAllyToItsController()
    {
        // The other destination: "deal any damage on that ally beyond its hit
        // points to **the identity of the player who controls the ally**".
        var printed = Cards().With("villain", ("ATK", "7"), ("SCH", "2"), ("HP", "20")).With("ally", ("HP", "3"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("encounter", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));
        Grant(world, villain, Marvel.Rules.Timing.Keywords.Overkill);
        Attack.Initiate(world, printed, new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0), []);
        var asked = Sequence.Work(world, printed, new NoCardAbilities(), []);
        Sequence.Answer(world, printed, new NoCardAbilities(), asked!, Decision.Take(ally.ObjectId), []);
        Sequence.Finish(world, printed, new NoCardAbilities(), []);
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(4, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:overkill.1")]
    [Rule("rr:ownership-and-control.5")]
    [Fact]
    public void OverkillUsesAnAllysControllerRatherThanItsOwner()
    {
        // Overkill names "the identity of the player who controls the ally".
        // A card put into another player's play area is controlled there even
        // though defeat sends it to its owner's discard pile. The move caused
        // by defeat must not change the destination already named by overkill.
        var printed = Cards().With("villain", ("ATK", "7"), ("SCH", "2"), ("HP", "20")).With("ally", ("HP", "3"));
        var world = Board(printed, players: 2);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 0));
        world.CreateCard("encounter", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));
        Grant(world, villain, Marvel.Rules.Timing.Keywords.Overkill);
        Attack.Initiate(world, printed, new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 1), []);
        var asked = Sequence.Work(world, printed, new NoCardAbilities(), []);
        Sequence.Answer(world, printed, new NoCardAbilities(), asked!, Decision.Take(ally.ObjectId), []);
        // The ally's controller becomes the target player; ownership only
        // decides which discard pile receives the ally after defeat.
        Assert.Equal(1, world.Attack!.Player);
        Sequence.Finish(world, printed, new NoCardAbilities(), []);
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(4, world.Seats[1].IdentityCard.Damage);
    }

    [Rule("rr:overkill.4")]
    [Fact]
    public void OverkillCarriesNothingWhenAToughCardAteTheDamage()
    {
        // "If excess damage from an attack with overkill is prevented, that
        // damage is **not** dealt to the identity or villain." A tough status
        // card prevents all of it (`rr:tough.2`), so nothing spills.
        var printed = Cards().With("hero", ("ATK", "6"), ("HP", "10")).With("minion", ("HP", "2"));
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Statuses.Give(world, minion, Statuses.Tough);
        Grant(world, hero, Marvel.Rules.Timing.Keywords.Overkill);
        BasicPowerInitiation.BasicAttack(world, printed, 0, minion, []);
        Agendas.Finish(world, printed);
        Assert.Equal(0, minion.Damage);
        Assert.Equal(0, villain.Damage);
    }

    [Rule("rr:damage.3.2")]
    [Rule("rr:overkill.3")]
    [Rule("rr:overkill.4")]
    [Fact]
    public void PreventedExcessIsExcludedFromOverkillAndItsReportedValue()
    {
        // Six is still the damage dealt, but preventing three means the
        // two-hit-point minion takes three. Only one taken point is excess, so
        // overkill deals one and reports that same value to card abilities.
        var printed = Cards().With("minion", ("HP", "2"));
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Abilities = new PreventThree();
        Grant(world, hero, Marvel.Rules.Timing.Keywords.Overkill);
        Agendas.Happening(world);
        var result = DamageAttacks.Attack(world, printed, hero, minion, 6, "test", "Attack", []);
        Assert.Equal(6, result.Dealt);
        Assert.Equal(3, result.Taken);
        Assert.Equal(1, result.Excess);
        Assert.Equal(1, villain.Damage);
    }
}
