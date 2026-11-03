using Marvel.Content;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

/// <summary>A projection reads the published game and makes nothing in it.</summary>
/// <remarks>
/// Engine choice: an area id is its creation order and reaches the event wire, so an area a
/// projection made would renumber a later event.
/// </remarks>
public sealed class ProjectionReadPurityTests
{
    private static readonly CardCatalog Facts =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void AProjectionLooksAtAPlaceWithoutMakingIt()
    {
        var world = new World(Facts, 1, 42);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        var abilities = new LookingConstants();
        world.Abilities = abilities;
        int made = world.Areas.Count;
        var scope = new RestrictedVisibilityPolicy(0).Authorize(null, 1);

        _ = WorldProjection.For(world, null, [], scope);
        _ = WorldProjection.ForEvents(world, scope).WithEvents([]);

        Assert.True(abilities.Looked > 0);
        Assert.Equal(made, world.Areas.Count);
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
