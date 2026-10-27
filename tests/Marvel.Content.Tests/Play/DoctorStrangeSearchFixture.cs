using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual programs on nonpublic prepared boards, outside product admission.</summary>
internal static class DoctorStrangeSearchFixture
{
    internal static World Board()
    {
        var world = ThorActualFixture.Board("06001a", 2);
        var seat = world.Seats[1];
        World.MoveToTop(seat.IdentityCard, world.AreaOf(DeckType.RemovedArea));
        seat.IdentityCard = world.CreateCard("09001b,09001a", seat.Hero);
        var aside = world.CreateArea(DeckType.AsideDeck, 1, PlayArea.Of(1));
        var cards = Enumerable.Range(32, 5).Select(id => world.CreateCard($"090{id}", aside)).ToArray();
        var invocation = InvocationDeck.Initialize(world, 1, cards, "test setup", []);
        World.MoveToTop(cards.Single(c => c.FaceId == "09032"), invocation.Discard);
        world.Abilities = AuthoredCards.Runner();
        return world;
    }

    internal static Card Sanctum(World world) => world.CreateCard("09008",
        world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 0));

    internal static Area Discard(World world, int player = 1) =>
        world.AreaOf(DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player);

    internal static void ClearDeck(World world)
    {
        foreach (var card in world.Seats[1].Deck.Cards.ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
    }

    internal static void Activate(World world, Card source)
    {
        var runner = world.Abilities!;
        var action = Xunit.Assert.Single(runner.Actions(world, 1), a => a.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
    }
}
