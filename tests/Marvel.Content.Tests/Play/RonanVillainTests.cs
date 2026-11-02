using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Ronan the Accuser's three stages and both main schemes over the production book.
public sealed class RonanVillainTests : CoreCardTestBase
{
    [Theory]
    [InlineData(RonanBoards.RonanOne, false)]
    [InlineData(RonanBoards.RonanTwo, false)]
    [InlineData(RonanBoards.RonanThree, false)]
    [InlineData(RonanBoards.RonanOne, true)]
    [Rule("rr:activation")]
    public void RonanTakesAnotherBoostCardAgainstThePlayerWhoControlsThePowerStone(string stage, bool scheme)
    {
        // "Forced Interrupt: When Ronan the Accuser activates against you, give him 1 additional
        // boost card if you control the Power Stone." Each boost card is discarded afterwards.
        var world = RonanBoards.Board(face: scheme ? "01001b" : "01001a", villain: stage);
        RonanBoards.Stone(world, world.Seats[0].IdentityCard);

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(2, RonanBoards.Discarded(world));
    }

    [Fact]
    [Rule("rr:activation")]
    public void RonanTakesOneBoostCardWhileHeHoldsThePowerStone()
    {
        var world = RonanBoards.Board();
        var ronan = GrootBoards.Villain(world);
        RonanBoards.Stone(world, ronan);

        KangTemporalFixture.Activate(world, ronan, scheme: false, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(1, RonanBoards.Discarded(world));
    }

    [Fact]
    [Rule("rr:you-your.7")]
    public void RonanTakesOneBoostCardAgainstAPlayerWithoutThePowerStone()
    {
        // Player 1 controls the stone; Ronan attacks player 0.
        var world = RonanBoards.Board(players: 2);
        RonanBoards.Stone(world, world.Seats[1].IdentityCard);

        KangTemporalFixture.Activate(world, GrootBoards.Villain(world), scheme: false, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(1, RonanBoards.Discarded(world));
    }

    [Theory]
    [InlineData(RonanBoards.RonanTwo, RonanBoards.CutThePower, DeckType.EncounterDeck)]
    [InlineData(RonanBoards.RonanTwo, RonanBoards.CutThePower, DeckType.EncounterDiscardPile)]
    [InlineData(RonanBoards.RonanThree, RonanBoards.SuperiorTactics, DeckType.EncounterDeck)]
    [InlineData(RonanBoards.RonanThree, RonanBoards.SuperiorTactics, DeckType.EncounterDiscardPile)]
    [Rule("rr:search")]
    public void LaterStagesRevealTheirSideScheme(string stage, string side, DeckType from)
    {
        // 16104/16105: "When Revealed: Search the encounter deck and discard pile for the Cut
        // the Power (Superior Tactics) side scheme and reveal it. (Shuffle.)"
        var world = RonanBoards.Board(villain: stage);
        var ronan = GrootBoards.Villain(world);
        RonanBoards.Stone(world, ronan);
        var scheme = world.CreateCard(side, world.AreaOf(from));

        GrootBoards.Runner(world).ExecuteReveal(world, ronan, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
    }

    [Fact]
    [Rule("rr:action.1")]
    public void InterceptionImminentsFirstPlayerActionExhaustsTheMilanoToRemoveThreeThreat()
    {
        // 16106b: "First Player Action: Exhaust the Milano → remove 3 threat from this scheme."
        var world = RonanBoards.Board(players: 2);
        var milano = ShipCommandBoards.PutMilano(world);
        var scheme = GrootBoards.MainScheme(world);
        scheme.PlaceTokens("k_threat", 5);

        Assert.Empty(ShipCommandBoards.ActionsOn(world, scheme, 1));
        ShipCommandBoards.Act(world, scheme, 0);

        Assert.Equal(2, scheme.Tokens["k_threat"]);
        Assert.False(milano.Ready);
    }

    [Fact]
    [Rule("rr:attach-to.3.1")]
    public void TakeWhatIsMineTakesThePowerStoneFromTheHero()
    {
        // 16107a: "When Revealed: Attach the Power Stone to Ronan the Accuser. If it is already
        // attached to him, give him 1 facedown boost card."
        var world = RonanBoards.Board();
        var ronan = GrootBoards.Villain(world);
        var stone = RonanBoards.Stone(world, world.Seats[0].IdentityCard);
        var stage = world.CreateCard(RonanBoards.TakeWhatIsMine, world.AreaOf(DeckType.MainSchemesDeck));

        GrootBoards.Runner(world).ExecuteReveal(world, stage, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(ronan, RonanBoards.Host(world, stone));
        Assert.DoesNotContain(world.Cards, card => card.Area.Type == DeckType.BoostCardsDeck);
        Assert.Equal(3, RonanBoards.Modified(world, ronan, "attack"));
    }

    [Fact]
    [Rule("rr:boost-boost-icon.6")]
    public void TakeWhatIsMineGivesRonanAFacedownBoostCardWhenHeHoldsTheStone()
    {
        var world = RonanBoards.Board();
        var ronan = GrootBoards.Villain(world);
        var stone = RonanBoards.Stone(world, ronan);
        var stage = world.CreateCard(RonanBoards.TakeWhatIsMine, world.AreaOf(DeckType.MainSchemesDeck));

        GrootBoards.Runner(world).ExecuteReveal(world, stage, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(ronan, RonanBoards.Host(world, stone));
        var boost = Assert.Single(world.Cards, card => card.Area.Type == DeckType.BoostCardsDeck);
        Assert.Equal(ronan.ObjectId, boost.Area.Host);
    }

    [Fact]
    [Rule("rr:cannot")]
    public void TakeWhatIsMineCannotBeThwartedWhileRonanHoldsThePowerStone()
    {
        // 16107b: "While the Power Stone is attached to Ronan the Accuser, threat cannot be
        // removed from this scheme."
        var world = RonanBoards.Board(scheme: RonanBoards.TakeWhatIsMine);
        var scheme = GrootBoards.MainScheme(world);
        scheme.PlaceTokens("k_threat", 5);
        RonanBoards.Stone(world, GrootBoards.Villain(world));

        Assert.Throws<RulesNotImplementedException>(() => BasicPowers.BasicThwart(world, world.Facts, 0, scheme, []));
        Assert.Equal(5, scheme.Tokens["k_threat"]);
    }

    [Fact]
    [Rule("rr:attachment.1.1")]
    public void TakeWhatIsMineCanBeThwartedWhileAHeroHoldsThePowerStone()
    {
        // Spider-Man's THW 1 gains the stone's +1.
        var world = RonanBoards.Board(scheme: RonanBoards.TakeWhatIsMine);
        var scheme = GrootBoards.MainScheme(world);
        scheme.PlaceTokens("k_threat", 5);
        RonanBoards.Stone(world, world.Seats[0].IdentityCard);

        BasicPowers.BasicThwart(world, world.Facts, 0, scheme, []);
        BadoonBoards.Drive(world);

        Assert.Equal(3, scheme.Tokens["k_threat"]);
    }
}
