using Marvel.View;

namespace Marvel.Godot;

/// <summary>Paces an already accepted response without simulating a past board.</summary>
internal sealed class EventActivitySequence
{
    private double elapsed;
    private IReadOnlyList<EventPresentation> beats = [];
    internal IReadOnlyList<EventPresentation> Entries { get; private set; } = [];
    internal int Index { get; private set; } = -1;
    internal int BeatCount => beats.Count;
    internal bool Active { get; private set; }
    internal double Elapsed => elapsed;
    internal double Onset => EventActivityTiming.Onset(Current);
    internal EventPresentation? Current => Index >= 0 ? beats[Index] : null;

    // Completed attacks remain chronology entries, not a second attack animation.
    internal EventPresentation? Attention => Active && Current?.Activity?.Operation != "Attack_Completed"
        ? Current : null;

    internal void Start(IReadOnlyList<EventPresentation> entries, bool animate)
    {
        Entries = entries.ToArray();
        beats = EventActivityBeats.From(Entries);
        Index = beats.Count == 0 ? -1 : 0;
        elapsed = 0;
        Active = animate && beats.Count > 0;
        if (!Active) Complete();
    }

    internal bool Advance(double seconds)
    {
        if (!Active || Current is null) return false;
        elapsed += Math.Max(0, seconds);
        if (elapsed < EventActivityTiming.Duration(Current)) return false;
        // A slow frame must not skip unread beats to catch up with wall time.
        elapsed = 0;
        if (Index + 1 < beats.Count) Index++;
        else Active = false;
        return true;
    }

    internal void Complete()
    {
        Active = false;
        Index = beats.Count - 1;
        elapsed = 0;
    }

    internal void Clear() => Start([], animate: false);

}
