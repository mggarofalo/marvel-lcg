using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production rows for Rocket Raccoon's Aggression and Basic cards on synthetic boards.
public sealed class RocketPlayerCardTests : CoreCardTestBase
{
    private const string WeaponsRunner = "01121";

    [Theory]
    [InlineData(1, 1)]
    [InlineData(0, 0)]
    [Rule("rr:basic-power")]
    public void BugHealsOneAfterRocketsBasicAttack(int damage, int offers)
    {
        // 16040: "Hero Response: After your hero makes a basic attack, heal 1 damage from
        // Bug." Undamaged, the response changes nothing and is not offered.
        var world = RocketBoards.Board();
        var bug = GrootBoards.InPlay(world, "16040", DeckType.AlliesArea);
        bug.TakeDamage(damage);
        var offered = new List<int>();

        RocketBoards.BasicAttack(world, GrootBoards.Villain(world));
        GrootBoards.Drive(world, GrootBoards.Take(bug, offered));

        Assert.Equal(offers, offered.Count);
        Assert.Equal(0, bug.Damage);
        Assert.Equal(1, GrootBoards.Villain(world).Damage);
    }

    [Fact]
    [Rule("rr:overkill.3")]
    [Rule("rr:excess-damage")]
    public void FollowThroughAddsOneToTheExcessAnAttackDeals()
    {
        // 16045: "Hero Interrupt: When your hero's attack deals any amount of excess
        // damage, increase that amount by 1." Particle Cannon's 4 against a 2-hit-point
        // Weapons Runner has 2 excess; increased to 3, overkill deals 3 to Rhino.
        var world = RocketBoards.Board();
        var cannon = RocketBoards.Upgrade(world, "16036", charge: 2);
        var follow = RocketBoards.Upgrade(world, "16045");
        var minion = GrootBoards.Engaged(world, WeaponsRunner);
        var offered = new List<int>();

        GrootBoards.Act(world, cannon, []);
        GrootBoards.Drive(world, prompt => GrootBoards.Take(minion)(prompt) ?? GrootBoards.Take(follow, offered)(prompt));

        Assert.Single(offered);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(3, GrootBoards.Villain(world).Damage);
    }

    [Fact]
    [Rule("rr:overkill.3")]
    public void FollowThroughRaisesTheExcessIntoTheFrayCounts()
    {
        // 16042: "Deal 6 damage to a minion. For each point of excess damage dealt by
        // this attack, remove 1 threat from the main scheme." 6 against 2 hit points is 4
        // excess; Follow Through makes it 5.
        var world = RocketBoards.Board();
        var follow = RocketBoards.Upgrade(world, "16045");
        var minion = GrootBoards.Engaged(world, WeaponsRunner);
        var main = GrootBoards.MainScheme(world);
        main.PlaceTokens("k_threat", 8);
        var fray = GrootBoards.Hand(world, "16042");
        var paying = new[] { GrootBoards.Hand(world, RocketBoards.Strength), GrootBoards.Hand(world, RocketBoards.Energy) };

        GrootBoards.Act(world, fray, paying);
        GrootBoards.Drive(world, prompt => GrootBoards.Take(minion)(prompt) ?? GrootBoards.Take(follow)(prompt));

        Assert.Equal(3, main.Tokens["k_threat"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:you-your.15")]
    [Rule("rr:excess-damage")]
    public void FollowThroughIgnoresAnAllysAttackAndAnAttackWithoutExcess(bool ally)
    {
        // "Your hero's attack": the Rocket Raccoon ally's overkill attack is the ally's
        // (rr:you-your.15). The pistol's 2 against Rhino deals no excess.
        var world = RocketBoards.Board();
        var follow = RocketBoards.Upgrade(world, "16045");
        var offered = new List<int>();
        if (ally)
        {
            var rocket = GrootBoards.InPlay(world, "16019", DeckType.AlliesArea);
            var minion = GrootBoards.Engaged(world, WeaponsRunner);
            AllyBasicPowers.AllyPower(world, Cards, rocket, minion, BasicPowers.AttackVerb, []);
            GrootBoards.Drive(world, prompt => GrootBoards.Take(rocket)(prompt) ?? GrootBoards.Take(follow, offered)(prompt));
            Assert.Equal(2, GrootBoards.Villain(world).Damage);
        }
        else
        {
            var pistol = RocketBoards.Upgrade(world, "16038", charge: 1);
            GrootBoards.Act(world, pistol, []);
            GrootBoards.Drive(world, prompt => GrootBoards.Take(GrootBoards.Villain(world))(prompt)
                ?? GrootBoards.Take(follow, offered)(prompt));
            Assert.Equal(2, GrootBoards.Villain(world).Damage);
        }

        Assert.Empty(offered);
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(1, true)]
    [Rule("rr:excess-damage")]
    public void FollowThroughNeedsDamageBeyondTheRemainingHitPoints(int remaining, bool offers)
    {
        // The pistol's 2 against exactly 2 remaining hit points deals no excess;
        // against 1 it deals 1.
        var world = RocketBoards.Board();
        var follow = RocketBoards.Upgrade(world, "16045");
        var pistol = RocketBoards.Upgrade(world, "16038", charge: 1);
        var minion = GrootBoards.Engaged(world, WeaponsRunner);
        minion.TakeDamage(2 - remaining);
        var offered = new List<int>();

        GrootBoards.Act(world, pistol, []);
        GrootBoards.Drive(world, prompt => GrootBoards.Take(minion)(prompt) ?? GrootBoards.Take(follow, offered)(prompt));

        Assert.Equal(offers ? 1 : 0, offered.Count);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
    }

    [Fact]
    [Rule("rr:attack-player-ability-type.1")]
    public void FollowThroughIgnoresExcessFromDamageThatIsNotAnAttack()
    {
        // Rocket Launcher deals 2 to a minion with 1 remaining hit point, but not as an
        // attack, so "your hero's attack" does not describe it.
        var world = RocketBoards.Board();
        var rocket = RocketBoards.Rocket(world);
        var follow = RocketBoards.Upgrade(world, "16045");
        var launcher = RocketBoards.Upgrade(world, "16037", charge: 1);
        GrootBoards.Engaged(world, WeaponsRunner).TakeDamage(1);
        var offered = new List<int>();

        GrootBoards.Act(world, launcher, []);
        GrootBoards.Drive(world, prompt => GrootBoards.Take(follow, offered)(prompt) ?? GrootBoards.Take(rocket)(prompt));

        Assert.Empty(offered);
        Assert.Equal(2, GrootBoards.Villain(world).Damage);
    }

    [Fact]
    [Rule("rr:interrupt")]
    public void DecliningFollowThroughLeavesTheExcessUnchanged()
    {
        // Passing the opportunity spends it for that damage; overkill carries the
        // original 2.
        var world = RocketBoards.Board();
        var cannon = RocketBoards.Upgrade(world, "16036", charge: 2);
        RocketBoards.Upgrade(world, "16045");
        var minion = GrootBoards.Engaged(world, WeaponsRunner);

        GrootBoards.Act(world, cannon, []);
        GrootBoards.Drive(world, GrootBoards.Take(minion));

        Assert.Equal(2, GrootBoards.Villain(world).Damage);
    }

    [Theory]
    [InlineData(3, 2, DeckType.UpgradesArea)]
    [InlineData(1, 0, DeckType.DiscardPile)]
    [Rule("rr:uses-x-type")]
    [Rule("rr:overkill")]
    public void HandCannonAddsTwoAttackAndOverkillToABasicAttack(int charge, long left, DeckType area)
    {
        // 16046: "Uses (3 charge counters). Hero Interrupt: When your hero makes a basic
        // attack, exhaust Hand Cannon and remove 1 charge counter from it → your hero gets
        // +2 ATK for that attack. That attack gains overkill." ATK 3 against a 2-hit-point
        // Weapons Runner carries 1 to Rhino. rr:uses-x-type discards the card with its
        // last counter.
        var world = RocketBoards.Board();
        var cannon = RocketBoards.Upgrade(world, "16046", charge);
        var minion = GrootBoards.Engaged(world, WeaponsRunner);

        RocketBoards.BasicAttack(world, minion);
        GrootBoards.Drive(world, GrootBoards.Take(cannon));

        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(1, GrootBoards.Villain(world).Damage);
        Assert.Equal(left, RocketBoards.Charge(cannon));
        Assert.Equal(area, cannon.Area.Type);
    }

    [Fact]
    [Rule("rr:defend-defense")]
    public void TheGrootAllyHealsTwoAfterHeDefends()
    {
        // 16047: "Response: After Groot defends against an attack, heal 2 damage from him."
        var world = RocketBoards.Board();
        var groot = GrootBoards.InPlay(world, "16047", DeckType.AlliesArea);
        KangTemporalFixture.Attack(world, GrootBoards.Villain(world), 0);

        GrootBoards.Drive(world, prompt =>
            prompt.Affordances.FirstOrDefault(a => a.Verb == "Defense" && a.AnchorId == groot.ObjectId) is { } defense
                ? Decision.Take(defense.Id)
                : GrootBoards.Take(groot)(prompt));

        Assert.Equal(0, groot.Damage);
        Assert.False(groot.Ready);
    }

    [Fact]
    [Rule("rr:defend-defense")]
    public void TheGrootAllyDoesNotHealWhenAnotherCharacterDefends()
    {
        var world = RocketBoards.Board();
        var rocket = RocketBoards.Rocket(world);
        var groot = GrootBoards.InPlay(world, "16047", DeckType.AlliesArea);
        groot.TakeDamage(2);
        var offered = new List<int>();
        KangTemporalFixture.Attack(world, GrootBoards.Villain(world), 0);

        GrootBoards.Drive(world, prompt =>
            prompt.Affordances.FirstOrDefault(a => a.Verb == "Defense" && a.AnchorId == rocket.ObjectId) is { } defense
                ? Decision.Take(defense.Id)
                : GrootBoards.Take(groot, offered)(prompt));

        Assert.Empty(offered);
        Assert.Equal(2, groot.Damage);
    }

    [Fact]
    [Rule("rr:choose-option.2")]
    public void FloraAndFaunaOffersOnlyTheRocketOptionWhenGrootCannotChange()
    {
        // At 10 growth counters with Groot ready the Groot option changes nothing and
        // cannot be chosen (rr:choose-option.2).
        var world = GrootBoards.Board(players: 2, others: RocketBoards.Identity);
        GrootBoards.Grow(GrootBoards.Groot(world), 10);
        var pistol = RocketBoards.Upgrade(world, "16038", player: 1);
        var card = GrootBoards.Hand(world, "16020");
        var options = new List<int>();

        GrootBoards.Play(world, card, [GrootBoards.Hand(world, RocketBoards.Energy)]);
        GrootBoards.Drive(world, prompt =>
        {
            if (prompt.Affordances.All(a => a.Verb == "Choose_Option" && a.AnchorId == card.ObjectId))
                options.Add(prompt.Affordances.Count);
            return GrootBoards.Take(pistol)(prompt) ?? Decision.Take(prompt.Affordances[0].Id);
        });

        Assert.Equal([1], options);
        Assert.Equal(2, RocketBoards.Charge(pistol));
    }

    [Fact]
    [Rule("rr:team-up")]
    [Rule("rr:choose-option.2")]
    public void FloraAndFaunaChargesAndReadiesARocketUpgrade()
    {
        // 16048: "Team-Up (Groot and Rocket Raccoon). ... or place 2 charge counters on a
        // Rocket Raccoon upgrade and ready that upgrade." The Groot ally completes the
        // team-up with Rocket Raccoon.
        var world = RocketBoards.Board();
        GrootBoards.InPlay(world, "16047", DeckType.AlliesArea);
        var pistol = RocketBoards.Upgrade(world, "16038", charge: 1);
        pistol.Exhaust();
        var card = GrootBoards.Hand(world, "16048");

        GrootBoards.Play(world, card, [GrootBoards.Hand(world, RocketBoards.Energy)]);
        GrootBoards.Drive(world, prompt => RocketOption(prompt) ?? GrootBoards.Take(pistol)(prompt));

        Assert.Equal(3, RocketBoards.Charge(pistol));
        Assert.True(pistol.Ready);
    }

    [Fact]
    [Rule("rr:team-up")]
    public void GrootsFloraAndFaunaChargesRocketRaccoonsUpgradeInAnotherPlayArea()
    {
        // 16020 in Groot's deck: a Rocket Raccoon upgrade is one in any player's play
        // area, here Rocket Raccoon's in the second seat.
        var world = GrootBoards.Board(players: 2, others: RocketBoards.Identity);
        var launcher = RocketBoards.Upgrade(world, "16037", player: 1);
        var card = GrootBoards.Hand(world, "16020");

        GrootBoards.Play(world, card, [GrootBoards.Hand(world, RocketBoards.Energy)]);
        GrootBoards.Drive(world, prompt => RocketOption(prompt) ?? GrootBoards.Take(launcher)(prompt));

        Assert.Equal(2, RocketBoards.Charge(launcher));
        Assert.Equal(0, GrootBoards.Growth(GrootBoards.Groot(world)));
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, 1)]
    [Rule("rr:prevent.1")]
    public void BoosterBootsPreventOneAttackDamageForTheTopCardOfTheDeck(bool defend, long taken)
    {
        // 16052: "Hero Interrupt: When you would take any amount of damage from an attack,
        // exhaust Booster Boots and discard the top card of your deck → prevent 1 of that
        // damage." Rhino's 2 is 1 after Rocket's DEF 1.
        var world = RocketBoards.Board();
        var rocket = RocketBoards.Rocket(world);
        var boots = RocketBoards.Upgrade(world, "16052");
        KangTemporalFixture.Attack(world, GrootBoards.Villain(world), 0);

        GrootBoards.Drive(world, prompt =>
            prompt.Affordances.FirstOrDefault(a => a.Verb == "Defense" && a.AnchorId == rocket.ObjectId) is { } defense
                ? (defend ? Decision.Take(defense.Id) : null)
                : GrootBoards.Take(boots)(prompt));

        Assert.Equal(taken, rocket.Damage);
        Assert.False(boots.Ready);
        Assert.Equal(11, world.Seats[0].Deck.Cards.Count);
    }

    [Fact]
    [Rule("rr:cost")]
    [Rule("rr:encounter-deck.2")]
    public void LookingForTroubleEngagesTheFirstMinionThenRemovesThreeThreat()
    {
        // 16043: "Discard cards from the top of the encounter deck until you discard a
        // minion. Put that minion into play engaged with you → remove 3 threat from the
        // main scheme." The minion is put into play, not revealed.
        var world = RocketBoards.Board();
        var encounter = world.AreaOf(DeckType.EncounterDeck);
        var minion = world.CreateCard(WeaponsRunner, encounter);
        world.CreateCard("01104", encounter);
        world.CreateCard("01104", encounter);
        var main = GrootBoards.MainScheme(world);
        main.PlaceTokens("k_threat", 5);
        var card = GrootBoards.Hand(world, "16043");

        GrootBoards.Act(world, card, []);
        GrootBoards.Drive(world, GrootBoards.Take(main));

        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.Equal(2, main.Tokens["k_threat"]);
        Assert.Equal(12, encounter.Cards.Count);
        Assert.Equal(2, world.AreaOf(DeckType.EncounterDiscardPile).Cards.Count);
    }

    private static Decision? RocketOption(Prompt prompt) =>
        prompt.Affordances.FirstOrDefault(a => a.Description?.Contains("charge counters", StringComparison.Ordinal) == true
            && a.Verb == "Choose_Option" && a.AnchorId == prompt.Affordances[0].AnchorId && prompt.Affordances.Count > 1) is { } option
            ? Decision.Take(option.Id) : null;
}
