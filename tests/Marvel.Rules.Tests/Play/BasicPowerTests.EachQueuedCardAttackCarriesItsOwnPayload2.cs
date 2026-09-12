using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public sealed class BasicPowerEachQueuedCardAttackCarriesItsOwnPayloadTests : BasicPowerTestBase
{
    [Fact]
    public void EachQueuedCardAttackCarriesItsOwnPayload()
    {
        // The engine chooses to put the complete card attack on its agenda
        // step. A later nested attack may update the board's compatibility
        // snapshot, but it cannot rewrite an earlier occurrence.
        var printed = new Printed().With("hero", ("ATK", "3")).With("villain", ("HP", "10")).With("minion", ("HP", "10")).With("event");
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var source = world.CreateCard("event", world.Seats[0].Hand);
        var abilities = new RecordingCardPowers();
        world.Abilities = abilities;
        Assert.True(BasicPowerInitiation.CardAttack(world, printed, 0, source, villain, 5, "first", [], abilityIndex: 0));
        Assert.True(BasicPowerInitiation.CardAttack(world, printed, 0, source, minion, 7, "second", [], abilityIndex: 0));
        Agendas.Finish(world, printed, abilities);
        Assert.Equal([villain.ObjectId, minion.ObjectId], abilities.Targets);
        Assert.Equal([5, 7], abilities.Amounts);
    }

    [Rule("rr:thwart.1")]
    [Rule("rr:thwart.1.1")]
    [Rule("rr:target.2.1")]
    [Fact]
    public void ABasicThwartExhaustsAndRemovesThreatButNeedsSomeToRemove()
    {
        // "This removes threat equal to the character's THW value from the
        // scheme", and `.1.1`: "a character can only initiate a basic thwart if
        // there is a scheme with **at least one threat** for the character to
        // remove."
        var printed = new Printed().With("hero", ("THW", "2"));
        var world = Board(printed);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        Assert.Empty(BasicPowers.Thwartable(world, printed, 0));
        scheme.PlaceTokens("k_threat", 5);
        BasicThwartPowers.BasicThwart(world, printed, 0, scheme, []);
        Agendas.Finish(world, printed);
        Assert.False(world.Seats[0].IdentityCard.Ready);
        Assert.Equal(3, scheme.Tokens["k_threat"]);
    }

    [Rule("rr:target.3.2")]
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AZeroPowerCanBeUsedAgainstAnOtherwiseValidTarget(bool attack)
    {
        // “A character with an ATK, SCH, or THW of 0 can perform an
        // activation or basic power using that value against a target that is
        // otherwise valid.” Zero changes no token, but it does not invalidate
        // the target or prevent the power's exhaust cost.
        var printed = new Printed().With("hero", ("ATK", "0"), ("THW", "0")).With("villain", ("HP", "10"));
        var world = Board(printed);
        var target = attack ? world.TheCardIn(DeckType.VillainArea)! : world.TheCardIn(DeckType.MainSchemesArea)!;
        if (!attack)
        {
            target.PlaceTokens("k_threat", 1);
        }

        if (attack)
        {
            BasicPowerInitiation.BasicAttack(world, printed, 0, target, []);
        }
        else
        {
            BasicThwartPowers.BasicThwart(world, printed, 0, target, []);
        }

        Agendas.Finish(world, printed);
        Assert.False(world.Seats[0].IdentityCard.Ready);
        Assert.Equal(attack ? 0 : 1, attack ? target.Damage : target.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:threat")]
    [Fact]
    public void ThwartCannotTakeMoreThreatThanIsThere()
    {
        // Threat is tokens, and a scheme cannot hold a negative number of them.
        var printed = new Printed().With("hero", ("THW", "4"));
        var world = Board(printed);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 1);
        var events = new List<GameEvent>();
        BasicThwartPowers.BasicThwart(world, printed, 0, scheme, events);
        events.AddRange(Agendas.Finish(world, printed));
        Assert.Equal(0, scheme.Tokens["k_threat"]);
        // **And the event says so.** `Card.PlaceTokens` clamps at zero on its
        // own, so the board is right either way and only the wire is wrong: an
        // uncapped thwart reports the scheme going from 1 threat to -3, and a
        // client drawing from the event stream would believe it.
        var reported = events.OfType<FieldSet>().Single(set => set.Field == "k_threat");
        Assert.Equal(1, reported.From);
        Assert.Equal(0, reported.To);
    }

    [Rule("rr:cannot")]
    [Rule("rr:cannot.2")]
    [Rule("rr:thwart.1.1")]
    [Fact]
    public void ACharacterCannotInitiateAThwartAgainstAProtectedScheme()
    {
        // "Cannot" is absolute. A scheme whose threat cannot be removed is
        // not one with threat "for the character to remove", so it is absent
        // from the legal targets rather than offered as a no-op.
        var printed = new Printed().With("hero", ("THW", "2"));
        var world = Board(printed);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 3);
        world.Abilities = new ProtectedScheme(scheme.ObjectId);
        Assert.Empty(BasicPowers.Thwartable(world, printed, 0));
        Assert.Throws<RulesNotImplementedException>(() => BasicThwartPowers.BasicThwart(world, printed, 0, scheme, []));
        Assert.True(world.Seats[0].IdentityCard.Ready);
        Assert.Equal(3, scheme.Tokens["k_threat"]);
    }

    [Rule("rr:cannot")]
    [Fact]
    public void AProtectedSchemeAlsoRefusesThreatRemovalFromAnEffect()
    {
        // The prohibition is checked by the shared primitive, not only by the
        // basic-power affordance. A card effect therefore cannot walk around
        // the word "cannot" by calling the token mutation directly.
        var printed = new Printed();
        var world = Board(printed);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 3);
        var prohibition = world.CreateCard("upgrade", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var abilities = new ProtectedScheme(scheme.ObjectId, prohibition.ObjectId);
        world.Abilities = abilities;
        var events = new List<GameEvent>();
        long removed = Threat.Remove(world, printed, abilities, scheme, 2, "test", "Remove_Threat", events);
        Assert.Equal(0, removed);
        Assert.Equal(3, scheme.Tokens["k_threat"]);
        Assert.Empty(events);
    }

    [Rule("rr:cannot.3")]
    [Fact]
    public void ExplicitCardTextCanOverrideAThreatRemovalProhibition()
    {
        // "An ability can override a rule with the word 'cannot' in it if the
        // ability has an explicit exception to that rule." The override is an
        // explicit property of this instruction and names the exact card whose
        // prohibition it overrides; an ordinary permission names none.
        var printed = new Printed();
        var world = Board(printed);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 3);
        var prohibition = world.CreateCard("upgrade", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var abilities = new ProtectedScheme(scheme.ObjectId, prohibition.ObjectId);
        long removed = Threat.Remove(world, printed, abilities, scheme, 2, "test", "Remove_Threat", [], overridesCannotFrom: prohibition.ObjectId);
        Assert.Equal(2, removed);
        Assert.Equal(1, scheme.Tokens["k_threat"]);
    }

    [Rule("rr:cannot.1")]
    [Fact]
    public void ExplicitExceptionToOneProhibitionDoesNotOverrideAnother()
    {
        // "Cannot is absolute" for every prohibition the card text does not
        // explicitly override. Naming one source leaves the other source's
        // independent prohibition in force.
        var printed = new Printed();
        var world = Board(printed);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 3);
        var first = world.CreateCard("upgrade", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var second = world.CreateCard("upgrade", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var abilities = new ProtectedScheme(scheme.ObjectId, first.ObjectId, second.ObjectId);
        long removed = Threat.Remove(world, printed, abilities, scheme, 2, "test", "Remove_Threat", [], overridesCannotFrom: first.ObjectId);
        Assert.Equal(0, removed);
        Assert.Equal(3, scheme.Tokens["k_threat"]);
    }

    [Rule("rr:recover-recovery")]
    [Rule("rr:recover-recovery.1")]
    [Fact]
    public void ABasicRecoveryExhaustsAndHealsButNeedsDamageToHeal()
    {
        // "The player exhausts their alter-ego and heals a number of hit points
        // equal to their REC value", and `.1`: "an identity that has no damage
        // to heal cannot perform a basic recovery."
        var printed = new Printed().With("alterego", ("REC", "3"), ("HP", "10"));
        var world = Board(printed, hero: false);
        var identity = world.Seats[0].IdentityCard;
        Assert.False(BasicPowers.CanRecover(world, printed, 0));
        Assert.Throws<RulesNotImplementedException>(() => AllyBasicPowers.BasicRecovery(world, printed, 0, []));
        identity.TakeDamage(5);
        Assert.True(BasicPowers.CanRecover(world, printed, 0));
        AllyBasicPowers.BasicRecovery(world, printed, 0, []);
        Assert.False(identity.Ready);
        Assert.Equal(2, identity.Damage);
    }

    [Rule("rr:heal.1")]
    [Fact]
    public void HealingCannotGoPastFullHealth()
    {
        // "A heal effect can only bring a character to its maximum hit points."
        var printed = new Printed().With("alterego", ("REC", "9"), ("HP", "10"));
        var world = Board(printed, hero: false);
        var identity = world.Seats[0].IdentityCard;
        identity.TakeDamage(2);
        var events = new List<GameEvent>();
        AllyBasicPowers.BasicRecovery(world, printed, 0, events);
        Assert.Equal(0, identity.Damage);
        // Healed 2 of a possible 9, and the event says 8 -> 10 rather than
        // 8 -> 17. `Card.TakeDamage` clamps, so again the board is right either
        // way and the wire is what a missing cap would spoil.
        var reported = events.OfType<FieldSet>().Single(set => set.Field == "health");
        Assert.Equal(8, reported.From);
        Assert.Equal(10, reported.To);
        // And healing a character with nothing to heal reports nothing at all,
        // rather than a no-op field change.
        DamageRecovery.Heal(world, printed, identity, 5, "test", "test", events);
        Assert.Single(events.OfType<FieldSet>(), set => set.Field == "health");
    }

    [Rule("rr:heal.1")]
    [Fact]
    public void HealAnswersWithWhatItActuallyHealed()
    {
        // Not the amount asked for. `rr:heal.1` caps a heal at full health, so
        // a character damaged by one heals one however large the number on the
        // card -- and cards are written against the difference: "Rhino heals 4
        // damage. **If no damage was healed this way**, this card gains surge."
        //
        // The board stays right either way, because `Card.TakeDamage` clamps.
        // It is the answer that a card reads, and an unclamped answer would
        // make a card that healed nothing believe it had.
        var printed = new Printed().With("alterego", ("REC", "3"), ("HP", "10"));
        var world = Board(printed, hero: false);
        var identity = world.Seats[0].IdentityCard;
        Assert.Equal(0, DamageRecovery.Heal(world, printed, identity, 4, "test", "test", []));
        identity.TakeDamage(1);
        Assert.Equal(1, DamageRecovery.Heal(world, printed, identity, 4, "test", "test", []));
        identity.TakeDamage(6);
        Assert.Equal(4, DamageRecovery.Heal(world, printed, identity, 4, "test", "test", []));
        Assert.Equal(2, identity.Damage);
    }

    [Rule("rr:defeat")]
    [Rule("rr:defeat.1")]
    [Fact]
    public void ADefeatedMinionIsDiscarded()
    {
        // "If a character has zero or fewer remaining hit points [...] it is
        // defeated", and "if an ally, minion, or side scheme is defeated, it is
        // discarded". A minion belongs to the scenario, so its pile is the
        // encounter discard.
        var printed = new Printed().With("hero", ("ATK", "3")).With("minion", ("HP", "3"));
        var world = Board(printed);
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        BasicPowerInitiation.BasicAttack(world, printed, 0, minion, []);
        Agendas.Finish(world, printed);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
    }

    [Rule("rr:defeat")]
    [Fact]
    public void ExactlyZeroRemainingHitPointsIsADefeat()
    {
        // "**Zero or fewer** remaining hit points" -- not "fewer than zero".
        var printed = new Printed().With("hero", ("ATK", "3")).With("minion", ("HP", "3"));
        var world = Board(printed);
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Agendas.Happening(world);
        Assert.True(DamagePlacement.Deal(world, printed, minion, minion, 3, "test", "test", []));
    }

    [Rule("rr:villain-defeat")]
    [Rule("rr:villain-villain-deck")]
    [Rule("rr:hit-points.2.2")]
    [Fact]
    public void DefeatingAVillainStageRemovesItAndRevealsTheNext()
    {
        // The villain is "represented by a sequential deck of one or more
        // cards." If its hit point dial reaches zero, remove that stage and
        // reveal the next one.
        var printed = new Printed().With("hero", ("ATK", "9")).With("villain", ("HP", "5")).With("villain2", ("HP", "12"));
        var world = Board(printed);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var next = world.CreateCard("villain2", world.AreaOf(DeckType.VillainDeck));
        BasicPowerInitiation.BasicAttack(world, printed, 0, villain, []);
        Agendas.Finish(world, printed);
        Assert.Equal(DeckType.RemovedArea, villain.Area.Type);
        Assert.Equal(DeckType.VillainArea, next.Area.Type);
        Assert.True(next.FaceUp);
        Assert.Equal(Outcome.Unfinished, world.Result);
        // `rr:villain-defeat.2` -- "excess damage that is dealt to defeat a
        // villain stage does not carry over to the new stage." Nine damage
        // against five hit points and the new stage starts clean.
        Assert.Equal(0, next.Damage);
    }
}
