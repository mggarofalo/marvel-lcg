using Marvel.Rules.State;

namespace Marvel.Rules.Timing;
internal sealed class DepartureScope(HashSet<int> departing, IReadOnlyList<int> cards)
    : IDisposable
{
    private bool disposed;

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        foreach (int card in cards)
        {
            departing.Remove(card);
        }
        disposed = true;
    }
}
