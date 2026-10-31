using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

/// <summary>Reads excess from the exact physical source's completed entry payment.</summary>
internal static class AbilityEntryResourceQuery
{
    internal static long Excess(AbilityExpressionContext context) =>
        Receipt(context) is { } payment ? payment.Excess : 0;

    /// <summary>The overpaid icons usable as energy.</summary>
    /// <remarks>
    /// rr:cost.4.1: generated resources beyond the cost "were not paid for that
    /// cost". rr:wild-resource.1: when a player generates a wild resource "they may
    /// specify which resource type" it is being used as. The payment receipt records
    /// no declaration for an overpaid icon, so the engine chooses to count each
    /// overpaid wild as declared energy. A paid declaration consumes its own printed
    /// type first and a wild icon otherwise.
    /// </remarks>
    internal static long EnergyExcess(AbilityExpressionContext context)
    {
        if (Receipt(context) is not { } payment) return 0;
        var remaining = payment.Generated.GroupBy(icon => icon).ToDictionary(group => group.Key, group => group.Count());
        foreach (char paid in payment.Paid)
        {
            char consumed = paid != 'G' && remaining.GetValueOrDefault(paid) > 0 ? paid : 'G';
            if (remaining.GetValueOrDefault(consumed) > 0) remaining[consumed]--;
        }
        return remaining.GetValueOrDefault('Y') + remaining.GetValueOrDefault('G');
    }

    private static EntryPayment? Receipt(AbilityExpressionContext context)
    {
        // No receipt means no resources were generated for this entry, including free puts.
        if (context.Occurrence.Entry.EntryPayment is not { } payment) return null;
        if (!context.Bindings.SourceBindingIsCurrent(context.Source)) return null;
        return payment.MatchesPending(context.Source, context.Player)
            || payment.MatchesEntered(context.Source, context.Player) ? payment : null;
    }
}
