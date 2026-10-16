using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>The accepted card entry after its resource payment commits.</summary>
internal sealed class CardPaymentEntry
{
    private bool consumed;
    internal required AbilityExecution Execution { get; init; }
    internal required World World { get; init; }
    internal required ICardFacts Facts { get; init; }
    internal required ICardPlayAbilities Abilities { get; init; }
    internal required Seat Seat { get; init; }
    internal required Card Card { get; init; }
    internal required int Incarnation { get; init; }
    internal required AdjustedCardCost Cost { get; init; }
    internal required IReadOnlyList<int> Targets { get; init; }
    internal required ResourcePaymentReceipt Payment { get; init; }

    internal AbilityExecutionResult Apply(World world, List<GameEvent> events)
    {
        if (!ReferenceEquals(World, world) || consumed)
            throw new InvalidOperationException("card entry requires its unused owning game");
        if (!Payment.IsComplete) throw new InvalidOperationException("card entry preceded its payment");
        if (Card.Incarnation != Incarnation)
            throw new InvalidOperationException("the paid card changed incarnation before entry");
        consumed = true;
        CardEntry.Enter(world, Facts, Abilities, Seat, Card, events, Targets);
        CardPayment.UseCostModifiers(world, Cost);
        return Execution.Finish(events);
    }
}
