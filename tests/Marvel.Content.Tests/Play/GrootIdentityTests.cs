using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production rows for Groot's identity on synthetic boards.
public sealed class GrootIdentityTests : CoreCardTestBase
{
    private const string HydraHunter = "04101";

    [Theory]
    [InlineData(3, 1, 0)]
    [InlineData(1, 0, 1)]
    [InlineData(2, 0, 0)]
    [Rule("rr:prevent.1")]
    [Rule("rr:damage")]
    public void FloraColossusSpendsOneGrowthCounterPerDamagePrevented(int growth, long left, long taken)
    {
        // 16001a: "Forced Interrupt: When Groot would take any amount of damage, remove
        // that many growth counters from him. For each growth counter removed this way,
        // prevent 1 of that damage." rr:prevent.1: "reduce the amount of damage the
        // target takes". Ruling a1a372ea9b59ba4e: "3 growth counters will be removed ...
        // The remaining 2 damage will then be dealt to Groot's hit points." Rhino's
        // undefended attack deals 2.
        var world = GrootBoards.Board();
        var groot = GrootBoards.Groot(world);
        GrootBoards.Grow(groot, growth);

        KangTemporalFixture.Attack(world, GrootBoards.Villain(world), 0);
        GrootBoards.Drive(world, _ => null);

        Assert.Equal(left, GrootBoards.Growth(groot));
        Assert.Equal(taken, groot.Damage);
    }

    [Fact]
    [Rule("rr:damage")]
    [Rule("rr:status-cards.2")]
    public void ToughPreventsTheDamageBeforeFloraColossusSpendsCounters()
    {
        // rr:damage orders "Tough status cards" before "Abilities that trigger 'when
        // [character] would take any amount of damage'". Ruling 7cf2a0ea8a6b2a1f: "the
        // tough status card will prevent the damage before Groot's Flora Colossus
        // ability is able to trigger."
        var world = GrootBoards.Board();
        var groot = GrootBoards.Groot(world);
        GrootBoards.Grow(groot, 3);
        Statuses.Give(world, groot, Statuses.Tough);

        KangTemporalFixture.Attack(world, GrootBoards.Villain(world), 0);
        GrootBoards.Drive(world, _ => null);

        Assert.Equal(3, GrootBoards.Growth(groot));
        Assert.Equal(0, groot.Damage);
        Assert.False(Statuses.Has(world, groot, Statuses.Tough));
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    [Rule("rr:damage")]
    [Rule("rr:tough.2")]
    public void AToughStatusCardLeavesNoDamageForAWouldTakeInterrupt(bool tough, int offers)
    {
        // rr:damage orders "Tough status cards" (step 2) before abilities that trigger
        // "when [character] would take any amount of damage" (step 3). A tough status
        // card prevents all of the attack's damage, so Backflip (01003) has nothing left
        // to prevent and is not offered.
        var world = GrootBoards.Board(players: 2);
        var spider = world.Seats[1].IdentityCard;
        var backflip = GrootBoards.Hand(world, "01003", 1);
        if (tough) Statuses.Give(world, spider, Statuses.Tough);
        var offered = new List<int>();

        KangTemporalFixture.Attack(world, GrootBoards.Villain(world), 1);
        GrootBoards.Drive(world, GrootBoards.Take(backflip, offered));

        Assert.Equal(offers, offered.Count);
        Assert.Equal(0, spider.Damage);
    }

    [Fact]
    [Rule("rr:piercing.1")]
    [Rule("rr:damage")]
    public void APiercingAttackDiscardsToughBeforeAWouldTakeInterruptIsOffered()
    {
        // rr:piercing.1: "discard each tough status card on that character before this
        // attack deals damage to a character". Hydra Hunter's attacks gain piercing, so
        // Backflip (01003) still has its 2 damage to prevent.
        var world = GrootBoards.Board(players: 2);
        var spider = world.Seats[1].IdentityCard;
        var backflip = GrootBoards.Hand(world, "01003", 1);
        Statuses.Give(world, spider, Statuses.Tough);
        var hunter = GrootBoards.Engaged(world, HydraHunter, 1);
        var offered = new List<int>();

        KangTemporalFixture.Attack(world, hunter, 1);
        GrootBoards.Drive(world, GrootBoards.Take(backflip, offered));

        Assert.Single(offered);
        Assert.Equal(0, spider.Damage);
        Assert.False(Statuses.Has(world, spider, Statuses.Tough));
    }

    [Fact]
    [Rule("rr:piercing.1")]
    [Rule("rr:damage")]
    public void APiercingAttackLeavesFloraColossusTheDamageToughNoLongerPrevents()
    {
        // The pierced tough status card is gone before rr:damage step 3, so Flora
        // Colossus spends 2 of Groot's 3 growth counters on Hydra Hunter's 2 damage.
        var world = GrootBoards.Board();
        var groot = GrootBoards.Groot(world);
        GrootBoards.Grow(groot, 3);
        Statuses.Give(world, groot, Statuses.Tough);
        var hunter = GrootBoards.Engaged(world, HydraHunter);

        KangTemporalFixture.Attack(world, hunter, 0);
        GrootBoards.Drive(world, _ => null);

        Assert.Equal(1, GrootBoards.Growth(groot));
        Assert.Equal(0, groot.Damage);
        Assert.False(Statuses.Has(world, groot, Statuses.Tough));
    }

    [Theory]
    [InlineData("16001a", 1, 0)]
    [InlineData("16001b", 3, 2)]
    [Rule("rr:indirect-damage")]
    [Rule("rr:identity.4")]
    public void FloraColossusAnswersIndirectDamageOnlyInHeroForm(string face, long left, long taken)
    {
        // rr:indirect-damage: assigned indirect damage is dealt to the characters it is
        // assigned to. rr:identity.4: only the faceup side is in play, and Flora Colossus
        // is printed on the hero face. Fan the Flames deals 2 indirect damage to Groot.
        var world = GrootBoards.Board(face);
        var groot = GrootBoards.Groot(world);
        GrootBoards.Grow(groot, 3);

        GrootEncounterTests.Reveal(world, "16028");
        GrootBoards.Drive(world, GrootEncounterTests.AssignTo(groot));

        Assert.Equal(left, GrootBoards.Growth(groot));
        Assert.Equal(taken, groot.Damage);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(9, 10)]
    [Rule("rr:max-maximum.6")]
    [Rule("rr:limit")]
    public void GrowthSpurtPlacesTwoToAMaximumOfTenOncePerRound(int growth, long after)
    {
        // 16001b: "Action: Place 2 growth counters on Groot (to a maximum of 10). (Limit
        // once per round.)" rr:max-maximum.6: the parenthetical "imposes a maximum on
        // that ability for the duration of that ability".
        var world = GrootBoards.Board("16001b");
        var groot = GrootBoards.Groot(world);
        GrootBoards.Grow(groot, growth);

        GrootBoards.Play(world, groot, []);
        GrootBoards.Drive(world, _ => null);

        Assert.Equal(after, GrootBoards.Growth(groot));
        Assert.DoesNotContain(GrootBoards.Runner(world).Actions(world, 0), a => a.Card == groot.ObjectId);
    }

    [Fact]
    [Rule("rr:max-maximum.6")]
    public void GrowthSpurtIsNotOfferedAtTenCounters()
    {
        // At 10 counters the capped placement would place nothing.
        var world = GrootBoards.Board("16001b");
        var groot = GrootBoards.Groot(world);
        GrootBoards.Grow(groot, 10);
        Assert.DoesNotContain(GrootBoards.Runner(world).Actions(world, 0), a => a.Card == groot.ObjectId);
    }
}
