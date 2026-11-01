using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production rows for Rocket Raccoon's signature cards on synthetic boards.
public sealed class RocketSignatureTests : CoreCardTestBase
{
    private const string WeaponsRunner = "01121";

    [Fact]
    [Rule("rr:basic-power")]
    [Rule("rr:lasting-effects")]
    public void IveGotAPlanReadiesRocketAfterABasicThwartAndAddsThwartForThePhase()
    {
        // 16030: "Hero Response: After you make a basic thwart (using your THW), ready
        // Rocket Raccoon. Rocket Raccoon gets +1 THW while in hero form until the end of
        // the phase."
        var world = RocketBoards.Board();
        var rocket = RocketBoards.Rocket(world);
        var main = GrootBoards.MainScheme(world);
        main.PlaceTokens("k_threat", 5);
        var plan = GrootBoards.Hand(world, "16030");
        var energy = GrootBoards.Hand(world, RocketBoards.Energy);

        BasicThwartPowers.BasicThwart(world, Cards, 0, main, []);
        GrootBoards.Drive(world, prompt =>
            prompt.Affordances.FirstOrDefault(a => a.AnchorId == plan.ObjectId) is { } offer
                ? new Decision(offer.Id, [], [energy.ObjectId]) : null);

        Assert.Equal(3, main.Tokens["k_threat"]);
        Assert.True(rocket.Ready);
        Assert.Equal(3, Modified(world, rocket, "thwart"));
        Assert.Equal(DeckType.DiscardPile, plan.Area.Type);
        world.Effects.Expire(TimingPoints.EndOfPhase);
        Assert.Equal(2, Modified(world, rocket, "thwart"));
    }

    [Fact]
    [Rule("rr:ready")]
    public void ReloadReadiesOnlyTechUpgrades()
    {
        // 16031: "Hero Action: Ready each [tech] upgrade you control." Follow Through is a
        // SKILL upgrade.
        var world = RocketBoards.Board();
        var cannon = RocketBoards.Upgrade(world, "16036", charge: 1);
        var pistol = RocketBoards.Upgrade(world, "16038", charge: 1);
        var follow = RocketBoards.Upgrade(world, "16045");
        foreach (var card in new[] { cannon, pistol, follow }) card.Exhaust();
        var reload = GrootBoards.Hand(world, "16031");

        GrootBoards.Play(world, reload, [GrootBoards.Hand(world, RocketBoards.Energy)]);
        GrootBoards.Drive(world, _ => null);

        Assert.True(cannon.Ready);
        Assert.True(pistol.Ready);
        Assert.False(follow.Ready);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.2")]
    public void ReloadIsNotOfferedWhenEveryTechUpgradeIsReady()
    {
        var world = RocketBoards.Board();
        RocketBoards.Upgrade(world, "16038", charge: 1);
        RocketBoards.Upgrade(world, "16045").Exhaust();
        var reload = GrootBoards.Hand(world, "16031");
        GrootBoards.Hand(world, RocketBoards.Energy);
        Assert.DoesNotContain(GrootBoards.Runner(world).Actions(world, 0), a => a.Card == reload.ObjectId);
    }

    [Fact]
    [Rule("rr:lasting-effects")]
    public void SchadenfreudeHealsTwoForEachEnemyRocketDamagesThisTurn()
    {
        // 16032: "Hero Action: Until the end of the turn, heal 2 damage from Rocket
        // Raccoon each time you deal any amount of damage to an enemy." The pistol damages
        // Rhino once; the launcher damages Rhino and a minion, two times.
        var world = RocketBoards.Board();
        var rocket = RocketBoards.Rocket(world);
        rocket.TakeDamage(7);
        var pistol = RocketBoards.Upgrade(world, "16038", charge: 3);
        var launcher = RocketBoards.Upgrade(world, "16037", charge: 2);
        GrootBoards.Engaged(world, WeaponsRunner);
        var card = GrootBoards.Hand(world, "16032");

        GrootBoards.Play(world, card, [GrootBoards.Hand(world, RocketBoards.Strength)]);
        GrootBoards.Drive(world, _ => null);
        GrootBoards.Act(world, pistol, []);
        GrootBoards.Drive(world, GrootBoards.Take(GrootBoards.Villain(world)));
        Assert.Equal(5, rocket.Damage);
        GrootBoards.Act(world, launcher, []);
        GrootBoards.Drive(world, prompt => prompt.Cancellable ? null : Decision.Take(prompt.Affordances[0].Id));
        Assert.Equal(1, rocket.Damage);

        world.Effects.Expire(TimingPoints.EndOfTurn);
        pistol.Refresh();
        GrootBoards.Act(world, pistol, []);
        GrootBoards.Drive(world, GrootBoards.Take(GrootBoards.Villain(world)));
        Assert.Equal(1, rocket.Damage);
        Assert.Equal(6, GrootBoards.Villain(world).Damage);
    }

    [Fact]
    [Rule("rr:you-your.15")]
    public void SchadenfreudeDoesNotHealForAnAllysDamage()
    {
        // rr:you-your.15: what allies do is "not considered to be performed by that player's identity".
        var world = RocketBoards.Board();
        var rocket = RocketBoards.Rocket(world);
        rocket.TakeDamage(4);
        var ally = GrootBoards.InPlay(world, "16047", DeckType.AlliesArea);
        var card = GrootBoards.Hand(world, "16032");
        GrootBoards.Play(world, card, [GrootBoards.Hand(world, RocketBoards.Strength)]);
        GrootBoards.Drive(world, _ => null);

        AllyBasicPowers.AllyPower(world, Cards, ally, GrootBoards.Villain(world), BasicPowers.AttackVerb, []);
        GrootBoards.Drive(world, _ => null);

        Assert.Equal(2, GrootBoards.Villain(world).Damage);
        Assert.Equal(4, rocket.Damage);
    }

    [Fact]
    [Rule("rr:resource-card.2")]
    public void SalvagePutsATechUpgradeFromTheDiscardPileOnTopOfTheDeckAfterItIsSpent()
    {
        // 16033: "Response: After you spend this card, put a [tech] upgrade from your
        // discard pile on top of your deck." Follow Through is not a [tech] upgrade.
        var world = RocketBoards.Board();
        var seat = world.Seats[0];
        var battery = world.CreateCard("16034", Discard(world));
        world.CreateCard("16045", Discard(world));
        var salvage = GrootBoards.Hand(world, "16033");
        var reload = GrootBoards.Hand(world, "16031");
        RocketBoards.Upgrade(world, "16038", charge: 1).Exhaust();

        GrootBoards.Act(world, reload, [salvage]);
        GrootBoards.Drive(world, prompt => GrootBoards.Take(salvage)(prompt) ?? GrootBoards.Take(battery)(prompt));

        Assert.Same(battery, seat.Deck.Cards[^1]);
        Assert.Equal(DeckType.DiscardPile, salvage.Area.Type);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.2")]
    public void SalvageIsNotOfferedWithoutATechUpgradeInTheDiscardPile()
    {
        var world = RocketBoards.Board();
        world.CreateCard("16045", Discard(world));
        var salvage = GrootBoards.Hand(world, "16033");
        var reload = GrootBoards.Hand(world, "16031");
        RocketBoards.Upgrade(world, "16038", charge: 1).Exhaust();
        var offered = new List<int>();

        GrootBoards.Act(world, reload, [salvage]);
        GrootBoards.Drive(world, GrootBoards.Take(salvage, offered));

        Assert.Empty(offered);
        Assert.Equal(12, world.Seats[0].Deck.Cards.Count);
    }

    [Fact]
    [Rule("rr:all-purpose-counter")]
    public void BatteryPackMovesAChargeCounterToAnotherTechUpgrade()
    {
        // 16034: "Action: Exhaust Battery Pack → move a charge counter from this card to
        // another [tech] upgrade you control."
        var world = RocketBoards.Board();
        var battery = RocketBoards.Upgrade(world, "16034", charge: 2);
        var pistol = RocketBoards.Upgrade(world, "16038");

        GrootBoards.Play(world, battery, []);
        GrootBoards.Drive(world, GrootBoards.Take(pistol));

        Assert.Equal(1, RocketBoards.Charge(battery));
        Assert.Equal(1, RocketBoards.Charge(pistol));
        Assert.False(battery.Ready);
    }

    [Theory]
    [InlineData(2, false)]
    [InlineData(0, true)]
    [Rule("rr:initiating-abilities.step.2")]
    public void BatteryPackNeedsACounterAndAnotherTechUpgrade(int charge, bool tech)
    {
        // Without a counter nothing moves; Follow Through is not a [tech] upgrade.
        var world = RocketBoards.Board();
        var battery = RocketBoards.Upgrade(world, "16034", charge);
        RocketBoards.Upgrade(world, tech ? "16038" : "16045");
        Assert.DoesNotContain(GrootBoards.Runner(world).Actions(world, 0), a => a.Card == battery.ObjectId);
    }

    [Theory]
    [InlineData("16029a", 12, 2)]
    [InlineData("16029b", 12, 0)]
    [Rule("rr:modifiers")]
    public void CyberneticSkeletonAddsHitPointsAndHeroAttack(string face, long health, long attack)
    {
        // 16035: "You get +3 hit points. While in hero form, Rocket Raccoon gets +1 ATK."
        var world = RocketBoards.Board(face);
        var rocket = RocketBoards.Rocket(world);
        RocketBoards.Upgrade(world, "16035");

        Assert.Equal(health, DamagePlacement.Health(world, Cards, rocket));
        Assert.Equal(attack, Modified(world, rocket, "attack"));
    }

    [Fact]
    [Rule("rr:ranged")]
    [Rule("rr:overkill")]
    public void ParticleCannonsRangedOverkillAttackIgnoresRetaliate()
    {
        // 16036: "deal 4 damage to an enemy. This attack gains overkill and ranged."
        // Whiplash (4 hit points, retaliate 1) is defeated with no excess; ranged ignores
        // retaliate (rr:ranged).
        var world = RocketBoards.Board();
        var rocket = RocketBoards.Rocket(world);
        var cannon = RocketBoards.Upgrade(world, "16036", charge: 2);
        var whiplash = GrootBoards.Engaged(world, "01172");

        GrootBoards.Act(world, cannon, []);
        GrootBoards.Drive(world, GrootBoards.Take(whiplash));

        Assert.Equal(DeckType.EncounterDiscardPile, whiplash.Area.Type);
        Assert.Equal(0, rocket.Damage);
        Assert.Equal(1, RocketBoards.Charge(cannon));
        Assert.False(cannon.Ready);
    }

    [Theory]
    [InlineData("16036")]
    [InlineData("16037")]
    [InlineData("16038")]
    [Rule("rr:cost.6")]
    public void AWeaponWithoutChargeCountersStaysInPlayAndOffersNothing(string face)
    {
        // Removing a charge counter is part of each weapon's cost; these weapons do not
        // print Uses, so they stay in play empty (ruling c0c1782ca297d006).
        var world = RocketBoards.Board();
        var weapon = RocketBoards.Upgrade(world, face);
        GrootBoards.Engaged(world, WeaponsRunner);
        Assert.DoesNotContain(GrootBoards.Runner(world).Actions(world, 0), a => a.Card == weapon.ObjectId);
        Assert.Equal(DeckType.UpgradesArea, weapon.Area.Type);
    }

    [Fact]
    [Rule("rr:retaliate-x")]
    public void RocketLauncherIsNotAnAttackAndHitsOnlyTheChosenPlayersMinions()
    {
        // 16037: "choose a player. Deal 2 damage to the villain and each minion engaged
        // with that player." It is not an attack, so Whiplash's retaliate does not answer
        // it; the minion engaged with the other player is untouched.
        var world = RocketBoards.Board(players: 2);
        var rocket = RocketBoards.Rocket(world);
        var launcher = RocketBoards.Upgrade(world, "16037", charge: 2);
        var whiplash = GrootBoards.Engaged(world, "01172");
        var theirs = GrootBoards.Engaged(world, WeaponsRunner, player: 1);

        GrootBoards.Act(world, launcher, []);
        GrootBoards.Drive(world, GrootBoards.Take(rocket));

        Assert.Equal(2, GrootBoards.Villain(world).Damage);
        Assert.Equal(2, whiplash.Damage);
        Assert.Equal(0, theirs.Damage);
        Assert.Equal(0, rocket.Damage);
        Assert.Equal(1, RocketBoards.Charge(launcher));
    }

    [Fact]
    [Rule("rr:attack-player-ability-type.1")]
    public void RocketsPistolDealsTwoAndSpendsACounter()
    {
        // 16038: "Exhaust Rocket's Pistol and remove 1 charge counter from it → deal 2
        // damage to an enemy."
        var world = RocketBoards.Board();
        var pistol = RocketBoards.Upgrade(world, "16038", charge: 3);

        GrootBoards.Act(world, pistol, []);
        GrootBoards.Drive(world, GrootBoards.Take(GrootBoards.Villain(world)));

        Assert.Equal(2, GrootBoards.Villain(world).Damage);
        Assert.Equal(2, RocketBoards.Charge(pistol));
        Assert.False(pistol.Ready);
    }

    [Theory]
    [InlineData("16029a", 3, true)]
    [InlineData("16029b", 0, false)]
    [Rule("rr:modifiers")]
    public void ThrusterBootsAddThwartAndAerialInHeroForm(string face, long thwart, bool aerial)
    {
        // 16039: "While in hero form, Rocket Raccoon gets +1 THW and gains the [aerial] trait."
        var world = RocketBoards.Board(face);
        var rocket = RocketBoards.Rocket(world);
        RocketBoards.Upgrade(world, "16039");

        Assert.Equal(thwart, Modified(world, rocket, "thwart"));
        Assert.Equal(aerial, Traits.Has(world, rocket, "AERIAL", Cards));
    }

    private static Area Discard(World world) => world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
}
