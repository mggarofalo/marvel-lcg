using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class TemporaryIdentityRegressionTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:unique-icon.1")]
    [Rule("rr:max-maximum.3")]
    [Theory]
    [InlineData("01084")]
    [InlineData("01091")]
    public void ADroneDoesNotCountAsItsUnderlyingUniqueOrLimitedPlayerCard(string face)
    {
        // rr:max-maximum.3 restricts copies "each player may control in play".
        // rr:unique-icon.1 prohibits another instance of the same unique card.
        var world = Board();
        world.CreateCard(face, world.Seats[0].Deck);
        var drone = Engage(world);
        var entering = world.CreateCard(face, world.Seats[0].Hand);

        Assert.Equal("Drone", EffectiveCards.Title(drone, Cards));
        Assert.False(Uniqueness.IsBlocked(world, Cards, entering));
        Assert.True(CardPlay.WithinPerPlayerLimit(world, Cards, world.Seats[0], entering, null, world.Abilities));
        Assert.Equal(World.Scenario, CardControl.ControllerOf(world, drone));

        Discard.Card(world, drone, "test", []);
        Assert.Equal(0, CardControl.ControllerOf(world, drone));
        Assert.Equal(Cards.Title(face), EffectiveCards.Title(drone, Cards));
    }

    [Rule("rr:player-elimination.4")]
    [Fact]
    public void EliminationTransfersTheDroneCopyAndKeepsItsFacingAndEnvironmentModifiers()
    {
        // Engaged minions pass to the next player clockwise, remaining in play.
        var world = Board();
        var environment = world.CreateCard("01140", world.AreaOf(DeckType.EnvironmentArea));
        world.CreateCard("01142", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, host: environment.ObjectId));
        world.CreateCard("01089", world.Seats[0].Deck);
        world.CreateCard("01089", world.Seats[0].Deck);
        var drone = Engage(world);
        var copy = drone.InstanceState;
        drone.TakeDamage(1);
        Assert.Equal(2, DamagePlacement.Health(world, Cards, drone));

        Elimination.Eliminate(world, Cards, 0, "test", []);

        Assert.Equal(PlayArea.Of(1), drone.Area.PlayArea);
        Assert.False(drone.FaceUp);
        Assert.Same(copy, drone.InstanceState);
        Assert.Equal(1, drone.Damage);
        Assert.Equal(2, DamagePlacement.Health(world, Cards, drone));
        Assert.Equal(2, StateFields.Modified(world, drone, "attack", Cards, world.Players));
        Assert.Equal(0, drone.Owner);
        Assert.Equal(World.Scenario, CardControl.ControllerOf(world, drone));
    }

    [Rule("rr:damage.step.8")]
    [Rule("rr:response.1")]
    [Fact]
    public void TheAuthoredEnvironmentResponseUsesTheDefeatedCopyAfterDeparture()
    {
        // Damage step 8 discards the defeated minion; a response resolves after
        // its condition. 01140: "After a facedown Drone minion is defeated,
        // place that card in its owners discard pile."
        var world = Board();
        var environment = world.CreateCard("01140", world.AreaOf(DeckType.EnvironmentArea));
        world.CreateCard("01089", world.Seats[0].Deck);
        world.CreateCard("01089", world.Seats[0].Deck);
        var drone = Engage(world);
        World.MoveToTop(drone, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        Agendas.Happening(world);
        var occurrence = world.Agenda.Occurrence!;
        var events = new List<GameEvent>();
        Assert.True(DamagePlacement.Deal(world, Cards, world.Seats[1].IdentityCard, drone, 1, "test", "Damage", events));
        Assert.Null(drone.InstanceState.Profile);
        Assert.Equal(0, drone.Damage);
        Assert.Equal(DeckType.DiscardPile, drone.Area.Type);

        var response = Assert.Single(world.Abilities.Waiting(world, occurrence, WindowKind.Response),
            option => option.Card == environment.ObjectId);
        events.AddRange(world.Abilities.Resolve(world, occurrence, response, [], []));

        Assert.Equal(PlayArea.Of(0), drone.Area.PlayArea);
        Assert.Same(drone, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0).Cards[^1]);
        Assert.Contains(events.OfType<CardsMoved>(), moved => moved.Verb == "Return_To_Discard" && moved.Cards.Any(landing => landing.Card == drone.ObjectId));
        World.MoveToTop(drone, world.Seats[0].Hero);
        Assert.DoesNotContain(world.Abilities.Waiting(world, occurrence, WindowKind.Response),
            option => option.Card == environment.ObjectId);
    }

    private static Card Engage(World world) =>
        FacedownMinions.EngageTop(world, 0, AuthoredCards.DroneProfile, "test", "Engage", [])!;

    [Fact]
    public void ATemporaryIdentitySuppressesAndThenRestoresTheHeldCardsResourcesAndText()
    {
        var world = Board();
        world.Seats[0].IdentityCard.TurnTo("01001b");
        world.Seats[0].IdentityCard.TakeDamage(2);
        var held = world.CreateCard(AuthoredCards.AuntMay, world.Seats[0].Deck);
        string printed = Resources.GeneratedBy(held.FaceId, Cards);
        Assert.NotEmpty(printed);
        var drone = Engage(world);
        Assert.Empty(world.Abilities.ResourcesGeneratedBy(world, drone, null));
        Assert.DoesNotContain(world.ActionAbilities.Actions(world, 0), option => option.Card == drone.ObjectId);
        Discard.Card(world, drone, "test", []);
        Assert.Equal(printed, world.Abilities.ResourcesGeneratedBy(world, drone, null));
        World.MoveToTop(drone, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        Assert.Contains(world.ActionAbilities.Actions(world, 0), option => option.Card == drone.ObjectId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AnUnresolvedResponseCannotReuseFacingOrAnEndedIncarnation(bool reenter)
    {
        var world = Board();
        var environment = world.CreateCard("01140", world.AreaOf(DeckType.EnvironmentArea));
        world.CreateCard("01089", world.Seats[0].Deck);
        world.CreateCard("01089", world.Seats[0].Deck);
        var drone = Engage(world);
        if (!reenter) drone.TurnFaceUp();
        Agendas.Happening(world);
        var occurrence = world.Agenda.Occurrence!;
        Assert.True(DamagePlacement.Deal(world, Cards, world.Seats[0].IdentityCard, drone, 1, "test", "Damage", []));
        if (reenter)
        {
            Assert.Contains(world.Abilities.Waiting(world, occurrence, WindowKind.Response),
                option => option.Card == environment.ObjectId);
            World.MoveToTop(drone, world.Seats[0].Deck);
            Assert.Same(drone, Engage(world));
            Discard.Card(world, drone, "test", []);
        }
        Assert.DoesNotContain(world.Abilities.Waiting(world, occurrence, WindowKind.Response),
            option => option.Card == environment.ObjectId);
    }

    [Rule("rr:removed-from-the-game.2")]
    [Fact]
    public void AnUnresolvedDefeatResponseDoesNotReturnACardRemovedFromTheGame()
    {
        // "A card that has been removed from the game cannot reenter the game through any means."
        var world = Board();
        var environment = world.CreateCard("01140", world.AreaOf(DeckType.EnvironmentArea));
        world.CreateCard("01089", world.Seats[0].Deck);
        world.CreateCard("01089", world.Seats[0].Deck);
        var drone = Engage(world);
        Agendas.Happening(world);
        var occurrence = world.Agenda.Occurrence!;
        Assert.True(DamagePlacement.Deal(world, Cards, world.Seats[0].IdentityCard, drone, 1, "test", "Damage", []));
        Assert.Contains(world.Abilities.Waiting(world, occurrence, WindowKind.Response),
            option => option.Card == environment.ObjectId);
        World.MoveToTop(drone, world.AreaOf(DeckType.RemovedArea));
        Assert.DoesNotContain(world.Abilities.Waiting(world, occurrence, WindowKind.Response),
            option => option.Card == environment.ObjectId);
        Assert.Equal(DeckType.RemovedArea, drone.Area.Type);
    }

    private static World Board()
    {
        var world = new World(Cards, 2) { Abilities = AuthoredCards.Runner() };
        for (int i = 0; i < 2; i++)
        {
            var seat = world.CreateSeat($"p{i}");
            seat.IdentityCard = world.CreateCard(i == 0 ? "01001a,01001b" : "01010a,01010b", seat.Hero);
        }
        return world;
    }
}
