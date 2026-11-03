using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Rules.Tests.State;

/// <summary>
/// A read of the world allocates nothing: an area's id is its creation order,
/// and <c>AreaRef.Id</c> carries it on the event wire.
/// </summary>
/// <remarks>
/// The rules are silent on bookkeeping identities; that a read answers rather
/// than allocates is the engine's choice.
/// </remarks>
public sealed class WorldReadTests
{
    [Fact]
    public void ADigestLooksAtAPlaceWithoutMakingIt()
    {
        var (world, abilities) = Board();
        int made = world.Areas.Count;

        _ = world.Digest();

        Assert.True(abilities.Looked > 0);
        Assert.Equal(made, world.Areas.Count);
        Assert.Equal(made, world.AreaOf(DeckType.VictoryDisplay).Id);
    }

    [Fact]
    public void AReadAnswersAPlaceNotYetMadeAsTheSameEmptyPlace()
    {
        var (world, _) = Board();
        int made = world.Areas.Count;

        using (world.BeginRead())
        {
            var first = world.AreaOf(DeckType.VictoryDisplay);

            Assert.Same(first, world.AreaOf(DeckType.VictoryDisplay));
            Assert.Equal(-1, first.Id);
            Assert.Empty(first.Cards);
            Assert.Null(world.FindArea(DeckType.VictoryDisplay));
        }

        Assert.Equal(made, world.Areas.Count);
    }

    [Fact]
    public void AReadFindsAPlaceTheWorldHasMade()
    {
        var (world, _) = Board();
        var display = world.AreaOf(DeckType.VictoryDisplay);

        using var read = world.BeginRead();

        Assert.Same(display, world.AreaOf(DeckType.VictoryDisplay));
        Assert.Same(display, world.FindArea(DeckType.VictoryDisplay));
    }

    [Theory]
    [InlineData("area")]
    [InlineData("card")]
    [InlineData("game area")]
    public void AReadRefusesToAllocate(string allocation)
    {
        var (world, _) = Board();
        var hand = world.Seats[0].Hand;
        using var read = world.BeginRead();

        var refused = Assert.Throws<InvalidOperationException>(() =>
        {
            _ = allocation switch
            {
                "area" => (object)world.CreateArea(DeckType.VictoryDisplay),
                "card" => world.CreateCard("hero", hand),
                _ => world.CreateGameArea(),
            };
        });

        Assert.StartsWith("a read of the world tried to make", refused.Message, StringComparison.Ordinal);
        Assert.Single(world.GameAreas);
    }

    [Fact]
    public void AnInnerReadClosingLeavesTheOuterReadOpen()
    {
        var (world, _) = Board();
        int made = world.Areas.Count;

        using (world.BeginRead())
        {
            using (world.BeginRead())
            {
            }

            Assert.Equal(-1, world.AreaOf(DeckType.VictoryDisplay).Id);
        }

        Assert.Equal(made, world.Areas.Count);
    }

    [Fact]
    public void AReadForgetsItsUnmadePlacesWhenItCloses()
    {
        var (world, _) = Board();
        Area unmade;
        using (world.BeginRead())
        {
            unmade = world.AreaOf(DeckType.VictoryDisplay);
        }

        var display = world.AreaOf(DeckType.VictoryDisplay);

        Assert.NotSame(unmade, display);
        Assert.Same(display, world.Areas[^1]);
    }

    [Fact]
    public void ACardMovedIntoAnUnmadePlaceAndBackLeavesTheReadClean()
    {
        var (world, _) = Board();
        var hero = world.Seats[0].IdentityCard;
        var home = hero.Area;

        using (world.BeginRead())
        {
            World.MoveToTop(hero, world.AreaOf(DeckType.VictoryDisplay));
            Assert.Same(world.AreaOf(DeckType.VictoryDisplay), hero.Area);
            World.MoveToTop(hero, home);
        }

        Assert.Same(home, hero.Area);
    }

    [Fact]
    public void AReadThatLeavesACardInAnUnmadePlaceRaises()
    {
        var (world, _) = Board();
        var hero = world.Seats[0].IdentityCard;
        var read = world.BeginRead();
        World.MoveToTop(hero, world.AreaOf(DeckType.VictoryDisplay));

        var refused = Assert.Throws<InvalidOperationException>(read.Dispose);

        Assert.Contains("VictoryDisplay", refused.Message, StringComparison.Ordinal);
    }

    private static (World World, LookingConstants Abilities) Board()
    {
        var world = new World(new CardValueFacts(), players: 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("hero", seat.Hero);
        var abilities = new LookingConstants();
        world.Abilities = abilities;
        return (world, abilities);
    }

    /// <summary>A constant ability that counts the victory display, as a card's text might.</summary>
    private sealed class LookingConstants : NoCardAbilities
    {
        public int Looked { get; private set; }

        public override IReadOnlyList<ContinuousEffect> Constant(World world, Card card)
        {
            Looked += world.AreaOf(DeckType.VictoryDisplay).Cards.Count + 1;
            return [];
        }
    }
}
