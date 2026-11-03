using System.Text.Json;
using Marvel.Content;
using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class ProjectedWorldTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public void OneProjectionDescribesEachEventListAsAFreshProjectionWould(int viewer)
    {
        // Synthetic disclosure fixture: a public reveal of the owner's identity and a
        // flip naming a card in the other seat's deck. No card behavior is asserted.
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 2, 5489);
        Seat owner = world.CreateSeat("Owner");
        Seat other = world.CreateSeat("Other");
        Card identity = world.CreateCard("01001b,01001a", owner.Hero);
        Card hidden = world.CreateCard("01088", other.Deck);
        CardsFlipped reveal = new([identity.ObjectId], true)
        {
            SubjectFaces = new Dictionary<int, string> { [identity.ObjectId] = identity.FaceId },
            PublicFaces = new Dictionary<int, string> { [identity.ObjectId] = identity.FaceId },
        };
        CardsFlipped concealed = new([hidden.ObjectId], true)
        {
            SubjectFaces = new Dictionary<int, string> { [hidden.ObjectId] = hidden.FaceId },
        };
        ViewScope scope = viewer < 0
            ? ViewScope.None
            : new RestrictedVisibilityPolicy(viewer).Authorize(null, 2);
        ProjectedWorld projected = WorldProjection.ForEvents(world, scope, activePlayer: 1);

        GameEvent[][] lists = [[reveal], [], [concealed], [reveal, concealed]];
        foreach (GameEvent[] events in lists)
        {
            Assert.Equal(
                JsonSerializer.Serialize(WorldProjection.For(world, null, events, scope, 1).World),
                JsonSerializer.Serialize(projected.WithEvents(events)));
        }

        Assert.Equal(["01001b"], projected.WithEvents([reveal]).EventFaces.Keys);
        Assert.Empty(projected.WithEvents([]).EventFaces);
    }
}
