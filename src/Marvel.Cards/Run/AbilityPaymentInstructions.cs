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
    internal sealed record Spend(string Required, long? Energy = null, int Generic = 0) : Step
    {
        internal Marvel.Rules.Prompts.CostOption? Price { get; init; }
        internal ImmutableArray<ResourceAllocation> Allocations { get; init; }
    }
    internal sealed record DiscardHand(Area Hand, ImmutableArray<BoundCard> Cards) : Step;
    internal sealed record RandomDiscardHand(Area Hand, ImmutableArray<BoundCard> Cards, int Count) : Step;
    internal sealed record ExhaustSelected(ImmutableArray<BoundCard> Cards, bool CaptureBasicUsePower = false) : Step;
    internal sealed record TakeDamage(BoundCard Target, long Amount) : Step;
    internal sealed record Exhaust(BoundCard Target) : Step;
    internal sealed record DiscardBound(BoundCard Target) : Step;
    internal sealed record ReturnBound(BoundCard Target) : Step;
    internal sealed record DeckCost(Marvel.Cards.Dsl.AbilityCost Cost) : Step;
    internal sealed record ConsumeCampaignBound(BoundCard Target) : Step;
    internal sealed record RemoveCounters(BoundCard Target, string Counter, long Count) : Step;
    internal sealed record Heal(BoundCard Target, long Amount) : Step;
    internal sealed record DealDamage(BoundCard Target, long Amount) : Step;
}
