using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// The five Technique attachments over the production book: their constant abilities, the surge
// Nebula gives the first one revealed each round, and their boost abilities.
public sealed class NebulaTechniqueTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:surge.1")]
    public void OnlyTheFirstTechniqueRevealedEachRoundGainsSurge()
    {
        // 16088: "The first Technique attachment revealed each round gains surge." Player 1
        // reveals the first and is dealt the surge card; player 0's later Technique is not.
        var world = NebulaBoards.Board(players: 2);
        var nebula = GrootBoards.Villain(world);
        nebula.TakeDamage(20);
        var first = RonanBoards.Reveal(world, NebulaBoards.WeaponMastery, 1);
        BadoonBoards.Drive(world);
        var second = RonanBoards.Reveal(world, NebulaBoards.WideStance, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(nebula.ObjectId, first.Area.Host);
        Assert.Equal(nebula.ObjectId, second.Area.Host);
        // Surge deals the revealing player another encounter card.
        Assert.Equal(1, NebulaBoards.Dealt(world, 1));
        Assert.Equal(0, NebulaBoards.Dealt(world, 0));
    }

    [Fact]
    [Rule("rr:surge.1")]
    public void ANewRoundGivesTheNextTechniqueSurgeAgain()
    {
        var world = NebulaBoards.Board();
        GrootBoards.Villain(world).TakeDamage(10);
        RonanBoards.Reveal(world, NebulaBoards.WeaponMastery);
        BadoonBoards.Drive(world);
        world.Effects.Expire(TimingPoints.EndOfRound);
        RonanBoards.Reveal(world, NebulaBoards.WideStance);
        BadoonBoards.Drive(world);

        Assert.Equal(2, NebulaBoards.Dealt(world));
    }

    [Fact]
    [Rule("rr:boost-boost-icon")]
    public void ATechniqueBoostCardAttachesAfterTheActivationResolvesItsSpecialAndIsNotRevealed()
    {
        // 16094 Boost: "After this activation ends, attach this card to Nebula and resolve its
        // 'Special' ability." Turning a boost card faceup is not revealing it, so a Technique
        // revealed later this round still gains surge.
        var world = NebulaBoards.Board();
        var nebula = GrootBoards.Villain(world);
        nebula.TakeDamage(10);
        var boost = BadoonBoards.OnTop(world, NebulaBoards.Cutthroat);
        long threat = NebulaBoards.Threat(world);

        KangTemporalFixture.Activate(world, nebula, scheme: false, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(nebula.ObjectId, boost.Area.Host);
        Assert.Equal(threat + 1, NebulaBoards.Threat(world));
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, NebulaBoards.Dealt(world));

        RonanBoards.Reveal(world, NebulaBoards.WideStance);
        BadoonBoards.Drive(world);
        Assert.Equal(1, NebulaBoards.Dealt(world));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:you-your.8")]
    public void AWideStanceBoostCardDiscardsFromTheAttackedPlayersHand(int player)
    {
        // 16098: "Special: Discard 1 card at random from your hand." As a boost card, its Special
        // is resolved by the player the activation was against.
        var world = NebulaBoards.Board(players: 2);
        GrootBoards.Hand(world, "01089", 0);
        GrootBoards.Hand(world, "01089", 1);
        BadoonBoards.OnTop(world, NebulaBoards.WideStance);

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme: false, player);
        BadoonBoards.Drive(world);

        Assert.Empty(world.Seats[player].Hand.Cards);
        Assert.Single(world.Seats[1 - player].Hand.Cards);
    }

    [Theory]
    [InlineData(8, 1, 0, 5)]
    [InlineData(8, 2, 0, 5)]
    [InlineData(4, 1, 0, 4)]
    [InlineData(8, 1, 1, 5)]
    [InlineData(6, 1, 1, 5)]
    [InlineData(5, 0, 1, 4)]
    [Rule("rr:cannot")]
    public void CutthroatAmbitionAndWideStanceBoundTheDamageNebulaTakesFromAnAttack(
        long dealt, int cutthroats, int stances, long taken)
    {
        // 16094: "Nebula cannot take more than 5 damage from a single attack." 16098: "Reduce the
        // amount of damage Nebula takes from each attack by 1." The reduction comes first.
        var world = NebulaBoards.Board();
        var nebula = GrootBoards.Villain(world);
        for (int i = 0; i < cutthroats; i++) NebulaBoards.Technique(world, NebulaBoards.Cutthroat);
        for (int i = 0; i < stances; i++) NebulaBoards.Technique(world, NebulaBoards.WideStance);
        world.Activation = new EnemyActivation(nebula.ObjectId, 0, Attacking: true, Id: 17);

        DamagePlacement.Resolve(world, world.Facts, new DamageRequest(
            world.Seats[0].IdentityCard, nebula, dealt, "test", "Attack") { FromAttack = true }, []);

        Assert.Equal(taken, nebula.Damage);
    }

    [Fact]
    [Rule("rr:cannot")]
    public void CutthroatAmbitionBoundsEveryDamageInstanceOfOneAttackTogether()
    {
        // Two damage instances in the same attack total at most 5; a new attack starts again.
        var world = NebulaBoards.Board();
        var nebula = GrootBoards.Villain(world);
        NebulaBoards.Technique(world, NebulaBoards.Cutthroat);
        var hero = world.Seats[0].IdentityCard;
        world.Activation = new EnemyActivation(nebula.ObjectId, 0, Attacking: true, Id: 17);

        DamagePlacement.Resolve(world, world.Facts, new DamageRequest(hero, nebula, 3, "test", "Attack") { FromAttack = true }, []);
        Assert.Equal(2, AttackDamageMaximum.Remaining(world, nebula));
        DamagePlacement.Resolve(world, world.Facts, new DamageRequest(hero, nebula, 3, "test", "Attack") { FromAttack = true }, []);
        Assert.Equal(5, nebula.Damage);
        Assert.Equal(0, AttackDamageMaximum.Remaining(world, nebula));

        world.Activation = new EnemyActivation(nebula.ObjectId, 0, Attacking: true, Id: 18);
        DamagePlacement.Resolve(world, world.Facts, new DamageRequest(hero, nebula, 3, "test", "Attack") { FromAttack = true }, []);
        Assert.Equal(8, nebula.Damage);
    }

    [Fact]
    [Rule("rr:cannot")]
    public void CutthroatAmbitionDoesNotBoundDamageOutsideAnAttack()
    {
        var world = NebulaBoards.Board();
        var nebula = GrootBoards.Villain(world);
        NebulaBoards.Technique(world, NebulaBoards.Cutthroat);

        DamagePlacement.Resolve(world, world.Facts, new DamageRequest(
            world.Seats[0].IdentityCard, nebula, 8, "test", "Deal_Damage"), []);

        Assert.Equal(8, nebula.Damage);
        Assert.Equal(5, AttackDamageMaximum.Amount(world, nebula));
    }

    [Fact]
    [Rule("rr:stalwart")]
    public void EvasiveManeuveringAndUnyieldingPersistenceGiveNebulaStalwart()
    {
        // 16095/16096: "Nebula gains stalwart." Ruling c03bdba88590962d: gaining it removes a
        // stunned status card she already has.
        var world = NebulaBoards.Board();
        var nebula = GrootBoards.Villain(world);
        nebula.TakeDamage(10);
        Statuses.Give(world, nebula, Statuses.Stunned);

        RonanBoards.Reveal(world, NebulaBoards.Evasive);
        BadoonBoards.Drive(world);

        Assert.Equal(1, RonanBoards.Modified(world, nebula, "stalwart"));
        Assert.False(Statuses.Has(world, nebula, Statuses.Stunned));
        var persistence = NebulaBoards.Technique(world, NebulaBoards.Unyielding);
        Assert.Equal(2, RonanBoards.Modified(world, nebula, "stalwart"));
        Assert.Equal(nebula.ObjectId, persistence.Area.Host);
    }

    [Fact]
    [Rule("rr:retaliate-x")]
    public void WeaponMasteryGivesNebulaRetaliate()
    {
        // 16097: "Nebula gains retaliate 1." Two copies are two instances.
        var world = NebulaBoards.Board();
        var nebula = GrootBoards.Villain(world);
        NebulaBoards.Technique(world, NebulaBoards.WeaponMastery);
        Assert.Equal(1, RonanBoards.Modified(world, nebula, "retaliate"));
        NebulaBoards.Technique(world, NebulaBoards.WeaponMastery);
        Assert.Equal(2, RonanBoards.Modified(world, nebula, "retaliate"));
    }

    [Theory]
    [InlineData(8, true, "10/14 → 5/14 HP", true)]
    [InlineData(5, true, "10/14 → 5/14 HP", false)]
    [InlineData(4, true, "10/14 → 6/14 HP", false)]
    [InlineData(8, false, "10/14 → 2/14 HP", false)]
    [Rule("rr:cannot")]
    public void APreviewShowsTheDamageNebulaCanStillTakeFromAnAttack(long amount, bool attack, string health, bool noted)
    {
        // Nebula has 10 of her 14 hit points left; Cutthroat Ambition lets an attack take 5.
        var world = NebulaBoards.Board();
        var nebula = GrootBoards.Villain(world);
        nebula.TakeDamage(4);
        NebulaBoards.Technique(world, NebulaBoards.Cutthroat);
        var hero = world.Seats[0].IdentityCard;

        string preview = attack
            ? Damage.PreviewBasicAttack(world, world.Facts, hero, nebula, amount)
            : Damage.PreviewDamage(world, world.Facts, hero, nebula, amount);

        Assert.Contains(health, preview, StringComparison.Ordinal);
        Assert.Equal(noted, preview.Contains("Takes at most 5 damage from this attack", StringComparison.Ordinal));
        Assert.Equal(5, AttackDamageMaximum.Remaining(world, nebula));
        Assert.Null(AttackDamageMaximum.Remaining(world, hero));
    }
}
