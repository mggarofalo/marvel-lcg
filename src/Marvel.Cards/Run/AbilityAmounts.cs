namespace Marvel.Cards.Run;

/// <summary>Shared arithmetic for accumulated ability amounts.</summary>
internal static class AbilityAmounts
{
    internal static long SaturatingSum(long own, IEnumerable<long> rest)
    {
        foreach (long amount in rest)
        {
            own = amount > long.MaxValue - own ? long.MaxValue : own + amount;
        }
        return own;
    }

    internal static long SaturatingMultiply(long left, long right) =>
        left <= 0 || right <= 0 ? 0
        : left > long.MaxValue / right ? long.MaxValue
        : left * right;
}
