using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public sealed class KeywordADefeatedCharacterTests : KeywordTestBase
{
    [Rule("rr:retaliate-x.2")]
    [Fact]
    public void ADefeatedCharacterDoesNotRetaliate()
    {
        // "The character with retaliate X **must be in play after the attack
        // resolves** to deal this damage." An attack that defeats it kills the
        // retaliation with it.
        var printed = new Printed().With("hero", ("ATK", "9"), ("HP", "10")).With("minion", ("HP", "2"), ("Retaliate", "3"));
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        BasicPowerInitiation.BasicAttack(world, printed, 0, minion, []);
        Agendas.Finish(world, printed);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:patrol")]
    [Rule("rr:patrol.1")]
    [Rule("rr:target.3.9")]
    [Fact]
    public void PatrolStopsTheMainSchemeBeingThwartedButNotASideScheme()
    {
        // "The engaged player cannot thwart the **main scheme**." A side scheme
        // is still fair game, which is what separates patrol from guard.
        var printed = new Printed().With("minion", ("Patrol", "1"), ("HP", "3"));
        var world = Board(printed);
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
        var side = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        side.PlaceTokens("k_threat", 2);
        Assert.Equal(2, BasicPowers.Thwartable(world, printed, 0).Count);
        world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.Equal([side.ObjectId], BasicPowers.Thwartable(world, printed, 0).Select(scheme => scheme.ObjectId));
    }

    [Rule("rr:patrol")]
    [Fact]
    public void AMinionPatrollingSomebodyElseDoesNotStopYou()
    {
        // Engagement-specific, like guard.
        var printed = new Printed().With("minion", ("Patrol", "1"), ("HP", "3"));
        var world = Board(printed, players: 2);
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 5);
        world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        Assert.Single(BasicPowers.Thwartable(world, printed, 0));
        Assert.Empty(BasicPowers.Thwartable(world, printed, 1));
    }

    [Rule("rr:assault")]
    [Rule("rr:assault.1")]
    [Rule("rr:assault.3")]
    [Fact]
    public void AssaultMakesAThwartUseAttackInstead()
    {
        // "While a character is making a basic thwart against this scheme, that
        // character uses its **ATK instead of its THW**." ATK 4 against THW 1,
        // so the difference is three threat. Clause 3 adds that an ability
        // increasing the character's "basic power" can increase that ATK;
        // the live +2 modifier makes the assault thwart remove six.
        var printed = new Printed().With("hero", ("ATK", "4"), ("THW", "1")).With("sideScheme", ("Assault", "1"));
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var hero = world.Seats[0].IdentityCard;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: "attack", Amount: 2, Card: hero.ObjectId, Affects: hero.ObjectId));
        var side = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        side.PlaceTokens("k_threat", 9);
        BasicThwartPowers.BasicThwart(world, printed, 0, side, []);
        Agendas.Finish(world, printed);
        Assert.Equal(3, side.Tokens["k_threat"]);
    }

    [Rule("rr:retaliate-x")]
    [Rule("rr:attack-enemy-activation.4.1")]
    [Fact]
    public void AHeroWithRetaliateHitsBackAtAnAttackingEnemy()
    {
        // An undefended attack's "targeted character is considered to have been
        // attacked", so the hero's retaliate response fires. Retaliate is a
        // character rule, not a player one, and damages the attacking enemy.
        var printed = new Printed().With("hero", ("HP", "10"), ("Retaliate", "2")).With("villain", ("ATK", "3"), ("HP", "20")).With("boost", ("Boost", "0"));
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        KeepEncounterDeckLive(world);
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));
        Attack.Initiate(world, printed, new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0), []);
        Undefended(world, printed);
        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(2, villain.Damage);
    }

    [Rule("rr:assault.2")]
    [Fact]
    public void AnAllyThwartingAnAssaultSchemeTakesTheDamageUnderItsAttack()
    {
        // "If the thwarting character is an ally, it takes the consequential
        // damage listed under its **ATK instead of its THW** after the thwart."
        // One icon under ATK and none under THW, so the ally takes damage
        // thwarting a scheme it would normally walk away from.
        var printed = new Printed().With("ally", ("HP", "4"), ("ATK", "2"), ("THW", "2"), ("AtkIcons", "1")).With("sideScheme", ("Assault", "1"));
        var world = Board(printed);
        var ally = world.CreateCard("ally", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var side = world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        side.PlaceTokens("k_threat", 9);
        AllyBasicPowers.AllyPower(world, printed, ally, side, BasicPowers.ThwartVerb, []);
        Agendas.Finish(world, printed);
        // Thwarted for its ATK, and took the icon under it.
        Assert.Equal(7, side.Tokens["k_threat"]);
        Assert.Equal(1, ally.Damage);
    }

    [Rule("rr:attack-enemy-activation.step.1")]
    [Fact]
    public void AMinionWithoutVillainousTakesNoCardOffTheEncounterDeck()
    {
        // "**Skip this step.**" Skipping matters beyond the icons: taking a
        // card off the encounter deck moves every later deal, so a minion that
        // wrongly took one desynchronises the rest of the game.
        var printed = new Printed().With("hero", ("HP", "10")).With("minion", ("ATK", "1"), ("HP", "3"));
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));
        Attack.Initiate(world, printed, new PhaseStep(Steps.Attack, 1, 2, Subject: minion.ObjectId, Seat: 0), []);
        Undefended(world, printed);
        Assert.Single(world.AreaOf(DeckType.EncounterDeck).Cards);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:loses")]
    [Rule("rr:attack-enemy-activation.step.1")]
    [Fact]
    public void AMinionThatLosesVillainousTakesNoBoostCard()
    {
        // A card that "loses a characteristic" no longer has its printed
        // Villainous keyword. The activation therefore skips the boost-card
        // step exactly like a minion that never printed the keyword.
        var printed = new Printed().With("hero", ("HP", "10")).With("minion", ("ATK", "1"), ("HP", "3"), ("Villainous", "1"));
        var world = Board(printed);
        world.Seats[0].IdentityCard.TurnTo("hero");
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Characteristics.LossOf("villainous"), Affects: minion.ObjectId));
        Attack.Initiate(world, printed, new PhaseStep(Steps.Attack, 1, 2, Subject: minion.ObjectId, Seat: 0), []);
        Undefended(world, printed);
        Assert.Single(world.AreaOf(DeckType.EncounterDeck).Cards);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:scheme-enemy-activation.step.1")]
    [Fact]
    public void AMinionWithoutVillainousSchemesWithoutABoostCard()
    {
        // The same clause on the scheming side, and the same consequence.
        var printed = new Printed().With("minion", ("SCH", "2"), ("HP", "3")).With("villain", ("SCH", "0")).With("scheme", ("EscalationThreat", "0")).With("boost", ("Boost", "5"));
        var world = Board(printed);
        world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("boost", world.AreaOf(DeckType.EncounterDeck));
        VillainPhase.Schedule(world.Agenda, round: 1);
        Sequence.Finish(world, printed, new NoCardAbilities(), []);
        // The villain schemes for its 0 plus a boost card worth 5; the minion
        // schemes for its printed 2 and takes no boost card. **7, not 12** --
        // a minion that wrongly took one would double the boost.
        Assert.Equal(7, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
    }

    [Rule("rr:villainous")]
    [Rule("rr:villainous.1")]
    [Theory]
    // Villainous is equivalent to the Forced Interrupt "when this character
    // uses a basic power, give it a boost card." The activation rule says that
    // "if a villain, **or a minion with the
    // villainous keyword**, is attacking, give it one facedown boost card. (If
    // a minion without the villainous keyword is attacking, skip this step.)"
    [InlineData("villain", 0, true)]
    [InlineData("minion", 0, false)]
    [InlineData("minion", 1, true)]
    public void OnlyAVillainOrAVillainousMinionIsGivenABoostCard(string faceId, int villainous, bool boosted)
    {
        var printed = new Printed().With(faceId, ("Villainous", villainous.ToString()));
        var world = Board(printed);
        var enemy = world.CreateCard(faceId, world.AreaOf(DeckType.EngagedEnemiesArea));
        Assert.Equal(boosted, Marvel.Rules.Timing.Keywords.IsBoosted(world, enemy, printed, 1));
    }

    [Rule("rr:temporary")]
    [Rule("rr:temporary.1")]
    [Fact]
    public void ATemporaryCardIsDiscardedWhenTheRoundEnds()
    {
        // "**Forced Interrupt**: when the round ends, discard this card from
        // play." A card without the keyword beside it stays.
        var printed = new Printed().With("temp", ("Temporary", "1")).With("permanentish", ("HP", "3"));
        var world = Board(printed);
        var temporary = world.CreateCard("temp", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var staying = world.CreateCard("permanentish", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        // A deck with a card in it: an empty one beside an empty discard is
        // `rr:player-deck.4`, and the discarded card would go straight back in.
        world.CreateCard("permanentish", world.Seats[0].Deck);
        PhaseEnd.EndVillainPhase(world, printed, []);
        Assert.Equal(DeckType.DiscardPile, temporary.Area.Type);
        Assert.Equal(DeckType.SupportsArea, staying.Area.Type);
    }

    [Rule("rr:victory-x")]
    [Rule("rr:victory-x.2")]
    [Fact]
    public void ADefeatedCardWorthPointsGoesToTheVictoryDisplay()
    {
        // "A character or side scheme with the victory X keyword is placed in
        // the victory display when it is defeated" -- **instead of** its
        // owner's discard pile, not as well as it.
        var printed = new Printed().With("minion", ("HP", "1"), ("Victory", "2"));
        var world = Board(printed);
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var attachment = world.CreateCard("attachment", world.AreaOf(DeckType.UpgradesArea, minion.Area.PlayArea, minion.ObjectId));
        var events = new List<GameEvent>();
        Agendas.Happening(world);
        DamagePlacement.Deal(world, printed, minion, minion, 1, "test", "test", events);
        Assert.Equal(DeckType.VictoryDisplay, minion.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, attachment.Area.Type);
        Assert.Contains(events.OfType<CardDetached>(), detached => detached.Card == attachment.ObjectId && detached.Host == minion.ObjectId);
        Assert.DoesNotContain(world.AreaOf(DeckType.EncounterDiscardPile).Cards, card => card.ObjectId == minion.ObjectId);
    }

    [Rule("rr:victory-display")]
    [Rule("rr:victory-x.1")]
    [Rule("rr:victory-x.1.1")]
    [Rule("rr:victory-x.1.2")]
    [Rule("rr:victory-x.3")]
    [Rule("rr:victory-x.5")]
    [Fact]
    public void ADefeatedHostsVictoryAttachmentJoinsTheSharedPointTotal()
    {
        // A victory attachment goes to the shared out-of-play display when its
        // host is defeated; the host itself discards normally, and the printed
        // victory values in the display are the scenario's point total.
        var printed = new Printed().With("minion", ("HP", "1")).With("attachment", ("Victory", "2"));
        var world = Board(printed);
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var attachment = world.CreateCard("attachment", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), minion.ObjectId));
        Agendas.Happening(world);
        var events = new List<GameEvent>();
        DamagePlacement.Deal(world, printed, minion, minion, 1, "test", "test", events);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(DeckType.VictoryDisplay, attachment.Area.Type);
        Assert.False(DeckTypes.IsInPlay(attachment.Area.Type));
        Assert.Equal(2, VictoryDisplay.VictoryPoints(world, printed));
        Assert.Contains(events.OfType<CardsMoved>(), moved => moved.Verb == "Victory" && moved.Cards.Any(landing => landing.Card == attachment.ObjectId));
        Assert.Contains(events.OfType<CardsMoved>(), moved => moved.Verb == "Defeat" && moved.Cards.Any(landing => landing.Card == minion.ObjectId));
    }

    [Rule("rr:victory-x.1.2")]
    [Rule("rr:victory-x.3")]
    [Fact]
    public void ADefeatedHostsVictoryZeroAttachmentStillJoinsTheDisplay()
    {
        // Victory X names the destination even when X is zero; zero changes
        // the point value, not whether the keyword exists.
        var printed = new Printed().With("minion", ("HP", "1")).With("attachment", ("Victory", "0"));
        var world = Board(printed);
        var minion = world.CreateCard("minion", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var attachment = world.CreateCard("attachment", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), minion.ObjectId));
        Agendas.Happening(world);
        DamagePlacement.Deal(world, printed, minion, minion, 1, "test", "test", []);
        Assert.Equal(DeckType.VictoryDisplay, attachment.Area.Type);
        Assert.Equal(0, VictoryDisplay.VictoryPoints(world, printed));
    }
}
