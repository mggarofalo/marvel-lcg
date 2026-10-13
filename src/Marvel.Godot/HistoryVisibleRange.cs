namespace Marvel.Godot;

/// <summary>Bounds native row allocation by viewport height, independently of archive length.</summary>
internal static class HistoryVisibleRange
{
    internal const int RowHeight = 80;

    internal static (int First, int Count) At(int total, double offset, double height)
    {
        int first = Math.Clamp((int)(offset / RowHeight), 0, Math.Max(0, total - 1));
        int count = Math.Min(Math.Max(0, total - first), Math.Max(1, (int)Math.Ceiling(height / RowHeight) + 1));
        return (first, count);
    }
}
