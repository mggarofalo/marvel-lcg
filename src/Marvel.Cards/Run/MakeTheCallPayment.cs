using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

// Commits the chosen ally's price using the same target-specific quote as the offer.
internal static class MakeTheCallPayment
{
    internal static ResourcePaymentReceipt Spend(World world, IResourceCardAbilities abilities,
        Decision input, Card source, Card ally, int player, List<GameEvent> events)
    {
        var allocations = input.Allocated;
        var price = allocations.Count == 0 ? null : MakeTheCallOffers.Price(
            world, ally, MakeTheCallOffers.Sources(world, player, source, ally, abilities));
        return CardResourcePayments.Spend(world, abilities, new CardResourcePaymentRequest
        {
            Hands = [world.Seats[player].Hand], Paying = input.Spent,
            Cost = Resources.Cost(ally.FaceId, world.Facts, world.Players) ?? 0,
            Required = Resources.Required(world, ally, world.Facts), Itself = source.ObjectId,
            Payer = player, PayingFor = ally, Price = price, Allocations = allocations,
        }, events);
    }
}
