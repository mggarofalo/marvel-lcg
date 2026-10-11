using Marvel.View;

namespace Marvel.Godot;

/// <summary>Paces an already accepted response without simulating a past board.</summary>
internal sealed class EventActivitySequence
{
    private double elapsed;
    internal IReadOnlyList<EventPresentation> Entries { get; private set; } = [];
    internal int Index { get; private set; } = -1;
    internal bool Active { get; private set; }
    internal EventPresentation? Current => Index >= 0 ? Entries[Index] : null;

    internal void Start(IReadOnlyList<EventPresentation> entries, bool animate)
    {
        Entries = entries.ToArray();
        Index = Entries.Count == 0 ? -1 : 0;
        elapsed = 0;
        Active = animate && Entries.Count > 0;
        if (!Active) Complete();
    }

    internal bool Advance(double seconds)
    {
        if (!Active || Current is null) return false;
        elapsed += Math.Max(0, seconds);
        if (elapsed < Duration(Current)) return false;
        // A slow frame must not skip unread beats to catch up with wall time.
        elapsed = 0;
        if (Index + 1 < Entries.Count) Index++;
        else Active = false;
        return true;
    }

    internal void Complete()
    {
        Active = false;
        Index = Entries.Count - 1;
        elapsed = 0;
    }

    internal void Clear() => Start([], animate: false);

    private static double Duration(EventPresentation entry) =>
        entry.Motion is EventMotionKind.Attack or EventMotionKind.Flip or EventMotionKind.Status ? 1.8 : 1.2;
}
