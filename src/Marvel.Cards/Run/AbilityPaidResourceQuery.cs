namespace Marvel.Cards.Run;

/// <summary>Chooses the committed payment belonging to the current ability's source.</summary>
internal static class AbilityPaidResourceQuery
{
    internal static bool Includes(AbilityExpressionContext context, char resource)
    {
        // Entry responses on the played card inspect its original price receipt,
        // independently of resources paid for the response itself.
        if (context.Occurrence.CardPlayPayment is { } entryPayment
            && entryPayment.Card == context.Source.ObjectId)
            return entryPayment.Includes(context.Source, context.Player, resource);

        return context.Payment.Contains(resource)
            || context.World.Effects.Active().Any(effect =>
                effect.Card == context.Source.ObjectId
                && string.Equals(effect.Kind, "paid:" + resource, StringComparison.Ordinal));
    }
}
