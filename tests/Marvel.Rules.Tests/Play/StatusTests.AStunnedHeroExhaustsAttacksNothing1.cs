using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public sealed class StatusAStunnedHeroExhaustsAttacksNothingTests : StatusTestBase
{
    [Rule("rr:stun-stunned.1")]
    [Rule("rr:stun-stunned.5")]
    [Fact]
    public void AStunnedHeroExhaustsAttacksNothingAndLosesTheStun()
    {
        // "**Forced Interrupt**: when this character would attack, remove each
        // stunned status card from it instead", and `.5`: "costs associated
        // with the attack attempt, **including exhausting the character**, must
        // still be paid."
        var printed = Cards();
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Statuses.Give(world, hero, Statuses.Stunned);
        BasicPowerInitiation.BasicAttack(world, printed, 0, villain, []);
        Agendas.Finish(world, printed);
        Assert.False(hero.Ready);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(0, Statuses.Count(world, hero, Statuses.Stunned));
    }

    [Rule("rr:attack-player-ability-type.1.1")]
    [Rule("rr:stun-stunned.5.1")]
    [Fact]
    public void AStunnedCharacterCanAttackWithNoLegalTarget()
    {
        // "A character can only initiate a basic attack if there is an enemy
        // that can be attacked **or if that character is stunned**", and
        // `.5.1` says the same from the other side. Attacking nothing is how
        // the stun comes off.
        var printed = Cards().With("minion", ("HP", "3"), ("Guard", "1"));
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        // A guarding minion makes the villain an illegal target.
        world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.DoesNotContain(BasicPowers.Attackable(world, printed, 0), card => card.ObjectId == villain.ObjectId);
        Statuses.Give(world, hero, Statuses.Stunned);
        BasicPowerInitiation.BasicAttack(world, printed, 0, villain, []);
        Agendas.Finish(world, printed);
        Assert.Equal(0, villain.Damage);
        Assert.Equal(0, Statuses.Count(world, hero, Statuses.Stunned));
    }

    [Rule("rr:confuse-confused.1")]
    [Rule("rr:confuse-confused.5")]
    [Fact]
    public void AConfusedHeroThwartsNothingAndLosesTheConfusion()
    {
        // "Discard the confused card instead. Costs associated with the thwart
        // attempt, including exhausting the character, must still be paid."
        var printed = Cards();
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 5);
        Statuses.Give(world, hero, Statuses.Confused);
        BasicThwartPowers.BasicThwart(world, printed, 0, scheme, []);
        Assert.False(hero.Ready);
        Assert.Equal(5, scheme.Tokens["k_threat"]);
        Assert.Equal(0, Statuses.Count(world, hero, Statuses.Confused));
    }

    [Rule("rr:confuse-confused.5.1")]
    [Fact]
    public void AConfusedHeroMayAttemptAThwartWhenNoSchemeIsValid()
    {
        // "A confused character can still attempt to thwart or use a thwart
        // ability even if there is no valid target." The only scheme has no
        // threat, so the attempt exists solely to remove Confused.
        var printed = Cards();
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        Statuses.Give(world, hero, Statuses.Confused);
        Assert.Empty(BasicPowers.Thwartable(world, printed, 0));
        BasicThwartPowers.BasicThwart(world, printed, 0, scheme, []);
        Assert.False(hero.Ready);
        Assert.False(Statuses.Has(world, hero, Statuses.Confused));
        Assert.Equal(0, scheme.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:stun-stunned.7")]
    [Fact]
    public void AStunCancelledAttemptIsNotAnAttack()
    {
        // "If an attack is canceled or replaced by an effect, the character
        // is not considered to have attacked." No attack begins or finishes,
        // so after-attack facts cannot be observed later in the window.
        var printed = Cards();
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Statuses.Give(world, hero, Statuses.Stunned);
        BasicPowerInitiation.BasicAttack(world, printed, 0, villain, []);
        Assert.Null(world.Attack);
        Assert.Null(world.FinishedAttack);
        Assert.Equal(0, villain.Damage);
    }

    [Rule("rr:stalwart.2")]
    [Fact]
    public void AConstantThatGrantsStalwartRemovesExistingAfflictions()
    {
        // "If a character gains the stalwart keyword while they have a
        // stunned and/or confused status card, each [...] card is removed."
        // The source enters after both cards, so this is gaining Stalwart and
        // not merely refusing a later status placement.
        var printed = Cards();
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        Statuses.Give(world, hero, Statuses.Stunned);
        Statuses.Give(world, hero, Statuses.Confused);
        var source = world.CreateCard("source", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var abilities = new GrantsStalwart(source.ObjectId, hero.ObjectId);
        world.Abilities = abilities;
        Reveal.EnterPlay(world, printed, source, [], abilities: abilities);
        Assert.False(Statuses.Has(world, hero, Statuses.Stunned));
        Assert.False(Statuses.Has(world, hero, Statuses.Confused));
    }

    [Rule("rr:hit-points.2.3")]
    [Rule("rr:hit-points.3.1")]
    [Fact]
    public void EndingAHitPointGrantDefeatsANowLethalMinion()
    {
        // A +3 hit-point effect raises the minion's pool from three to six.
        // When it ceases, "if the amount of damage on the card is equal to or
        // greater than the card's hit points, that card is defeated."
        var printed = Cards().With("minion", ("HP", "3"));
        var world = Board(printed);
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Effects.Register(new Marvel.Rules.Timing.ContinuousEffect(Marvel.Rules.Timing.EffectSource.LastingEffect, "health", Amount: 3, Affects: minion.ObjectId, Lasts: Marvel.Rules.Timing.Duration.UntilEndOf(Marvel.Rules.Timing.TimingPoints.EndOfRound)));
        minion.TakeDamage(3);
        Assert.Equal(3, DamagePlacement.Health(world, printed, minion) - minion.Damage);
        Agendas.Happening(world);
        world.Effects.Expire(Marvel.Rules.Timing.TimingPoints.EndOfRound, []);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
    }

    [Rule("rr:hit-points.2.1")]
    [Rule("rr:hit-points.2.3")]
    [Fact]
    public void EndingAHitPointGrantEliminatesANowLethalIdentity()
    {
        // An identity's dial falls with the modifier. Ten damage is survivable
        // at thirteen HP and becomes zero remaining HP when +3 ends.
        var printed = Cards();
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        world.Effects.Register(new Marvel.Rules.Timing.ContinuousEffect(Marvel.Rules.Timing.EffectSource.LastingEffect, "health", Amount: 3, Affects: hero.ObjectId, Lasts: Marvel.Rules.Timing.Duration.UntilEndOf(Marvel.Rules.Timing.TimingPoints.EndOfRound)));
        hero.TakeDamage(10);
        Agendas.Happening(world);
        world.Effects.Expire(Marvel.Rules.Timing.TimingPoints.EndOfRound, []);
        Assert.True(world.Seats[0].Eliminated);
    }

    [Rule("rr:hit-points.2.2")]
    [Rule("rr:hit-points.2.3")]
    [Fact]
    public void EndingAHitPointGrantDefeatsANowLethalVillainStage()
    {
        // The villain's dial follows the same rule. With no later stage, the
        // zero-HP stage is removed and its defeat ends the game for the players.
        var printed = Cards();
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Effects.Register(new Marvel.Rules.Timing.ContinuousEffect(Marvel.Rules.Timing.EffectSource.LastingEffect, "health", Amount: 3, Affects: villain.ObjectId, Lasts: Marvel.Rules.Timing.Duration.UntilEndOf(Marvel.Rules.Timing.TimingPoints.EndOfRound)));
        villain.TakeDamage(20);
        Agendas.Happening(world);
        world.Effects.Expire(Marvel.Rules.Timing.TimingPoints.EndOfRound, []);
        Assert.Equal(DeckType.RemovedArea, villain.Area.Type);
        Assert.Equal(Marvel.Rules.Play.Outcome.PlayersWin, world.Result);
    }

    [Rule("rr:stun-stunned.1")]
    [Fact]
    public void AStunReplacesACardAbilityAttackAfterItsCostsArePaid()
    {
        var printed = Cards();
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var eventCard = world.CreateCard("event", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        Statuses.Give(world, hero, Statuses.Stunned);
        BasicPowerInitiation.CardAttack(world, printed, 0, eventCard, villain, 8, "event", []);
        Agendas.Finish(world, printed);
        Assert.Equal(0, villain.Damage);
        Assert.True(hero.Ready);
        Assert.False(Statuses.Has(world, hero, Statuses.Stunned));
    }

    [Rule("rr:confuse-confused.1")]
    [Fact]
    public void AConfusionReplacesACardAbilityThwartAfterItsCostsArePaid()
    {
        var printed = Cards();
        var world = Board(printed);
        var hero = world.Seats[0].IdentityCard;
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        var eventCard = world.CreateCard("event", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        scheme.PlaceTokens("k_threat", 5);
        Statuses.Give(world, hero, Statuses.Confused);
        BasicThwartPowers.CardThwart(world, printed, 0, eventCard, scheme, 4, "event", []);
        Agendas.Finish(world, printed);
        Assert.Equal(5, scheme.Tokens["k_threat"]);
        Assert.True(hero.Ready);
        Assert.False(Statuses.Has(world, hero, Statuses.Confused));
    }

    [Rule("rr:stun-stunned.1")]
    [Rule("rr:stun-stunned.6")]
    [Fact]
    public void AStunnedEnemyDoesNotAttackAtAll()
    {
        // "If a stunned villain or minion would attack, discard the stunned
        // status card instead." So none of the attack's six steps happens, and
        // the boost card it would have been given stays on the encounter deck.
        var printed = Cards();
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));
        Statuses.Give(world, villain, Statuses.Stunned);
        Attack.Initiate(world, printed, new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0), []);
        Sequence.Finish(world, printed, new NoCardAbilities(), []);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Single(world.AreaOf(DeckType.EncounterDeck).Cards);
        Assert.Equal(0, Statuses.Count(world, villain, Statuses.Stunned));
    }

    [Rule("rr:confuse-confused.1")]
    [Rule("rr:confuse-confused.6")]
    [Fact]
    public void AConfusedEnemyPlacesNoThreat()
    {
        var printed = Cards();
        var world = Board(printed);
        // "If a confused villain or minion would scheme, discard the confused
        // status card instead." Alter-ego form makes the villain attempt that
        // scheme.
        world.Seats[0].IdentityCard.TurnTo("alterego");
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));
        Statuses.Give(world, villain, Statuses.Confused);
        VillainPhase.Schedule(world.Agenda, round: 1);
        Sequence.Finish(world, printed, new NoCardAbilities(), []);
        // The main scheme's own acceleration only, and the villain's SCH of 2
        // never lands.
        Assert.Equal(1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
        Assert.Equal(0, Statuses.Count(world, villain, Statuses.Confused));
    }

    [Rule("rr:stalwart")]
    [Rule("rr:stalwart.1")]
    [Rule("rr:confuse-confused.4")]
    [Rule("rr:stun-stunned.4")]
    [Fact]
    public void AStalwartCharacterCannotBeStunnedOrConfused()
    {
        // "If a character has an ability stating that it 'cannot be confused'"
        // or "cannot be stunned", that status cannot be placed. Stalwart is
        // exactly those two constant abilities. Tough is unaffected.
        var printed = Cards().With("minion", ("HP", "3"), ("Stalwart", "1"));
        var world = Board(printed);
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.Null(Statuses.Inflict(world, printed, minion, Statuses.Stunned));
        Assert.Null(Statuses.Inflict(world, printed, minion, Statuses.Confused));
        Assert.NotNull(Statuses.Inflict(world, printed, minion, Statuses.Tough));
    }

    [Rule("rr:status-cards.1")]
    [Fact]
    public void ACharacterCannotHoldTwoOfOneStatus()
    {
        // "A character cannot have more than one status card of each type at a
        // time."
        var printed = Cards().With("minion", ("HP", "3"));
        var world = Board(printed);
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.NotNull(Statuses.Inflict(world, printed, minion, Statuses.Stunned));
        Assert.Null(Statuses.Inflict(world, printed, minion, Statuses.Stunned));
    }
}
