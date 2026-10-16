using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Content.Tests.Play;

internal static class PreparedCostPayment
{
    internal static AbilityPaymentResult Commit(World world, AbilityCostPayment payment,
        ICardCounterPools pools, List<GameEvent> events)
    {
        AbilityPaymentResult result = default!;
        var occurrence = new Occurrence(0, [Steps.TurnAction]);
        world.Agenda.Executions.RunBatch(occurrence, owner =>
        {
            result = payment.Commit(owner, pools, Steps.TurnAction, events);
            return result.Suspended ? owner.Yield(events) : owner.Finish(events);
        });
        return result;
    }
}
