using Marvel.Rules.State;
using Xunit;

namespace Marvel.View.Tests;

internal static class VisibilityFixture
{
    internal static CardDescriptor Hand(WorldDescriptor world, int seat) =>
        Assert.Single(Assert.Single(
            world.Areas,
            area => area.Zone == nameof(DeckType.HandsArea) && area.Owner == seat).Cards);

    internal static CardDescriptor Card(WorldDescriptor world, int id) =>
        Assert.Single(
            world.Areas.SelectMany(area => area.Cards.Concat(area.Removed)),
            card => card.Id == id);

    internal static CardDescriptor Card(WorldDescriptor world, string faceId) =>
        Assert.Single(
            world.Areas.SelectMany(area => area.Cards.Concat(area.Removed)),
            card => card.Face?.Id == faceId);

    internal static World Board()
    {
        var world = new World(new VisibilityFacts(), players: 2, seed: 7);
        Seat zero = world.CreateSeat("Zero");
        Seat one = world.CreateSeat("One");
        world.CreateCard("zero-deck", zero.Deck);
        world.CreateCard("one-deck", one.Deck);
        world.CreateCard("zero-hand", zero.Hand);
        world.CreateCard("one-hand", one.Hand);
        Area allies = world.CreateArea(
            DeckType.AlliesArea, cardOwner: 1, playArea: PlayArea.Of(1));
        world.CreateCard("player-ally", allies);
        Area encounter = world.AreaOf(DeckType.EncounterDeck);
        world.CreateCard("secret-a", encounter);
        world.CreateCard("secret-b", encounter);
        world.CreateCard("public-villain", world.AreaOf(DeckType.VillainArea));
        return world;
    }

}
