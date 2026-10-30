using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// The actual Weakened obligation and the full authored book on a nonpublic
// Rhino board; Kang's public admission stays closed. Player 1, Spider-Man, is
// not the first player and holds the obligation throughout.
public sealed class KangWeakenedTests : CoreCardTestBase
{
    private const string Weakened = "11018";

    [Theory]
    [InlineData(1, 1)]
    [InlineData(0, 0)]
    [Rule("rr:basic-power.1.1")]
    [Rule("rr:obligation.1")]
    public void WeakenedDamagesItsHolderAfterTheirHerosBasicAttack(int attacker, int damage)
    {
        // 11018: "Forced Response: After you use a basic hero power, take 1 damage."
        // rr:obligation.1: "you" is "only ... the player whose play area the
        // obligation is in". rr:basic-power.1.1: attack power "can be used by a
        // character to perform a basic attack". Player 0's attack does not count.
        var (world, f) = Board();

        BasicPowers.BasicAttack(world, Cards, attacker, f.Villain, []);
        f.Finish();

        Assert.Equal(2, f.Villain.Damage);
        Assert.Equal(damage, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(0, 0)]
    [Rule("rr:basic-power.1.2")]
    public void WeakenedDamagesItsHolderAfterTheirHerosBasicThwart(int thwarter, int damage)
    {
        // rr:basic-power.1.2: thwart power "can be used by a character to perform a
        // basic thwart".
        var (world, f) = Board();
        f.Main.PlaceTokens("k_threat", 5);

        BasicThwartPowers.BasicThwart(world, Cards, thwarter, f.Main, []);
        f.Finish();

        Assert.True(f.Main.Tokens["k_threat"] < 5);
        Assert.Equal(damage, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(1, 0 + 1, 0)]
    [InlineData(0, 0, 2 - 1)]
    [InlineData(-1, 2, 0)]
    [Rule("rr:defend-defense.2")]
    [Rule("rr:defend-defense.7.2")]
    public void WeakenedDamagesItsHolderAfterAnAttackTheirHeroBasicDefended(int defender, int holder, int other)
    {
        // rr:defend-defense.7.2: "If an ability triggers after a character uses a
        // basic power, that ability triggers after an attack in which a character
        // made a basic defense resolves." Rhino (ATK 2) attacks player 1. Spider-Man
        // (DEF 3) defends; rr:defend-defense.2: "The amount of damage dealt by the
        // attack is reduced by the hero's DEF value", so no attack damage, then
        // Weakened's 1. Captain Marvel
        // (DEF 1) defending for player 1 is not player 1's hero; undefended, player 1
        // takes the attack's 2 and nothing more.
        var (world, f) = Board();
        KangTemporalFixture.Attack(world, f.Villain, 1);

        KangTemporalFixture.Drive(world, f.Runner, prompt => defender >= 0 && prompt.Player == defender
            && prompt.Affordances.SingleOrDefault(a => a.Verb == "Defense") is { } defense
            ? Decision.Take(defense.Id) : null);

        Assert.Equal(holder, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(other, world.Seats[0].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:defend-defense.5")]
    public void WeakenedFiresWhenItsHolderBasicDefendsAnAttackOnTheOtherPlayer()
    {
        // rr:defend-defense.5: "If a player defends against an enemy attack that
        // targets a different player ... the defending player becomes the new target
        // of that attack." Rhino (ATK 2) attacks player 0; player 1's Spider-Man
        // (DEF 3) basic defends, takes no attack damage, then Weakened's 1.
        var (world, f) = Board();
        KangTemporalFixture.Attack(world, f.Villain, 0);

        KangTemporalFixture.Drive(world, f.Runner, prompt => prompt.Player == 1
            && prompt.Affordances.SingleOrDefault(a => a.Verb == "Defense") is { } defense
            ? Decision.Take(defense.Id) : null);

        Assert.Equal(1, world.Seats[1].IdentityCard.Damage);
        Assert.False(world.Seats[1].IdentityCard.Ready);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:stun-stunned.1")]
    public void WeakenedIgnoresABasicAttackReplacedByStun()
    {
        // rr:stun-stunned.1: "When this character would attack, remove each stunned
        // status card from it instead." Ruling 983ae8870c226cd4: "Stunned and confused status cards replace the
        // usage of the basic power with their removal." Player 1's stunned
        // Spider-Man attacks: the stun is removed, Rhino takes nothing, and no basic
        // power was used.
        var (world, f) = Board();
        var identity = world.Seats[1].IdentityCard;
        Statuses.Give(world, identity, Statuses.Stunned);

        BasicPowers.BasicAttack(world, Cards, 1, f.Villain, []);
        f.Finish();

        Assert.False(Statuses.Has(world, identity, Statuses.Stunned));
        Assert.Equal(0, f.Villain.Damage);
        Assert.Equal(0, identity.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:defend-defense.4")]
    [Rule("rr:defend-defense.4.3")]
    public void WeakenedIgnoresADefenseLabeledAbility()
    {
        // rr:defend-defense.4: a defense-labeled ability makes the identity "the
        // defender", but rr:defend-defense.4.3: "Resolving a defense-labeled ability
        // is not a basic defense". Player 1 plays Backflip (cost 0) and prevents all
        // of Rhino's damage; Weakened does not respond.
        var (world, f) = Board();
        var backflip = world.CreateCard("01003", world.Seats[1].Hand);
        KangTemporalFixture.Attack(world, f.Villain, 1);

        KangTemporalFixture.Drive(world, f.Runner, prompt => prompt.Affordances
            .SingleOrDefault(a => a.AnchorId == backflip.ObjectId) is { } play ? Decision.Take(play.Id) : null);

        Assert.Equal(DeckType.DiscardPile, backflip.Area.Type);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.True(world.Seats[1].IdentityCard.Ready);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:you-your.15")]
    public void WeakenedIgnoresAnAllysBasicAttack()
    {
        // rr:you-your.15: attacks "that resolve from allies in play under a player's
        // control are not considered to be performed by that player's identity."
        var (world, f) = Board();
        var ally = world.CreateCard("01067", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));

        AllyBasicPowers.AllyPower(world, Cards, ally, f.Villain, BasicPowers.AttackVerb, []);
        f.Finish();

        Assert.Equal(1, f.Villain.Damage);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:attack-player-ability-type.1")]
    public void WeakenedIgnoresAnAttackFromACardAbility()
    {
        // rr:attack-player-ability-type.1: "A hero or ally can use their basic
        // attack power to attack an enemy." Uppercut (01054) is an attack event, not
        // that power. Player 1 pays its 3 with Genius and Energy and deals 5 damage;
        // Weakened does not respond.
        var (world, f) = Board();
        var hand = world.Seats[1].Hand;
        var uppercut = world.CreateCard("01054", hand);
        var genius = world.CreateCard("01089", hand);
        var energy = world.CreateCard("01088", hand);
        var action = Assert.Single(f.Runner.Actions(world, 1), ability => ability.Card == uppercut.ObjectId);

        f.Runner.ExecuteAction(world, new PlayerAction(action, [genius.ObjectId, energy.ObjectId], []));
        f.Finish(prompt => Decision.Take(f.Villain.ObjectId));

        Assert.Equal(5, f.Villain.Damage);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:obligation.6")]
    [Rule("rr:wild-resource.3")]
    public void WeakenedAlterEgoActionDiscardsAChosenPhysicalResourceAndTheObligation()
    {
        // 11018: "Alter-Ego Action: Discard a [physical] resource from your hand →
        // discard this obligation." rr:wild-resource.3: "When resources are not
        // being generated for a cost, a wild resource does not have any
        // characteristic other than 'wild resource'" (ruling 437680ffd6d92fb8), so
        // Vibranium is not offered; Energy prints no physical icon.
        var (world, f) = Board(alterEgo: true);
        var weakened = world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)).Cards[0];
        var hand = world.Seats[1].Hand;
        var strength = world.CreateCard("01090", hand);
        world.CreateCard("01044", hand);
        world.CreateCard("01088", hand);
        var action = Assert.Single(f.Runner.Actions(world, 1), ability => ability.Card == weakened.ObjectId);
        Assert.Equal([strength.ObjectId], f.Runner.Describe(world, action).Targets!.Legal);

        f.Runner.ExecuteAction(world, new PlayerAction(action, [], [strength.ObjectId]));
        f.Finish();

        Assert.Equal(DeckType.DiscardPile, strength.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, weakened.Area.Type);
        Assert.Equal(2, hand.Cards.Count);
        Assert.Empty(world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)).Cards);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [Rule("rr:obligation.6")]
    public void WeakenedAlterEgoActionNeedsAlterEgoFormAndAPhysicalResource(bool alterEgo, bool physical)
    {
        // A hero cannot use an Alter-Ego Action, and without a card printing a
        // physical resource the cost cannot be paid. rr:obligation.6: "Only the
        // player with the obligation in their play area can trigger abilities or pay
        // costs on that obligation", so player 0 never may.
        var (world, f) = Board(alterEgo);
        var weakened = world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)).Cards[0];
        world.CreateCard(physical ? "01090" : "01044", world.Seats[1].Hand);
        world.CreateCard("01090", world.Seats[0].Hand);

        Assert.DoesNotContain(f.Runner.Actions(world, 1), ability => ability.Card == weakened.ObjectId);
        Assert.DoesNotContain(f.Runner.Actions(world, 0), ability => ability.Card == weakened.ObjectId);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void WeakenedRejectsANonPhysicalCardWithoutPaying()
    {
        // rr:initiating-abilities.step.5: if the cost cannot be paid, "abort this
        // process without paying any costs."
        var (world, f) = Board(alterEgo: true);
        var weakened = world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)).Cards[0];
        world.CreateCard("01090", world.Seats[1].Hand);
        var vibranium = world.CreateCard("01044", world.Seats[1].Hand);
        var action = Assert.Single(f.Runner.Actions(world, 1), ability => ability.Card == weakened.ObjectId);
        var before = world.Digest().Canonical();

        Assert.Throws<RulesNotImplementedException>(() =>
            f.Runner.ExecuteAction(world, new PlayerAction(action, [], [vibranium.ObjectId])));

        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(DeckType.ObligationsArea, weakened.Area.Type);
    }

    private static (World World, RedSkullFixture Fixture) Board(bool alterEgo = false)
    {
        var (world, f) = KangTemporalFixture.Board(players: 2, encounter: 2);
        world.CreateCard(Weakened, world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)));
        if (alterEgo) world.Seats[1].IdentityCard.TurnTo("01001b");
        return (world, f);
    }
}
