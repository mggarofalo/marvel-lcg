using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Brotherhood of Badoon's Charge Up triggers over the production book.
public sealed class BadoonShipTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:special")]
    public void DrangSchemingChargesTheBadoonShip()
    {
        // 16058: "Forced Response: After Drang schemes, resolve the Badoon Ship's 'Charge Up'
        // ability." 16063: "Charge Up — Special: Place 1 barrage counter here."
        var world = BadoonBoards.Board(face: "01001b");
        var ship = BadoonBoards.Ship(world);

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme: true, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(1, BadoonBoards.Barrage(ship));
    }

    [Theory]
    [InlineData(BadoonBoards.DrangOne)]
    [InlineData(BadoonBoards.DrangTwo)]
    [Rule("rr:activation")]
    public void DrangAttackingDoesNotChargeTheShipBeforeStageThree(string villain)
    {
        // Stages I and II respond only after Drang schemes; an attack is the other activation.
        var world = BadoonBoards.Board(villain: villain);
        var ship = BadoonBoards.Ship(world);

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme: false, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(0, BadoonBoards.Barrage(ship));
    }

    [Fact]
    [Rule("rr:villain-defeat")]
    public void DrangThreeIsUnauthoredSoReachingHimRaises()
    {
        // Drang (III)'s When Revealed defers his minions' quickstrike, teamwork and responses
        // until all have engaged (ruling 82bcc63411002a00), which the engine cannot express.
        // Defeating Drang (II) reveals him; the engine raises rather than guess. Retaliate 1
        // deals Drang (II)'s last hit point.
        var world = BadoonBoards.Board(villain: BadoonBoards.DrangTwo);
        var hero = world.Seats[0].IdentityCard;
        var drang = GrootBoards.Villain(world);
        drang.TakeDamage(13);
        world.CreateCard(BadoonBoards.DrangThree, world.AreaOf(DeckType.VillainDeck));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: "retaliate", Amount: 1,
            Card: hero.ObjectId, Affects: hero.ObjectId));

        KangTemporalFixture.Activate(world, drang, scheme: false, 0);
        var thrown = Assert.Throws<RulesNotImplementedException>(() => BadoonBoards.Drive(world));

        Assert.Contains(BadoonBoards.DrangThree, thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Rule("rr:indirect-damage")]
    public void TheFourthBarrageCounterDealsTwoIndirectDamageToEachPlayerAndClearsTheShip()
    {
        // 16063: "Then, if there are 4 or more barrage counters here, deal 2 indirect damage to
        // each player and remove all barrage counters from here."
        var world = BadoonBoards.Board(players: 2, face: "01001b");
        var ship = BadoonBoards.Ship(world);
        ship.PlaceTokens("c_barrage", 3);

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme: true, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(0, BadoonBoards.Barrage(ship));
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(2, world.Seats[1].IdentityCard.Damage);
    }

    [Fact]
    [Rule("rr:indirect-damage")]
    public void TheThirdBarrageCounterDealsNoDamage()
    {
        var world = BadoonBoards.Board(players: 2, face: "01001b");
        var ship = BadoonBoards.Ship(world);
        ship.PlaceTokens("c_barrage", 2);

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme: true, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(3, BadoonBoards.Barrage(ship));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
    }

    [Fact]
    [Rule("rr:special")]
    public void ChargeUpDoesNothingWithoutABadoonShip()
    {
        // rr:special: a Special resolves only through an explicit instruction, and there is
        // no Badoon Ship to resolve it on.
        var world = BadoonBoards.Board(face: "01001b");

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme: true, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(1, GrootBoards.MainScheme(world).Tokens.GetValueOrDefault("k_threat"));
    }

    [Theory]
    [InlineData(BadoonBoards.TerrestrialInvasion)]
    [InlineData(BadoonBoards.ProtectThePlanet)]
    [Rule("rr:villain-phase")]
    public void EachMainSchemeChargesTheShipAfterStepOne(string scheme)
    {
        // 16061b/16062b: "Forced Response: After resolving step one of the villain phase,
        // resolve the Badoon Ship's 'Charge Up' ability."
        var world = BadoonBoards.Board(scheme: scheme);
        var ship = BadoonBoards.Ship(world);

        StepOne(world);

        Assert.Equal(1, BadoonBoards.Barrage(ship));
    }

    [Fact]
    [Rule("rr:villain-phase.step.1")]
    public void TheMainSchemeChargesTheShipEvenWhenStepOnePlacesNoThreat()
    {
        // "After resolving step one" follows the step, not a positive placement: a main scheme
        // that lost its acceleration field places nothing and still charges the ship.
        var world = BadoonBoards.Board();
        var ship = BadoonBoards.Ship(world);
        var scheme = GrootBoards.MainScheme(world);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Characteristics.LossOf("escalation_threat"), Affects: scheme.ObjectId));

        StepOne(world);

        Assert.Equal(0, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(1, BadoonBoards.Barrage(ship));
    }

    [Fact]
    [Rule("rr:villain-phase")]
    public void BombardmentChargesTheShipAgainAfterStepOne()
    {
        // 16067: the side scheme and the main scheme each respond to step one.
        var world = BadoonBoards.Board();
        var ship = BadoonBoards.Ship(world);
        GrootBoards.Side(world, "16067", 3);

        StepOne(world);

        Assert.Equal(2, BadoonBoards.Barrage(ship));
    }

    [Fact]
    [Rule("rr:main-scheme-main-scheme-deck.step.2")]
    public void ProtectThePlanetChargesTheShipWhenRevealed()
    {
        // 16062a: "When Revealed: Resolve the Badoon Ship's 'Charge Up' ability."
        var world = BadoonBoards.Board();
        var ship = BadoonBoards.Ship(world);
        var stage = world.CreateCard(BadoonBoards.ProtectThePlanet, world.AreaOf(DeckType.MainSchemesDeck));

        GrootBoards.Runner(world).ExecuteReveal(world, stage, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(1, BadoonBoards.Barrage(ship));
    }

    [Fact]
    [Rule("rr:engage")]
    public void BadoonEngineerChargesTheShipWhenItEngagesAndAfterItActivates()
    {
        // 16065: "Forced Response: After Badoon Engineer engages you or activates against you,
        // resolve the Badoon Ship's 'Charge Up' ability."
        var world = BadoonBoards.Board(face: "01001b");
        var ship = BadoonBoards.Ship(world);
        var engineer = ShipCommandBoards.Reveal(world, BadoonBoards.Engineer);
        BadoonBoards.Drive(world);
        Assert.Equal(DeckType.EngagedEnemiesArea, engineer.Area.Type);
        Assert.Equal(1, BadoonBoards.Barrage(ship));

        KangTemporalFixture.Activate(world, engineer, scheme: true, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(2, BadoonBoards.Barrage(ship));
    }

    [Fact]
    [Rule("rr:boost-boost-icon")]
    public void BadoonEngineerAsABoostCardChargesTheShip()
    {
        // 16065: "Boost: Resolve the Badoon Ship's 'Charge Up' ability."
        var world = BadoonBoards.Board(face: "01001b");
        var ship = BadoonBoards.Ship(world);
        BadoonBoards.OnTop(world, BadoonBoards.Engineer);

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme: true, 0);
        BadoonBoards.Drive(world);

        // One from the boost, one from Drang's own forced response after he schemes.
        Assert.Equal(2, BadoonBoards.Barrage(ship));
    }

    private static void StepOne(World world)
    {
        world.Agenda.Add(new PhaseStep(new ThreatStageOperation(ThreatStageKind.PlaceThreat), 1, 1));
        BadoonBoards.Drive(world);
    }
}
