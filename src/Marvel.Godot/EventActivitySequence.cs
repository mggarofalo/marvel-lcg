using Marvel.View;

namespace Marvel.Godot;

/// <summary>Paces an already accepted response without simulating a past board.</summary>
internal sealed class EventActivitySequence
{
    private double elapsed;
    private double beatDuration;
    internal IReadOnlyList<EventPresentation> Entries { get; private set; } = [];
    internal int Index { get; private set; } = -1;
    internal bool Active { get; private set; }
    internal EventPresentation? Current => Index >= 0 ? Entries[Index] : null;

    internal void Start(IReadOnlyList<EventPresentation> entries, bool animate)
    {
        Entries = entries.ToArray();
        Index = Entries.Count == 0 ? -1 : 0;
        elapsed = 0;
        beatDuration = Entries.Count <= 1 ? 0.4 : Math.Min(0.6, 2.4 / Entries.Count);
        Active = animate && Entries.Count > 0;
        if (!Active) Complete();
    }

    internal bool Advance(double seconds)
    {
        if (!Active || Current is null) return false;
        elapsed += Math.Max(0, seconds);
        if (elapsed < beatDuration) return false;
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

}
