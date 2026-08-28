using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>Facedown player cards that Ultron makes into Drone minions.</summary>
public sealed class FacedownDroneTests
{
    [Rule("rr:base-value")]
    [Rule("rr:engage")]
    [Rule("rr:player-s-play-area.3")]
    [Rule("rr:villain-s-play-area.3")]
    [Fact]
    public void TheTopPlayerCardBecomesThePrintedFacedownDrone()
    {
        // 01140: "Each facedown Drone minion engaged with a player has a base
        // SCH of 1, a base ATK of 1, and a base hit points of 1." The player
        // card underneath prints different values and a different trait so a
        // leak of its face through any runtime answer fails visibly.
        var facts = new Printed();
        var world = Board(facts, players: 2);
        world.CreateCard("bottom", world.Seats[1].Deck);
        var top = world.CreateCard("player-card", world.Seats[1].Deck);
        int originalId = top.ObjectId;
        string originalFace = top.FaceId;
        var events = new List<GameEvent>();

        var drone = Assert.IsType<Card>(
            FacedownDrones.EngageTop(world, 1, "01140", "Create_Drone", events));

        Assert.Same(top, drone);
        Assert.Equal(originalId, drone.ObjectId);
        Assert.Equal(originalFace, drone.FaceId);
        Assert.False(drone.FaceUp);
        Assert.Equal(DeckType.EngagedEnemiesArea, drone.Area.Type);
        Assert.Equal(PlayArea.Of(1), drone.Area.PlayArea);
        Assert.Equal(CardKind.Minion, FacedownDrones.Kind(drone, facts));
        Assert.Equal([FacedownDrones.Trait], Traits.Of(world, drone, facts));
        Assert.Equal(1, FacedownDrones.BaseValue(drone, facts, "SCH", world.Players));
        Assert.Equal(1, StateFields.Modified(world, drone, "attack", facts, world.Players));
        Assert.Equal(1, Damage.Health(world, facts, drone));
        Assert.False(Keywords.IsBoosted(drone, facts, world.Players));
        Assert.Contains(drone, BasicPowers.Attackable(world, facts, 1));

        var record = world.Digest().Cards.Single(card => card.Id == drone.ObjectId);
        Assert.Equal(originalFace, record.Card);
        Assert.False(record.FaceUp);
        Assert.Equal("EngagedEnemiesArea", record.Zone);
        Assert.Equal(1, record.Fields["scheme"]);
        Assert.Equal(1, record.Fields["attack"]);
        Assert.Equal(1, record.Fields["health"]);
        Assert.Equal(1, record.Fields["t_DRONE"]);
        Assert.DoesNotContain("t_AVENGER", record.Fields.Keys);
        Assert.Single(events.OfType<CardsMoved>(), moved => moved.Verb == "Create_Drone");
    }

    [Rule("rr:minion.2")]
    [Rule("rr:discard-pile.1")]
    [Fact]
    public void ADefeatedDroneReturnsFaceupToItsOwnersDiscardPile()
    {
        // 01140: "After a facedown Drone minion is defeated, place that card
        // in its owner's discard pile." Its printed face and printed kind are
        // active again once it has left the engagement area.
        var facts = new Printed();
        var world = Board(facts, players: 1);
        world.CreateCard("bottom", world.Seats[0].Deck);
        var underneath = world.CreateCard("player-card", world.Seats[0].Deck);
        var drone = Assert.IsType<Card>(
            FacedownDrones.EngageTop(world, 0, "01140", "Create_Drone", []));
        Agendas.Happening(world);

        bool defeated = Damage.Deal(
            world, facts, drone, drone, 1, "test", "Deal_Damage", []);

        Assert.True(defeated);
        Assert.Same(underneath, drone);
        Assert.Equal(DeckType.DiscardPile, drone.Area.Type);
        Assert.Equal(PlayArea.Of(0), drone.Area.PlayArea);
        Assert.True(drone.FaceUp);
        Assert.False(FacedownDrones.Is(drone));
        Assert.Equal(CardKind.Ally, FacedownDrones.Kind(drone, facts));
        Assert.Equal(["AVENGER"], Traits.Of(world, drone, facts));
    }

    [Rule("rr:engage.1")]
    [Fact]
    public void EnumerationIsPerPlayerAndInObjectIdOrder()
    {
        // "An engaged minion remains engaged with the same player" until an
        // effect moves it. The helpers therefore read the engagement areas,
        // not owner: a Drone remains its original player's card even if a
        // later effect makes it engage somebody else.
        var facts = new Printed();
        var world = Board(facts, players: 2);
        world.CreateCard("player-card", world.Seats[0].Deck);
        var first = FacedownDrones.EngageTop(world, 0, "test", "Create_Drone", [])!;
        world.CreateCard("player-card", world.Seats[1].Deck);
        var second = FacedownDrones.EngageTop(world, 1, "test", "Create_Drone", [])!;
        world.CreateCard("player-card", world.Seats[0].Deck);
        var third = FacedownDrones.EngageTop(world, 0, "test", "Create_Drone", [])!;

        Assert.Equal(
            [first.ObjectId, second.ObjectId, third.ObjectId],
            FacedownDrones.InPlay(world).Select(card => card.ObjectId));
        Assert.Equal(
            [first.ObjectId, third.ObjectId],
            FacedownDrones.EngagedWith(world, 0).Select(card => card.ObjectId));
        Assert.Equal(
            [second.ObjectId],
            FacedownDrones.EngagedWith(world, 1).Select(card => card.ObjectId));
    }

    private static World Board(Printed facts, int players)
    {
        var world = new World(facts, players);
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("hero", seat.Hero);
        }

        return world;
    }

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "hero" => CardKind.Hero,
            "player-card" => CardKind.Ally,
            "bottom" => CardKind.Event,
            _ => CardKind.Unknown,
        };

        public IReadOnlyList<string> Traits(string faceId) =>
            faceId == "player-card" ? ["AVENGER"] : [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            faceId switch
            {
                "hero" => new Dictionary<string, string> { ["HP"] = "10" },
                "player-card" => new Dictionary<string, string>
                {
                    ["SCH"] = "8", ["ATK"] = "9", ["HP"] = "7", ["Retaliate"] = "3",
                    ["Victory"] = "5",
                },
                _ => new Dictionary<string, string>(),
            };

        public long PrintedValue(
            string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out string? value)
            && long.TryParse(value, out long number)
                ? number
                : fallback;
    }
}
