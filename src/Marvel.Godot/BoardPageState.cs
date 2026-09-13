namespace Marvel.Godot;

/// <summary>Stable presentation-only pages for bounded tabletop collections.</summary>
public sealed class BoardPageState
{
    private readonly Dictionary<int, int> pages = [];

    /// <summary>Returns the clamped page for a collection.</summary>
    public int Page(int key, int pageCount)
    {
        int page = pages.GetValueOrDefault(key);
        return Math.Clamp(page, 0, Math.Max(0, pageCount - 1));
    }

    /// <summary>Moves a collection to a clamped page.</summary>
    public void SetPage(int key, int page, int pageCount) =>
        pages[key] = Math.Clamp(page, 0, Math.Max(0, pageCount - 1));

    /// <summary>Forgets presentation-only pages when the table session ends.</summary>
    public void Clear() => pages.Clear();
}
