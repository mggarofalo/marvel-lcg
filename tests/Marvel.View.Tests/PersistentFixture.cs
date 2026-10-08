using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Rules.State;
using Marvel.Tests;

namespace Marvel.View.Tests;

internal static class PersistentFixture
{
    internal static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    internal static readonly AbilityBook Abilities = AbilityCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")));

    internal static World Board(string identity = "01029a")
    {
        var world = new World(Cards, 1, seed: 7) { Abilities = new AbilityRunner(Abilities) };
        Seat seat = world.CreateSeat("P0");
        seat.IdentityCard = world.CreateCard(identity, seat.Hero);
        return world;
    }

    internal static Area Controlled(World world) => world.AreaOf(DeckType.UpgradesArea,
        PlayArea.Of(0), cardOwner: 0, host: world.Seats[0].IdentityCard.ObjectId);

    internal static Card Attach(World world, string face, Card host) => world.CreateCard(face,
        world.AreaOf(DeckType.UpgradesArea, host.Area.PlayArea, host: host.ObjectId));

    internal static WorldDescriptor Visible(World world) => WorldProjection.For(world, null, [],
        new RestrictedVisibilityPolicy(0).Authorize(null, world.Players)).World;

    internal static CardDescriptor Card(World world, Card card) => VisibilityFixture.Card(Visible(world), card.ObjectId);
    internal static CardPersistentDescriptor Facts(World world, Card card) => Card(world, card).Persistent!;
}
