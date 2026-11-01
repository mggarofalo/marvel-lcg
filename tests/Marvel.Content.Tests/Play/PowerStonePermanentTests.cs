using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// The production Power Stone (16149) when its host leaves play. "Permanent." rr:permanent.5:
// "If the card to which a permanent attachment was attached leaves play, resolve that
// attachment's 'attach to' text. If it has none or that text does not have a valid target,
// remove the attachment from the game."
public sealed class PowerStonePermanentTests : CoreCardTestBase
{
    private const string HulkAlly = "01050";
    private const string WeaponsRunner = "01121";

    private static World Board(string identity = "01019a,01019b", int players = 1) =>
        ShipCommandBoards.PowerStoneBoard(identity, players);

    private static Card Stone(World world, Card host, string face = ShipCommandBoards.PowerStone) =>
        ShipCommandBoards.Attach(world, host, face);

    private static Card Host(World world, Card stone) => world.Cards[stone.Area.Host];

    [Fact]
    [Rule("rr:permanent.5")]
    public void ThePowerStoneOfAnEliminatedHeroAttachesToTheVillain()
    {
        // Ruling 8944f7176577c908: an eliminated identity's Power Stone resolves its "attach to" text.
        var world = Board(ShipCommandBoards.SpiderMan, players: 2);
        var villain = GrootBoards.Villain(world);
        var stone = Stone(world, world.Seats[0].IdentityCard);
        var events = new List<GameEvent>();

        Elimination.Eliminate(world, world.Facts, 0, "test", events);

        Assert.Equal(villain, Host(world, stone));
        Assert.True(DeckTypes.IsInPlay(stone.Area.Type));
        Assert.Contains(events.OfType<CardAttached>(), attached => attached.Card == stone.ObjectId && attached.Host == villain.ObjectId);
    }

    [Fact]
    [Rule("rr:permanent.5")]
    public void ThePowerStoneOfADefeatedMinionAttachesToTheVillain()
    {
        // The Hulk ally defeats a 2-hit-point Weapons Runner holding the stone; an ally's
        // attack does not take it, and rr:permanent.5 resolves "Attach to the villain".
        var world = Board();
        var villain = GrootBoards.Villain(world);
        var runner = GrootBoards.Engaged(world, WeaponsRunner);
        var stone = Stone(world, runner);

        DefeatWithTheHulk(world, runner);

        Assert.Equal(DeckType.EncounterDiscardPile, runner.Area.Type);
        Assert.Equal(villain, Host(world, stone));
    }

    [Fact]
    [Rule("rr:permanent.5")]
    public void AnAttachmentThatIsNotPermanentLeavesWithItsHost()
    {
        // Charge (01099) reads "Attach to Rhino" but is not permanent, so it is discarded.
        var world = Board();
        var runner = GrootBoards.Engaged(world, WeaponsRunner);
        var charge = Stone(world, runner, "01099");

        DefeatWithTheHulk(world, runner);

        Assert.Equal(DeckType.EncounterDiscardPile, charge.Area.Type);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:permanent.5")]
    public void APermanentAttachmentInATreeOfAttachmentsRaises(bool stoneHostsCharge)
    {
        // Only a direct permanent attachment with nothing attached to it is modelled.
        var world = Board();
        var runner = GrootBoards.Engaged(world, WeaponsRunner);
        var first = Stone(world, runner, stoneHostsCharge ? "16149" : "01099");
        Stone(world, first, stoneHostsCharge ? "01099" : "16149");

        var raised = Record.Exception(() => DefeatWithTheHulk(world, runner));

        Assert.True(raised is RulesNotImplementedException || raised?.InnerException is RulesNotImplementedException);
    }

    private static void DefeatWithTheHulk(World world, Card enemy)
    {
        var hulk = GrootBoards.InPlay(world, HulkAlly, DeckType.AlliesArea);
        AllyBasicPowers.AllyPower(world, world.Facts, hulk, enemy, BasicPowers.AttackVerb, []);
        ShipCommandBoards.Choose(world);
    }

    [Fact]
    [Rule("rr:permanent.5")]
    public void APermanentAttachmentWithNoValidTargetIsRemovedFromTheGame()
    {
        // With no villain in play, "Attach to the villain" has no valid target.
        var world = Board();
        var villain = GrootBoards.Villain(world);
        World.MoveToTop(villain, world.AreaOf(DeckType.RemovedArea));
        var runner = GrootBoards.Engaged(world, WeaponsRunner);
        var stone = Stone(world, runner);

        DefeatWithTheHulk(world, runner);

        Assert.Equal(DeckType.RemovedArea, stone.Area.Type);
    }
}
