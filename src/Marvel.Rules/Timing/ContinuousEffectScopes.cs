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

/// <summary>One registered effect and its remaining uses.</summary>
/// <remarks>
/// The effect itself is immutable, because it has to be writable to a save.
/// How much of it is left over is not part of what the card says, so it
/// lives here.
/// </remarks>
internal sealed class Entry(ContinuousEffect effect)
{
    public ContinuousEffect Effect { get; } = effect;

    public int? Remaining { get; set; } = effect.Lasts?.Uses;
}
