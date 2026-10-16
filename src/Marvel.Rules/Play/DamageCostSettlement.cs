using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Settles simultaneous damage costs once per recipient incarnation in payment order.</summary>
public static class DamageCostSettlement
{
    /// <summary>Validate the entire receipt set before opening any resulting defeat window.</summary>
    public static void Settle(World world, AbilityExecution owner,
        IReadOnlyList<DamageCostReceipt> receipts, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(receipts);
        world.Agenda.Executions.RequireActive(owner);
        if (receipts.Distinct().Count() != receipts.Count)
            throw new InvalidOperationException("a damage cost receipt cannot be settled twice");
        var validated = receipts.Select(receipt =>
            (Receipt: receipt, Damage: receipt.ValidateSettlement(world, owner))).ToArray();
        // rr:cost.5: multiple costs are paid simultaneously. Two damage components
        // paid to the same character create one imminent defeat after placement.
        // GroupBy retains first appearance order; enumeration never chooses by a hash key.
        foreach (var group in validated.GroupBy(item => (item.Damage.Target, item.Damage.Incarnation)))
        {
            var first = group.First();
            var combined = first.Damage with
            {
                Dealt = group.Sum(item => item.Damage.Dealt),
                Taken = group.Sum(item => item.Damage.Taken),
            };
            var result = first.Receipt.BeginSettlement(combined, events);
            foreach (var item in group) item.Receipt.AcceptSettlement(result);
        }
    }
}
