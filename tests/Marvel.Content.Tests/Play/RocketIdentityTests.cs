using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production rows for Rocket Raccoon's identity on synthetic boards.
public sealed class RocketIdentityTests : CoreCardTestBase
{
    private const string WeaponsRunner = "01121";

    [Fact]
    [Rule("rr:excess-damage")]
    [Rule("rr:overkill")]
    public void MurderedYouDrawsAfterAnAttackDealsExcessDamageToAMinion()
    {
        // 16029a: "Response: After you deal excess damage to an enemy, draw 1 card."
        // Particle Cannon's 4 against a 2-hit-point Weapons Runner deals 2 excess
        // (rr:excess-damage), which overkill carries to Rhino (rr:overkill).
        var world = RocketBoards.Board();
        var rocket = RocketBoards.Rocket(world);
        var cannon = RocketBoards.Upgrade(world, "16036", charge: 2);
        var minion = GrootBoards.Engaged(world, WeaponsRunner);
        var offered = new List<int>();

        GrootBoards.Act(world, cannon, []);
        GrootBoards.Drive(world, prompt => GrootBoards.Take(minion)(prompt) ?? Respond(rocket, offered)(prompt));

        Assert.Single(offered);
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(2, GrootBoards.Villain(world).Damage);
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(1, 1)]
    [InlineData(0, 0)]
    [Rule("rr:excess-damage")]
    public void MurderedYouDrawsOncePerEnemyDealtExcessDamage(int wounded, int draws)
    {
        // Ruling 3f57fe6e7dc99dd2: when one effect deals excess damage to two enemies,
        // "Murdered You!" responds to each of them. Rocket Launcher deals 2 to Rhino and
        // to each minion engaged with the chosen player; only a minion already holding
        // 1 damage has 1 remaining hit point, so only it is dealt excess. It is not an
        // attack.
        var world = RocketBoards.Board();
        var rocket = RocketBoards.Rocket(world);
        var launcher = RocketBoards.Upgrade(world, "16037", charge: 2);
        var minions = new[] { GrootBoards.Engaged(world, WeaponsRunner), GrootBoards.Engaged(world, WeaponsRunner) };
        for (int i = 0; i < wounded; i++) minions[i].TakeDamage(1);
        var offered = new List<int>();

        GrootBoards.Act(world, launcher, []);
        GrootBoards.Drive(world, prompt => Respond(rocket, offered)(prompt) ?? FirstTarget(prompt));

        Assert.Equal(draws, offered.Count);
        Assert.Equal(draws, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(2, GrootBoards.Villain(world).Damage);
        Assert.All(minions, minion => Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type));
    }

    [Fact]
    [Rule("rr:prevent.1.1")]
    [Rule("rr:excess-damage")]
    public void MurderedYouDrawsWhenToughPreventsTheExcessDamage()
    {
        // rr:prevent.1.1: prevention reduces the damage taken, "but the amount of damage
        // 'dealt' is not reduced". Ruling 70423690d446327f: "it's possible to deal excess
        // damage to a character with Tough", and "Murdered You!" triggers. The pistol's 2
        // against 1 remaining hit point deals 1 excess; tough prevents all of it.
        var world = RocketBoards.Board();
        var rocket = RocketBoards.Rocket(world);
        var pistol = RocketBoards.Upgrade(world, "16038", charge: 3);
        var minion = GrootBoards.Engaged(world, WeaponsRunner);
        minion.TakeDamage(1);
        Statuses.Give(world, minion, Statuses.Tough);
        var offered = new List<int>();

        GrootBoards.Act(world, pistol, []);
        GrootBoards.Drive(world, prompt => GrootBoards.Take(minion)(prompt) ?? Respond(rocket, offered)(prompt));

        Assert.Single(offered);
        Assert.Single(world.Seats[0].Hand.Cards);
        Assert.Equal(1, minion.Damage);
        Assert.False(Statuses.Has(world, minion, Statuses.Tough));
    }

    [Fact]
    [Rule("rr:you-your.15")]
    public void MurderedYouIgnoresExcessDamageAnAllyDeals()
    {
        // "You" is Rocket Raccoon and the cards that extend him; an ally's attack is the
        // ally's (rr:you-your). The Rocket Raccoon ally's 4 overkill damage against the
        // Weapons Runner is not dealt by you.
        var world = RocketBoards.Board();
        var rocket = RocketBoards.Rocket(world);
        var ally = GrootBoards.InPlay(world, "16019", DeckType.AlliesArea);
        var minion = GrootBoards.Engaged(world, WeaponsRunner);
        var offered = new List<int>();

        AllyBasicPowers.AllyPower(world, Cards, ally, minion, BasicPowers.AttackVerb, []);
        GrootBoards.Drive(world, prompt => GrootBoards.Take(ally)(prompt) ?? Respond(rocket, offered)(prompt));

        Assert.Empty(offered);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(2, GrootBoards.Villain(world).Damage);
    }

    [Fact]
    [Rule("rr:limit")]
    public void TinkeringDiscardsATechUpgradeToDrawTwoOncePerRound()
    {
        // 16029b: "Action: Choose and discard a [tech] upgrade you control → draw 2
        // cards. (Limit once per round.)"
        var world = RocketBoards.Board("16029b");
        var rocket = RocketBoards.Rocket(world);
        var boots = RocketBoards.Upgrade(world, "16039");
        RocketBoards.Upgrade(world, "16045");
        RocketBoards.Upgrade(world, "16035");

        GrootBoards.Play(world, rocket, []);
        GrootBoards.Drive(world, GrootBoards.Take(boots));

        Assert.Equal(DeckType.DiscardPile, boots.Area.Type);
        Assert.Equal(2, world.Seats[0].Hand.Cards.Count);
        Assert.DoesNotContain(GrootBoards.Runner(world).Actions(world, 0), a => a.Card == rocket.ObjectId);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.2")]
    public void TinkeringIsNotOfferedWithoutATechUpgrade()
    {
        // Follow Through is a SKILL upgrade, not a [tech] one.
        var world = RocketBoards.Board("16029b");
        RocketBoards.Upgrade(world, "16045");
        Assert.DoesNotContain(GrootBoards.Runner(world).Actions(world, 0),
            a => a.Card == RocketBoards.Rocket(world).ObjectId);
    }

    /// <summary>Takes an optional response anchored to <paramref name="card"/>.</summary>
    internal static Func<Prompt, Decision?> Respond(Card card, List<int> offered) => prompt =>
        prompt.Cancellable ? GrootBoards.Take(card, offered)(prompt) : null;

    private static Decision? FirstTarget(Prompt prompt) =>
        prompt.Cancellable ? null : Decision.Take(prompt.Affordances[0].Id);
}
