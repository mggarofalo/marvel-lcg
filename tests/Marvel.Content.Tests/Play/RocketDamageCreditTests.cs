using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Which damage Schadenfreude (16032) counts: "Until the end of the turn, heal 2 damage
// from Rocket Raccoon each time you deal any amount of damage to an enemy."
public sealed class RocketDamageCreditTests : CoreCardTestBase
{
    private const string WeaponsRunner = "01121";

    [Fact]
    [Rule("rr:damage.step.1")]
    public void SchadenfreudeDoesNotHealWhenUltronCannotTakeTheDamage()
    {
        // Ultron (01136): "Ultron cannot take damage while a [[Drone]] minion is in play."
        // Rocket Launcher (16037) deals 2 to the villain and to each minion engaged with
        // the chosen player. Ultron is dealt none of it, so only the Weapons Runner
        // engaged with Rocket counts: one heal of 2. The drone is engaged with the second
        // player, and Rocket's player is chosen.
        var world = RocketBoards.Board(players: 2, villain: "01136");
        var rocket = RocketBoards.Rocket(world);
        rocket.TakeDamage(4);
        var drone = GrootBoards.Engaged(world, "01143", player: 1);
        var minion = GrootBoards.Engaged(world, WeaponsRunner);
        var ultron = GrootBoards.Villain(world);
        var launcher = RocketBoards.Upgrade(world, "16037", charge: 2);
        Schadenfreude(world);

        GrootBoards.Act(world, launcher, []);
        GrootBoards.Drive(world, prompt => prompt.Cancellable ? null : GrootBoards.Take(rocket)(prompt));

        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(0, drone.Damage);
        Assert.Equal(0, ultron.Damage);
        Assert.Equal(2, rocket.Damage);
    }

    [Fact]
    [Rule("rr:you-your.12")]
    public void SchadenfreudeDoesNotHealForDamageRocketsCardDealsToAFriendlyCharacter()
    {
        // Battle Fury (06018): "After your hero attacks and defeats a minion, deal 1
        // damage to your hero and discard Battle Fury → ready your hero." Rocket's
        // basic attack defeats a Weapons Runner with 1 hit point left: one heal of 2 for
        // the enemy, none for the 1 Rocket deals himself.
        var world = RocketBoards.Board();
        var rocket = RocketBoards.Rocket(world);
        rocket.TakeDamage(4);
        var fury = world.CreateCard("06018", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var minion = GrootBoards.Engaged(world, WeaponsRunner);
        minion.TakeDamage(1);
        Schadenfreude(world);
        var offered = new List<int>();

        RocketBoards.BasicAttack(world, minion);
        var labels = new List<string>();
        GrootBoards.Drive(world, prompt =>
        {
            labels.Add(prompt.Label);
            return GrootBoards.Take(fury, offered)(prompt);
        });

        Assert.Single(offered);
        // The attack's response window names its conditions; with no excess dealt, the
        // excess-damage condition is not among them.
        Assert.DoesNotContain(labels, label => label.Contains(ExcessDamageTiming.ExcessDamageDealt, StringComparison.Ordinal));
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.True(rocket.Ready);
        Assert.Equal(3, rocket.Damage);
    }

    [Fact]
    [Rule("rr:you-your.12")]
    public void SchadenfreudeDoesNotHealForAnUpgradeAnotherPlayerControls()
    {
        // Electrostatic Armor (10031) plays "under any player's control": Rocket's player
        // owns this one, Spider-Man controls it. "After you defend against an attack,
        // deal 1 damage to the attacking character" — Spider-Man deals it, not Rocket.
        var world = RocketBoards.Board(players: 2);
        var rocket = RocketBoards.Rocket(world);
        rocket.TakeDamage(4);
        var spider = world.Seats[1].IdentityCard;
        var armor = world.CreateCard("10031", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 0));
        var minion = GrootBoards.Engaged(world, "01101", player: 1);
        Schadenfreude(world);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation { DealBoostCards = false }, 1, 2,
            Subject: minion.ObjectId, Seat: 1));
        var offered = new List<int>();

        GrootBoards.Drive(world, prompt => prompt.Asking == Question.Defender && prompt.Player == 1
            ? Decision.Take(spider.ObjectId) : GrootBoards.Take(armor, offered)(prompt));

        Assert.Equal(0, armor.Owner);
        Assert.Single(offered);
        Assert.Equal(1, minion.Damage);
        Assert.Equal(4, rocket.Damage);
    }

    private static void Schadenfreude(World world)
    {
        var card = GrootBoards.Hand(world, "16032");
        GrootBoards.Play(world, card, [GrootBoards.Hand(world, RocketBoards.Strength)]);
        GrootBoards.Drive(world, _ => null);
    }
}
