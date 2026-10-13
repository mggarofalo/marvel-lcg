namespace Marvel.Godot;

/// <summary>Ordinal navigation and pixel-bounded density; these are presentation choices.</summary>
internal sealed class TimelineViewport
{
    internal int Count { get; private set; }
    internal int Selected { get; private set; } = -1;
    internal double Left { get; private set; }
    internal double Span { get; private set; } = 24;
    internal bool Following { get; private set; } = true;

    internal void SetCount(int count)
    {
        Count = Math.Max(0, count);
        if (Following) Latest();
        else { Selected = Math.Min(Selected, Count - 1); Clamp(); }
    }

    internal void Latest()
    {
        Following = true;
        Selected = Count - 1;
        Left = Math.Max(0, Count - Span);
    }

    internal void Select(int index)
    {
        Selected = Count == 0 ? -1 : Math.Clamp(index, 0, Count - 1);
        Following = Selected == Count - 1;
        if (Selected < Left) Left = Selected;
        if (Selected >= Left + Span) Left = Selected - Span + 1;
        Clamp();
    }

    internal void Pan(double amount)
    {
        Following = false;
        Left += amount;
        Clamp();
    }

    internal void Zoom(double factor, double anchor)
    {
        anchor = Math.Clamp(anchor, 0, 1);
        double pointed = Left + Span * anchor;
        Span = Math.Clamp(Span * factor, 4, Math.Max(24, Count));
        Left = pointed - Span * anchor;
        Clamp();
    }

    internal int At(double fraction) => Count == 0 ? -1
        : Math.Clamp((int)Math.Floor(Left + Span * Math.Clamp(fraction, 0, 1)), 0, Count - 1);

    internal IEnumerable<(int First, int Last, double Fraction)> Bins(double width)
    {
        int slots = Math.Max(1, (int)(width / 10));
        int stride = Math.Max(1, (int)Math.Ceiling(Span / slots));
        int last = Math.Min(Count, (int)Math.Ceiling(Left + Span));
        for (int first = Math.Max(0, (int)Math.Floor(Left)); first < last; first += stride)
            yield return (first, Math.Min(last - 1, first + stride - 1), (first + .5 - Left) / Span);
    }

    private void Clamp() => Left = Math.Clamp(Left, 0, Math.Max(0, Count - Span));
}
