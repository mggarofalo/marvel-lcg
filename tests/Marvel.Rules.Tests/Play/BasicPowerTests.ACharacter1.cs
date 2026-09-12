using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public sealed class BasicPowerACharacterTests : BasicPowerTestBase
{
    [Rule("rr:dash-value.2")]
    [Fact]
    public void ACharacterCannotExhaustToUseADashPower()
    {
        // A dash means the power cannot be used. It is not a modifiable zero:
        // the attempted attack is rejected before the hero exhausts.
        var printed = new Printed().With("hero", ("ATK", "–")).With("villain", ("HP", "10"));
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Assert.Throws<RulesNotImplementedException>(() => BasicPowers.BasicAttack(world, printed, 0, villain, []));
        Assert.True(hero.Ready);
        Assert.Equal(0, villain.Damage);
    }

    [Rule("rr:dash-value.3")]
    [Rule("rr:attachment.1.2")]
    [Rule("rr:modifiers.7")]
    [Fact]
    public void AReferencedDashIsAnUnmodifiableZero()
    {
        // "A value of a dash (–) cannot be modified."
        var printed = new Printed().With("hero", ("ATK", "–")).With("attachment", ("ATK+", "4"));
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        world.CreateCard("attachment", world.AreaOf(DeckType.UpgradesArea, hero.Area.PlayArea, hero.ObjectId, cardOwner: 0));
        Assert.Equal(0, StateFields.Modified(world, hero, "attack", printed, world.Players));
    }

    [Rule("rr:dash-value.3")]
    [Rule("rr:modifiers.7")]
    [Fact]
    public void AnOmittedDashPowerIsAnUnmodifiableZero()
    {
        // The generated dataset omits a basic-power field when the printed
        // card shows a dash. "A value of a dash (–) cannot be modified."
        var printed = new Printed();
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: "thwart", Amount: 4, Card: hero.ObjectId, Affects: hero.ObjectId));
        Assert.False(BasicPowers.CanUsePower(printed, hero, "THW"));
        Assert.Equal(0, StateFields.Modified(world, hero, "thwart", printed, world.Players));
    }

    [Rule("rr:modifiers.4")]
    [Fact]
    public void AModifiedValueCannotFallBelowZero()
    {
        // "After all active modifiers have been taken into account, if a
        // value is below zero, it is treated as zero."
        var printed = new Printed().With("hero", ("ATK", "1"));
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: "attack", Amount: -2, Card: hero.ObjectId, Affects: hero.ObjectId));
        Assert.Equal(0, StateFields.Modified(world, hero, "attack", printed, world.Players));
        var fields = StateFields.For(hero, printed, world.Players, inPlay: true, hasHeldPools: true, hasFirstPlayerToken: false, world);
        Assert.Equal(0, fields["attack"]);
    }

    [Fact]
    public void AHealthAdjustmentRemainsSignedUntilItJoinsPrintedHealth()
    {
        // Health is a derived total: the generic field reader supplies only
        // the signed adjustment, and Damage.Health adds the printed base.
        var printed = new Printed().With("hero", ("HP", "10"));
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: "health", Amount: -2, Card: hero.ObjectId, Affects: hero.ObjectId));
        Assert.Equal(-2, StateFields.Modified(world, hero, "health", printed, world.Players));
        Assert.Equal(8, Damage.Health(world, printed, hero));
    }

    [Rule("rr:star-icon.5")]
    [Fact]
    public void AnUndefinedStarPowerHasAValueOfZero()
    {
        // With no card-text definition for the star, the referenced power is
        // zero rather than an invented value.
        var printed = new Printed().With("hero", ("ATK", "*")).With("villain", ("HP", "10"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        BasicPowers.BasicAttack(world, printed, 0, villain, []);
        Agendas.Finish(world, printed);
        Assert.Equal(0, villain.Damage);
    }

    [Rule("rr:attack-player-ability-type.1")]
    [Rule("rr:target.2.1")]
    [Fact]
    public void ABasicAttackExhaustsAndDealsTheCharactersAttackValue()
    {
        // "A character **must exhaust** to use this power. This deals damage
        // equal to the character's ATK value to the enemy."
        var printed = new Printed().With("hero", ("ATK", "3")).With("villain", ("HP", "10"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var events = new List<GameEvent>();
        BasicPowers.BasicAttack(world, printed, 0, villain, events);
        Agendas.Finish(world, printed);
        Assert.False(world.Seats[0].IdentityCard.Ready);
        Assert.Equal(3, villain.Damage);
    }

    [Rule("rr:consequential-damage.2")]
    [Rule("rr:consequential-damage.2.1")]
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public void AnAllyPowerAbortsWithoutConsequentialDamageIfItsTargetLeaves(bool attacking, bool reenters)
    {
        // If the target leaves before the basic power applies, the ally still
        // paid its exhaust cost but is not considered to have attacked or
        // thwarted and takes no consequential damage. The same physical card
        // returning is a new copy and does not restore the chosen target.
        var printed = new Printed().With("ally", ("ATK", "2"), ("THW", "2"), ("HP", "3")).With("villain", ("HP", "10"));
        var world = Board(printed);
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var target = attacking ? world.TheCardIn(DeckType.VillainArea)! : world.TheCardIn(DeckType.MainSchemesArea)!;
        if (!attacking)
        {
            target.PlaceTokens("k_threat", 2);
        }

        AllyBasicPowers.AllyPower(world, printed, ally, target, attacking ? BasicPowers.AttackVerb : BasicPowers.ThwartVerb, []);
        var originalArea = target.Area;
        World.MoveToTop(target, world.AreaOf(DeckType.EncounterDiscardPile));
        if (reenters)
        {
            World.MoveToTop(target, originalArea);
            if (!attacking)
            {
                target.PlaceTokens("k_threat", 2);
            }
        }

        Agendas.Finish(world, printed);
        Assert.False(ally.Ready);
        Assert.Equal(0, ally.Damage);
        Assert.Equal(attacking ? 0 : 2, target.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, target.Damage);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Rule("rr:star-icon.4")]
    [Fact]
    public void AStarredIdentityPowerChecksItsTextWhenUsed()
    {
        // The star changes no number; it requires the engine to check the
        // identity's text at the timing point created by using the power.
        var printed = new Printed().With("hero", ("ATK", "3*")).With("villain", ("HP", "10"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var observer = new StarredPowerObserver(world.Seats[0].IdentityCard.ObjectId);
        BasicPowers.BasicAttack(world, printed, 0, villain, []);
        Agendas.Finish(world, printed, observer);
        Assert.True(observer.Checked);
        Assert.Equal(3, villain.Damage);
    }

    [Rule("rr:modifiers")]
    [Rule("rr:upgrade.2")]
    [Fact]
    public void ABasicAttackDealsTheModifiedAttackValue()
    {
        // "The character's ATK value", and `rr:modifiers` has the game
        // "constantly check and (if necessary) update the count of any variable
        // quantity that is being modified" -- so it is the value now, not the
        // one printed. An upgrade attached to the hero printing `ATK+ 2` is the
        // ordinary case, and 116 cards in the pool carry one.
        var printed = new Printed().With("hero", ("ATK", "3")).With("upgrade", ("ATK+", "2")).With("villain", ("HP", "10"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var identity = world.Seats[0].IdentityCard;
        world.CreateCard("upgrade", world.AreaOf(DeckType.UpgradesArea, identity.Area.PlayArea, identity.ObjectId, cardOwner: 0));
        BasicPowers.BasicAttack(world, printed, 0, villain, []);
        Agendas.Finish(world, printed);
        Assert.Equal(5, villain.Damage);
    }

    [Rule("rr:exhausted.2")]
    [Fact]
    public void AnExhaustedCharacterCannotUseABasicPower()
    {
        // "If an exhausted card must exhaust to pay the cost of using its
        // ability, that ability cannot be used until the card is ready."
        var printed = new Printed().With("hero", ("ATK", "3")).With("villain", ("HP", "10"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Seats[0].IdentityCard.Exhaust();
        var thrown = Assert.Throws<RulesNotImplementedException>(() => BasicPowers.BasicAttack(world, printed, 0, villain, []));
        Assert.Contains("is exhausted", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(0, villain.Damage);
    }

    [Rule("rr:attack-player-ability-type.1.2")]
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void PermissionMakesABasicAttackWithoutChangingReadiness(bool useAlly, bool exhausted)
    {
        // "Without exhausting" can be used on an exhausted hero or ally. A
        // ready character remains ready; an exhausted one remains exhausted.
        var printed = new Printed().With("hero", ("ATK", "3")).With("ally", ("ATK", "2"), ("HP", "3")).With("villain", ("HP", "10"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var character = useAlly ? world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0)) : world.Seats[0].IdentityCard;
        if (exhausted)
        {
            character.Exhaust();
        }

        BasicPowers.BasicAttackWithoutExhausting(world, printed, character, villain, []);
        Agendas.Finish(world, printed);
        Assert.Equal(!exhausted, character.Ready);
        Assert.Equal(useAlly ? 2 : 3, villain.Damage);
    }

    [Rule("rr:player-turn.3")]
    [Fact]
    public void AnAlterEgoCannotAttackAndAHeroCannotRecover()
    {
        // "Use their alter-ego's basic recovery *(if in alter-ego form)* or
        // their hero's basic attack or thwart power *(if in hero form)*."
        var printed = new Printed().With("hero", ("ATK", "3")).With("villain", ("HP", "10"));
        var world = Board(printed, hero: false);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Seats[0].IdentityCard.TakeDamage(2);
        Assert.Throws<RulesNotImplementedException>(() => BasicPowers.BasicAttack(world, printed, 0, villain, []));
        world.Seats[0].IdentityCard.TurnTo("hero");
        Assert.Throws<RulesNotImplementedException>(() => AllyBasicPowers.BasicRecovery(world, printed, 0, []));
    }

    [Rule("rr:guard.1")]
    [Rule("rr:target.3.8")]
    [Fact]
    public void AGuardingMinionTakesEveryVillainOffTheList()
    {
        // "The engaged player cannot attack any villain." So the villain leaves
        // the list and the minions stay -- including the one guarding.
        var printed = new Printed().With("hero", ("ATK", "3")).With("villain", ("HP", "10")).With("minion", ("HP", "3"), ("Guard", "1"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Assert.Equal([villain.ObjectId], Ids(BasicPowers.Attackable(world, printed, 0)));
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.Equal([minion.ObjectId], Ids(BasicPowers.Attackable(world, printed, 0)));
        var thrown = Assert.Throws<RulesNotImplementedException>(() => BasicPowers.BasicAttack(world, printed, 0, villain, []));
        Assert.Contains("is not an enemy", thrown.Message, StringComparison.Ordinal);
    }

    [Rule("rr:guard")]
    [Fact]
    public void AMinionGuardingSomebodyElseDoesNotStopYou()
    {
        // "**While a minion with the guard keyword is engaged with a player**,
        // **that** player cannot use cards they control to attack a villain."
        // Guard is engagement-specific, not a board-wide effect.
        var printed = new Printed().With("hero", ("ATK", "3")).With("villain", ("HP", "10")).With("minion", ("HP", "3"), ("Guard", "1"));
        var world = Board(printed, players: 2);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        Assert.Contains(villain.ObjectId, Ids(BasicPowers.Attackable(world, printed, 0)));
        Assert.DoesNotContain(villain.ObjectId, Ids(BasicPowers.Attackable(world, printed, 1)));
    }
}
