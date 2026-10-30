namespace Marvel.Cards.Run;

/// <summary>Reads excess from the exact physical source's completed entry payment.</summary>
internal static class AbilityEntryResourceQuery
{
    internal static long Excess(AbilityExpressionContext context)
    {
        // rr:cost.4.1: resources generated in excess "were not paid for that cost".
        // No receipt means no resources were generated for this entry, including free puts.
        if (context.Occurrence.Entry.EntryPayment is not { } payment) return 0;
        if (!context.Bindings.SourceBindingIsCurrent(context.Source)) return 0;
        return payment.MatchesPending(context.Source, context.Player)
            || payment.MatchesEntered(context.Source, context.Player) ? payment.Excess : 0;
    }
}
