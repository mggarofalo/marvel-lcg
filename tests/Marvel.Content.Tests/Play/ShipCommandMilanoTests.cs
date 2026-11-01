using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// The production Milano (16142) and the two First Player Actions of Ship Command on synthetic boards.
public sealed class ShipCommandMilanoTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:resource-ability.1")]
    public void TheMilanoGeneratesAWildResourceForAnyPlayerOutsidePeril()
    {
        // "Piloting - Resource: Exhaust the Milano -> generate a [wild] resource for any player."
        var world = ShipCommandBoards.Board(players: 2);
        var milano = ShipCommandBoards.PutMilano(world);

        Assert.Contains(milano.ObjectId, ShipCommandBoards.Sources(world, 0));
        Assert.Contains(milano.ObjectId, ShipCommandBoards.Sources(world, 1));
        var source = GrootBoards.Runner(world).ResourceAbilities(world, 1).Single(s => s.Effect == milano.ObjectId);
        Assert.Equal("G", source.Generates);
        Assert.Equal(World.Scenario, milano.Owner);
    }

    [Fact]
    [Rule("rr:peril.1")]
    public void TheFirstPlayersMilanoCannotPayForAnotherPlayerResolvingAPerilCard()
    {
        // rr:peril.1: "other players cannot trigger abilities" while a player resolves a peril card.
        var world = ShipCommandBoards.Board(players: 2);
        var milano = ShipCommandBoards.PutMilano(world);
        world.CreateCard("16146", world.AreaOf(DeckType.RevealingArea, PlayArea.Of(1)));

        Assert.DoesNotContain(milano.ObjectId, ShipCommandBoards.Sources(world, 1));
        Assert.Equal(1, Peril.Resolver(world));
    }

    [Fact]
    [Rule("rr:peril.1")]
    public void TheFirstPlayerResolvingTheirOwnPerilCardMayStillUseTheMilano()
    {
        var world = ShipCommandBoards.Board(players: 2);
        var milano = ShipCommandBoards.PutMilano(world);
        world.CreateCard("16146", world.AreaOf(DeckType.RevealingArea, PlayArea.Of(0)));

        Assert.Contains(milano.ObjectId, ShipCommandBoards.Sources(world, 0));
    }

    [Fact]
    [Rule("rr:first-player")]
    public void TheMilanoFollowsTheFirstPlayerTokenAtTheEndOfTheRound()
    {
        // Ruling 027e5dbfef0d5861: a card "the first player controls" changes control
        // when the first player changes.
        var world = ShipCommandBoards.Board(players: 2);
        var milano = ShipCommandBoards.PutMilano(world);
        milano.Exhaust();
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.PassFirstPlayerToken), 1, 5));

        GrootBoards.Drive(world, _ => null);

        Assert.Equal(1, world.FirstPlayer);
        Assert.Equal(PlayArea.Of(1), milano.Area.PlayArea);
        Assert.Equal(DeckType.SupportsArea, milano.Area.Type);
        Assert.False(milano.Ready);
        Assert.Equal(1, CardControl.ControllerOf(world, milano));
        Assert.Equal(World.Scenario, milano.Owner);
    }

    [Fact]
    [Rule("rr:player-elimination")]
    public void TheMilanoPassesWithTheTokenWhenTheFirstPlayerIsEliminated()
    {
        var world = ShipCommandBoards.Board(players: 2);
        var milano = ShipCommandBoards.PutMilano(world);
        var events = new List<GameEvent>();

        Elimination.Eliminate(world, world.Facts, 0, "test", events);

        Assert.Equal(1, world.FirstPlayer);
        Assert.Equal(PlayArea.Of(1), milano.Area.PlayArea);
        Assert.True(DeckTypes.IsInPlay(milano.Area.Type));
        Assert.Contains(events.OfType<CardsMoved>(), moved => moved.Verb == "Move"
            && moved.Cards.Any(landing => landing.Card == milano.ObjectId));
    }

    [Fact]
    [Rule("rr:first-player")]
    public void TheMilanoStaysPutWhenTheTokenStaysWithItsController()
    {
        // Alone, the first player keeps the token; the Milano stays beneath Aunt May (01006).
        var world = ShipCommandBoards.Board();
        var milano = ShipCommandBoards.PutMilano(world);
        var auntMay = GrootBoards.InPlay(world, "01006", DeckType.SupportsArea);
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.PassFirstPlayerToken), 1, 5));

        GrootBoards.Drive(world, _ => null);

        Assert.Equal(PlayArea.Of(0), milano.Area.PlayArea);
        Assert.Equal([milano.ObjectId, auntMay.ObjectId], milano.Area.Cards.Select(card => card.ObjectId));
    }

    [Fact]
    [Rule("rr:first-player")]
    public void AMilanoWithACardAttachedRaisesRatherThanMovingIt()
    {
        // Only a support with nothing attached is modelled following the first player.
        var world = ShipCommandBoards.Board(players: 2);
        var milano = ShipCommandBoards.PutMilano(world);
        var attached = world.CreateCard("01099", world.AreaOf(DeckType.EncounterDiscardPile));
        World.MoveToTop(attached, world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), milano.ObjectId, 0));
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.PassFirstPlayerToken), 1, 5));

        var raised = Record.Exception(() => GrootBoards.Drive(world, _ => null));

        Assert.True(raised is RulesNotImplementedException || raised?.InnerException is RulesNotImplementedException);
    }

    [Fact]
    [Rule("rr:action.1")]
    public void AFirstPlayerActionIsNotOfferedToAnotherPlayerWhoCouldPayIt()
    {
        // A Milano in player 1's supports pays the cost, but Cannonade (16144) is a
        // "First Player Action" and player 0 is the first player.
        var world = ShipCommandBoards.Board(players: 2);
        var milano = world.CreateCard(ShipCommandBoards.Milano, world.AreaOf(DeckType.EncounterDiscardPile));
        World.MoveToTop(milano, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var cannonade = GrootBoards.Side(world, "16144", 5);

        Assert.Equal(0, world.FirstPlayer);
        Assert.Empty(ShipCommandBoards.ActionsOn(world, cannonade, 1));
    }

    [Fact]
    [Rule("rr:action.1")]
    public void CannonadesFirstPlayerActionExhaustsTheMilanoToRemoveThreeThreat()
    {
        // 16144: "First Player Action: Exhaust the Milano -> remove 3 threat from this scheme."
        var world = ShipCommandBoards.Board(players: 2);
        var milano = ShipCommandBoards.PutMilano(world);
        var cannonade = GrootBoards.Side(world, "16144", 5);

        Assert.Empty(ShipCommandBoards.ActionsOn(world, cannonade, 1));
        ShipCommandBoards.Act(world, cannonade, 0);

        Assert.Equal(2, cannonade.Tokens["k_threat"]);
        Assert.False(milano.Ready);
        Assert.Empty(ShipCommandBoards.ActionsOn(world, cannonade, 0));
    }

    [Fact]
    [Rule("rr:exhausted.2")]
    public void CannonadeIsNotOfferedWithoutAReadyMilano()
    {
        var world = ShipCommandBoards.Board();
        var cannonade = GrootBoards.Side(world, "16144", 5);

        Assert.Empty(ShipCommandBoards.ActionsOn(world, cannonade, 0));
    }

    [Fact]
    [Rule("rr:action.1")]
    public void RogueVesselsFirstPlayerActionExhaustsTheMilanoAndSpendsTwoResources()
    {
        // 16143: "First Player Action: Exhaust the Milano and spend 2 resources of any type
        // -> discard this card."
        var world = ShipCommandBoards.Board();
        var milano = ShipCommandBoards.PutMilano(world);
        var vessel = world.CreateCard("16143", world.AreaOf(DeckType.EnvironmentArea));
        var paying = new[] { GrootBoards.Hand(world, ShipCommandBoards.Energy), GrootBoards.Hand(world, ShipCommandBoards.Genius) };

        ShipCommandBoards.Act(world, vessel, 0, paying);

        Assert.Equal(DeckType.EncounterDiscardPile, vessel.Area.Type);
        Assert.False(milano.Ready);
        Assert.All(paying, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
    }

    [Fact]
    [Rule("rr:exhausted.1")]
    public void RogueVesselCannotSpendTheMilanoItExhaustsAsACost()
    {
        // The Milano pays its own exhaust cost, so it cannot also generate one of the two resources.
        var world = ShipCommandBoards.Board();
        ShipCommandBoards.PutMilano(world);
        var vessel = world.CreateCard("16143", world.AreaOf(DeckType.EnvironmentArea));
        // Backflip prints one [physical] resource; the Energy basic resource prints two.
        GrootBoards.Hand(world, "01003");

        Assert.Empty(ShipCommandBoards.ActionsOn(world, vessel, 0));
    }

    [Fact]
    [Rule("rr:environment")]
    public void RogueVesselDealsOneDamageToEachPlayerWhenTheVillainPhaseEnds()
    {
        // "Forced Interrupt: When the villain phase ends, deal 1 damage to each player."
        var world = ShipCommandBoards.Board(players: 2);
        world.CreateCard("16143", world.AreaOf(DeckType.EnvironmentArea));
        world.Agenda.Add(new PhaseStep(new PhaseTransitionOperation(PhaseTransitionKind.EndVillainPhase), 1, 6));

        ShipCommandBoards.Choose(world);

        Assert.All(world.Seats, seat => Assert.Equal(1, seat.IdentityCard.Damage));
    }
}
