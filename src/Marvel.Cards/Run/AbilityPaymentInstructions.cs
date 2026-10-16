using System.Collections.Immutable;
using Marvel.Rules.State;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

/// <summary>Closed payment instructions with the card incarnation accepted at initiation.</summary>
internal static class AbilityPaymentInstructions
{
    internal sealed record BoundCard(Card Card, int Incarnation)
    {
        internal Card? Current => Card.Incarnation == Incarnation ? Card : null;

        internal Card RequireCurrent() => Current ?? throw new RulesNotImplementedException(
            $"card {Card.ObjectId} no longer names the accepted copy paying a required cost");
    }

    internal abstract record Step;
    internal sealed record Spend(string Required, long? Energy = null) : Step;
    internal sealed record DiscardHand(Area Hand, ImmutableArray<BoundCard> Cards) : Step;
    internal sealed record ExhaustSelected(ImmutableArray<BoundCard> Cards) : Step;
    internal sealed record TakeDamage(BoundCard Target, long Amount) : Step;
    internal sealed record Exhaust(BoundCard Target) : Step;
    internal sealed record DiscardBound(BoundCard Target) : Step;
    internal sealed record RemoveCounters(BoundCard Target, string Counter, long Count) : Step;
    internal sealed record Heal(BoundCard Target, long Amount) : Step;
    internal sealed record DealDamage(BoundCard Target, long Amount) : Step;
}
