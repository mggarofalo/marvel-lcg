using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

internal sealed class ProjectionScope(
    IReadOnlyDictionary<Area, Card[]> orders) : IDisposable
{
    private bool disposed;

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        foreach (var (area, order) in orders)
        {
            area.Replace(order);
            foreach (var card in order)
            {
                card.ProjectTo(area);
            }
        }
        disposed = true;
    }
}
