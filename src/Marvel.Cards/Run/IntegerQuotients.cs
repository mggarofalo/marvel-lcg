namespace Marvel.Cards.Run;

internal static class IntegerQuotients
{
    internal static long Floor(long dividend, long divisor)
    {
        if (divisor <= 0) throw new InvalidOperationException("compiled floor division requires a positive divisor");
        long quotient = dividend / divisor;
        return dividend % divisor < 0 ? quotient - 1 : quotient;
    }
}
