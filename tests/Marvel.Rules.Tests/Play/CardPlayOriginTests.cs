using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>Synthetic printed permission proves location admission without pretending stored cards are held.</summary>
public sealed class CardPlayOriginTests
{
    [Fact]
    [Rule("rr:player-turn.5")]
    public void StoredEventRequiresExplicitPermissionAndRemainsOutsideTheHand()
    {
        var world = new World(new Printed(), 2);
        world.CreateSeat("first");
        world.CreateSeat("second");
        var host = world.CreateCard("support", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var card = world.CreateCard("event", world.Seats[0].Hand);
        Assert.True(CardPlayOrigin.CanPlayEvent(world, card, 0));
        BeneathCards.Place(world, card, host, true, "synthetic", []);
        Assert.False(CardPlayOrigin.CanPlayEvent(world, card, 0));
        world.Abilities = new Permission();
        string before = world.Digest().Canonical();
        var rng = world.Random.Generator.GetState();
        Assert.True(CardPlayOrigin.CanPlayEvent(world, card, 0));
        Assert.False(CardPlayOrigin.CanPlayEvent(world, card, 1));
        Assert.Same(card, Assert.Single(BeneathCards.Of(world, host)));
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(rng.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rng.Index, world.Random.Generator.GetState().Index);
        World.MoveToTop(card, world.Seats[0].Deck);
        Assert.False(CardPlayOrigin.CanPlayEvent(world, card, 0));
    }

    private sealed class Permission : NoCardAbilities
    {
        public override bool CanPlayFromStorage(World world, Card card, int player) => true;
    }

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId == "event" ? CardKind.Event : CardKind.Support;
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
