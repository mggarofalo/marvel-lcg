using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Obligation helpers over the Kang board; Kang's public admission stays closed.</summary>
internal static class KangObligationFixture
{
    internal static Card Obligation(World world, string face, int player) =>
        world.CreateCard(face, world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(player)));

    /// <summary>Turns the seat to its alter-ego face: Carol Danvers at seat 0, Peter Parker otherwise.</summary>
    internal static void AlterEgo(World world, int player) =>
        world.Seats[player].IdentityCard.TurnTo(player == 0 ? "01010b" : "01001b");

    internal static Card InHand(World world, string face, int player) =>
        world.CreateCard(face, world.Seats[player].Hand);

    internal static Card InDiscard(World world, string face, int player) =>
        world.CreateCard(face, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(player), cardOwner: player));

    internal static bool Offers(RedSkullFixture f, Card card, int player) =>
        f.Runner.Actions(f.World, player).Any(action => action.Card == card.ObjectId);

    /// <summary>Triggers the one action on <paramref name="card"/>, paying with the chosen hand cards.</summary>
    internal static List<GameEvent> Act(RedSkullFixture f, Card card, int player, params Card[] discard)
    {
        var action = Assert.Single(f.Runner.Actions(f.World, player), offer => offer.Card == card.ObjectId);
        var events = f.Runner.ExecuteAction(f.World,
            new PlayerAction(action, [], [.. discard.Select(chosen => chosen.ObjectId)])).Events.ToList();
        events.AddRange(f.Finish());
        return events;
    }

    internal static IReadOnlyList<Card> Beneath(World world, Card host) => BeneathCards.Of(world, host);
}
