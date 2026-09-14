namespace Marvel.Godot;

/// <summary>Invalidates deferred and input callbacks when their board leaves the tree.</summary>
internal sealed class BoardRenderLifetime
{
    private readonly InteractionGeneration generation = new();
    private bool disposed;

    internal int Current => generation.Current;
    internal int Advance() => generation.Advance();

    internal bool IsCurrent(int candidate) => !disposed && candidate == generation.Current;

    internal void Dispose(BoardRenderResult? rendered)
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        generation.Advance();
        if (rendered is not null)
        {
            rendered.IsCurrent = static () => false;
        }
    }
}
