using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public abstract class CoreCardTestBase
{
    protected static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    protected static long Modified(World world, Card card, string field) =>
        StateFields.Modified(world, card, field, Cards, world.Players);

    protected static World Hero(string faceId, int players = 1)
    {
        var world = new World(Cards, players);
        for (int player = 0; player < players; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            string identity = player == 0 ? faceId : "01001a";
            seat.IdentityCard = world.CreateCard(identity, seat.Hero);
        }

        return world;
    }
}
