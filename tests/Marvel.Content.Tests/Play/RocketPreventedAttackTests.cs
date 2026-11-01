using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// An attack whose damage an attachment fully prevents still deals that damage.
// rr:prevent.1.3: "If all damage from an attack is prevented, the attacking character
// is considered to have dealt damage". Rulings 70423690d446327f and 05670207b5acc4d9
// measure excess damage on the damage dealt, so a prevented attack can deal excess.
public sealed class RocketPreventedAttackTests : CoreCardTestBase
{
    private const string TemporalShield = "11014";
    private const string PhotographicReflexes = "04104";
    private const string WeaponsRunner = "01121";

    [Theory]
    [InlineData(TemporalShield, 1)]
    [InlineData(PhotographicReflexes, 2)]
    [Rule("rr:prevent.1.3")]
    [Rule("rr:excess-damage")]
    public void APreventedPistolShotStillDealsExcessAndDamagesAnEnemy(string attachment, int recoil)
    {
        // Rocket's Pistol (16038) deals 2 to Rhino with 1 hit point left; the attachment
        // prevents all of it. Temporal Shield deals 1 to the attacker; Photographic
        // Reflexes deals the 2 to Rocket instead. "Murdered You!" (16029a) draws for the
        // 1 excess dealt, and Schadenfreude (16032) heals 2 for the damage dealt.
        var world = RocketBoards.Board();
        var rocket = RocketBoards.Rocket(world);
        rocket.TakeDamage(4);
        var rhino = GrootBoards.Villain(world);
        rhino.TakeDamage(Damage.Health(world, world.Facts, rhino) - 1);
        var attached = Attach(world, attachment, rhino);
        var pistol = RocketBoards.Upgrade(world, "16038", charge: 3);
        var schadenfreude = GrootBoards.Hand(world, "16032");
        GrootBoards.Play(world, schadenfreude, [GrootBoards.Hand(world, RocketBoards.Strength)]);
        GrootBoards.Drive(world, _ => null);
        var offered = new List<int>();

        GrootBoards.Act(world, pistol, []);
        GrootBoards.Drive(world, prompt => GrootBoards.Take(rhino)(prompt) ?? RocketIdentityTests.Respond(rocket, offered)(prompt));

        Assert.Equal(DeckType.EncounterDiscardPile, attached.Area.Type);
        Assert.Equal(Damage.Health(world, world.Facts, rhino) - 1, rhino.Damage);
        Assert.Equal(4 + recoil - 2, rocket.Damage);
        Assert.Single(offered);
        Assert.Single(world.Seats[0].Hand.Cards);
    }

    [Fact]
    [Rule("rr:prevent.1.3")]
    [Rule("rr:excess-damage")]
    public void FollowThroughIsOfferedWhenAPreventedAttackDealsExcess()
    {
        // 16045: "When your hero's attack deals any amount of excess damage, increase
        // that amount by 1." The prevented pistol shot still deals 1 excess to Rhino, so
        // the interrupt is offered; Temporal Shield prevents the increased 3 as well.
        var world = RocketBoards.Board();
        var rocket = RocketBoards.Rocket(world);
        var rhino = GrootBoards.Villain(world);
        rhino.TakeDamage(Damage.Health(world, world.Facts, rhino) - 1);
        Attach(world, TemporalShield, rhino);
        var follow = RocketBoards.Upgrade(world, "16045");
        var pistol = RocketBoards.Upgrade(world, "16038", charge: 3);
        var offered = new List<int>();

        GrootBoards.Act(world, pistol, []);
        GrootBoards.Drive(world, prompt => GrootBoards.Take(rhino)(prompt) ?? GrootBoards.Take(follow, offered)(prompt));

        Assert.Single(offered);
        Assert.Equal(Damage.Health(world, world.Facts, rhino) - 1, rhino.Damage);
        Assert.Equal(1, rocket.Damage);
    }

    [Fact]
    [Rule("rr:prevent.1.3")]
    [Rule("rr:overkill.3")]
    public void IntoTheFrayCountsTheExcessOfAPreventedAttack()
    {
        // 16042: "Deal 6 damage to a minion. For each point of excess damage dealt by
        // this attack, remove 1 threat from the main scheme." Temporal Shield prevents
        // all 6 against a 2-hit-point Weapons Runner; the 4 excess is still dealt.
        var world = RocketBoards.Board();
        var minion = GrootBoards.Engaged(world, WeaponsRunner);
        Attach(world, TemporalShield, minion);
        var main = GrootBoards.MainScheme(world);
        main.PlaceTokens("k_threat", 8);
        var fray = GrootBoards.Hand(world, "16042");
        var paying = new[] { GrootBoards.Hand(world, RocketBoards.Strength), GrootBoards.Hand(world, RocketBoards.Energy) };

        GrootBoards.Act(world, fray, paying);
        GrootBoards.Drive(world, GrootBoards.Take(minion));

        Assert.Equal(4, main.Tokens["k_threat"]);
        Assert.Equal(0, minion.Damage);
        Assert.NotEqual(DeckType.EncounterDiscardPile, minion.Area.Type);
    }

    [Fact]
    [Rule("rr:prevent.1.3")]
    [Rule("rr:damage.step.1")]
    public void TemporalShieldLeavesNoDamageForArmoredRhinoSuitToTake()
    {
        // Temporal Shield prevents all damage from the attack before any step-1
        // replacement sees it; Armored Rhino Suit (01098: "When any amount of damage
        // would be dealt to Rhino, place it here instead") has nothing to place. The
        // pistol's 2 still deals 1 excess to Rhino with 1 hit point left.
        var world = RocketBoards.Board();
        var rocket = RocketBoards.Rocket(world);
        var rhino = GrootBoards.Villain(world);
        rhino.TakeDamage(Damage.Health(world, world.Facts, rhino) - 1);
        var suit = Attach(world, "01098", rhino);
        Attach(world, TemporalShield, rhino);
        var pistol = RocketBoards.Upgrade(world, "16038", charge: 3);
        var offered = new List<int>();

        GrootBoards.Act(world, pistol, []);
        GrootBoards.Drive(world, prompt => GrootBoards.Take(rhino)(prompt) ?? RocketIdentityTests.Respond(rocket, offered)(prompt));

        Assert.Equal(0, suit.Damage);
        Assert.Equal(Damage.Health(world, world.Facts, rhino) - 1, rhino.Damage);
        Assert.Single(offered);
    }

    private static Card Attach(World world, string face, Card host) =>
        world.CreateCard(face, world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, host.ObjectId));
}
