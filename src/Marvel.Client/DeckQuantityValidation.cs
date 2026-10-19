namespace Marvel.Client;

/// <summary>Validates the bounded public quantity-offer shape before rendering.</summary>
internal static class DeckQuantityValidation
{
    internal static bool Complete(IReadOnlyDictionary<string, int[]>? quantities)
    {
        return quantities is { Count: <= 1000 } && quantities.All(CompleteQuantity);
    }

    private static bool CompleteQuantity(KeyValuePair<string, int[]> pair)
    {
        return !string.IsNullOrWhiteSpace(pair.Key) && pair.Value is { Length: <= 4 }
            && pair.Value.All(value => value is >= 0 and <= 3) && pair.Value.Distinct().Count() == pair.Value.Length;
    }

}
