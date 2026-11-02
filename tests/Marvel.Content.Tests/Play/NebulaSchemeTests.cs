using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Nebula's two main schemes and Nebula's Ship over the production book.
public sealed class NebulaSchemeTests : CoreCardTestBase
{
    [Theory]
    [InlineData(NebulaBoards.ArtOfEvasion, 1, 0)]
    [InlineData(NebulaBoards.ArtOfEvasion, 1, 3)]
    [InlineData(NebulaBoards.ArtOfEvasion, 2, 3)]
    [InlineData(NebulaBoards.WarpDrive, 2, 4)]
    [Rule("rr:main-scheme-main-scheme-deck")]
    public void EachMainSchemesEscalationIsTheEvasionCountersOnNebulasShip(string scheme, int players, int evasion)
    {
        // 16091b/16092b: "X is equal to the number of evasion counters on Nebula's Ship."
        var world = NebulaBoards.Board(players, scheme: scheme);
        NebulaBoards.PutShip(world, evasion);

        Assert.Equal(evasion, RonanBoards.Modified(world, GrootBoards.MainScheme(world), "escalation_threat"));
    }

    [Fact]
    [Rule("rr:main-scheme-main-scheme-deck")]
    public void EscalationIsZeroWithoutNebulasShip()
    {
        var world = NebulaBoards.Board();
        Assert.Equal(0, RonanBoards.Modified(world, GrootBoards.MainScheme(world), "escalation_threat"));
    }

    [Theory]
    [InlineData(1, 0, 4)]
    [InlineData(2, 1, 6)]
    [Rule("rr:discard.4")]
    public void WarpDriveInitiatedDiscardsTwoCardsPerEvasionCounterFromEveryDeck(int players, int evasion, int discarded)
    {
        // 16092a: "When Revealed: Place 2 evasion counters on Nebula's Ship. For each evasion
        // counter on Nebula's Ship, discard the top 2 cards of each player deck and the encounter
        // deck." The count is read after the 2 counters are placed.
        var world = NebulaBoards.Board(players);
        var ship = NebulaBoards.PutShip(world, evasion);
        for (int i = 0; i < 12; i++) world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        var stage = world.CreateCard(NebulaBoards.WarpDrive, world.AreaOf(DeckType.MainSchemesDeck));

        GrootBoards.Runner(world).ExecuteReveal(world, stage, 0);
        BadoonBoards.Drive(world);

        Assert.Equal(evasion + 2, NebulaBoards.Evasion(ship));
        for (int player = 0; player < players; player++)
            Assert.Equal(12 - discarded, world.Seats[player].Deck.Cards.Count);
        Assert.Equal(24 - discarded, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
    }

    [Fact]
    [Rule("rr:villain-phase")]
    public void NebulasShipGainsAnEvasionCounterAsTheVillainPhaseBegins()
    {
        // 16093: "Forced Interrupt: When the villain phase begins, place 1 evasion counter here."
        var world = NebulaBoards.Board();
        var ship = NebulaBoards.PutShip(world);
        GrootBoards.Villain(world).TakeDamage(10);

        VillainPhase.Schedule(world.Agenda, 1);
        BadoonBoards.Drive(world);

        Assert.Equal(1, NebulaBoards.Evasion(ship));
    }

    [Theory]
    [InlineData(1, 3, 2)]
    [InlineData(2, 3, 1)]
    [InlineData(2, 2, 0)]
    [InlineData(1, 1, 0)]
    [Rule("rr:action.1")]
    public void ShootTheThrustersExhaustsTheMilanoToRemoveACounterPerResource(int spent, int evasion, long left)
    {
        // 16093: "First Player Action: Exhaust the Milano and spend up to 2 resources of any type
        // → remove 1 evasion counter from here for each resource spent this way."
        var world = NebulaBoards.Board(players: 2);
        var ship = NebulaBoards.PutShip(world, evasion);
        var milano = ShipCommandBoards.PutMilano(world);
        // Backflip and Swinging Web Kick each print one resource.
        var backflip = GrootBoards.Hand(world, "01003");
        var kick = GrootBoards.Hand(world, "01005");
        var paying = spent == 1 ? new[] { backflip } : [backflip, kick];
        GrootBoards.Hand(world, "01003", 1);
        GrootBoards.Hand(world, "01005", 1);

        // Player 1 holds the same resources and is not the first player.
        Assert.Empty(ShipCommandBoards.ActionsOn(world, ship, 1));
        // Shoot the Thrusters! is authored once for 1 resource (action ordinal 0) and once for 2.
        var action = Assert.Single(ShipCommandBoards.ActionsOn(world, ship, 0), a => a.Ordinal == spent - 1);
        world.Agenda.AddPlayerAction(0, new PlayerAction(action, [.. paying.Select(c => c.ObjectId)], []));
        GrootBoards.Drive(world, _ => null);

        Assert.Equal(left, NebulaBoards.Evasion(ship));
        Assert.False(milano.Ready);
        Assert.All(paying, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [Rule("rr:exhausted.1")]
    public void ShootTheThrustersNeedsAnEvasionCounterAndAReadyMilano(int evasion, bool ready)
    {
        var world = NebulaBoards.Board();
        var ship = NebulaBoards.PutShip(world, evasion);
        var milano = ShipCommandBoards.PutMilano(world);
        if (!ready) milano.Exhaust();
        GrootBoards.Hand(world, "01003");
        GrootBoards.Hand(world, "01005");

        Assert.Empty(ShipCommandBoards.ActionsOn(world, ship, 0));
    }

    [Fact]
    [Rule("rr:first-player.4")]
    public void TheMilanosControllerCannotShootTheThrustersOnceTheFirstPlayerPasses()
    {
        // The Milano stays under player 0's control after player 1 becomes the first player.
        var world = NebulaBoards.Board(players: 2);
        var ship = NebulaBoards.PutShip(world, 2);
        ShipCommandBoards.PutMilano(world);
        GrootBoards.Hand(world, "01003");
        world.FirstPlayer = 1;

        Assert.Empty(ShipCommandBoards.ActionsOn(world, ship, 0));
    }

    [Fact]
    [Rule("rr:action.1")]
    public void SpendingTwoResourcesNeedsTwoEvasionCounters()
    {
        var world = NebulaBoards.Board();
        var ship = NebulaBoards.PutShip(world, 1);
        ShipCommandBoards.PutMilano(world);
        GrootBoards.Hand(world, "01003");
        GrootBoards.Hand(world, "01005");

        Assert.Equal(0, Assert.Single(ShipCommandBoards.ActionsOn(world, ship, 0)).Ordinal);
    }
}
