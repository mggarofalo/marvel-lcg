using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Pays an ability cost without applying card-play-only resource bonuses.</summary>
public static class AbilityResourcePayment
{
    /// <summary>Validates the source's resource restrictions before any contribution is committed.</summary>
    public static ResourcePaymentReceipt Spend(World world, IResourceCardAbilities abilities,
        AbilityResourcePaymentRequest request, List<GameEvent> events) =>
        new CardResourcePayment(world, world.Facts, abilities, new CardResourcePaymentRequest
        {
            Hands = [world.Seats[request.Player].Hand], Paying = [.. request.Paying],
            Cost = request.Cost, Required = request.Required, Itself = -1, Payer = request.Player,
            AbilitySource = request.Source,
            Price = request.Price, Allocations = request.Allocations,
        }).Spend(events);
}
