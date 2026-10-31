using Marvel.Content;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class GameAreaProjectionTests
{
    private static readonly CardCatalog Facts =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void AnOrdinaryTableNamesTheOneGameAreaForEverySeatAndCard()
    {
        var world = new World(Facts, 2, 42);
        for (int seat = 0; seat < 2; seat++)
            world.CreateSeat($"p{seat}").IdentityCard = world.CreateCard("01001a", world.Seats[seat].Hero);
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));

        var result = WorldProjection.For(world, null, [], new PermissiveVisibilityPolicy().Authorize(null, 2));

        int only = Assert.Single(result.World.GameAreas).Id;
        Assert.All(result.World.Players, player => Assert.Equal(only, player.GameArea));
        Assert.Equal(only, Located(result, villain).GameArea);
        Assert.Equal(only, Located(result, world.Seats[1].IdentityCard).GameArea);
    }

    [Fact]
    public void ASeparatedTableNamesEachLocalGameAreaAndLeavesTheCentralCardInNone()
    {
        // pack:mc11:areas: each stage 3A is placed "directly in front of your play
        // area" in your own game area; stage 2B "remains in play in a central
        // location [...] though it is not part of any other game area".
        var world = new World(Facts, 2, 42);
        var locals = new Card[2];
        for (int seat = 0; seat < 2; seat++)
        {
            world.CreateSeat($"p{seat}").IdentityCard = world.CreateCard("01001a", world.Seats[seat].Hero);
            world.Join(PlayArea.Of(seat), world.CreateGameArea(), "prepared split", []);
            locals[seat] = world.CreateCard(seat == 0 ? "11002" : "11003", world.AreaOf(DeckType.VillainArea, PlayArea.Of(seat)));
        }
        var central = world.CreateCard("11008a,11008b", world.AreaOf(DeckType.MainSchemesArea));
        world.Detach(PlayArea.Villains, "prepared split", []);

        var result = WorldProjection.For(world, null, [], new RestrictedVisibilityPolicy(0).Authorize(null, 2));

        for (int seat = 0; seat < 2; seat++)
        {
            int id = world.GameAreaOf(PlayArea.Of(seat))!.Id;
            Assert.Equal(id, result.World.Players[seat].GameArea);
            Assert.Equal(id, Located(result, locals[seat]).GameArea);
        }
        Assert.NotEqual(result.World.Players[0].GameArea, result.World.Players[1].GameArea);
        Assert.Null(Located(result, central).GameArea);
    }

    private static CardLocationDescriptor Located(VisibleResult result, Card card) =>
        Assert.Single(result.World.Areas.SelectMany(area => area.Cards), item => item.Id == card.ObjectId).Location!;
}
