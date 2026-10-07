using Marvel.View;

namespace Marvel.Godot;

/// <summary>Retains one authorized response for playback and reversible visual navigation.</summary>
internal sealed class EventPlaybackSequence
{
    private EventPresentation[] entries = [];
    private bool restartOnPlay;
    internal int Index { get; private set; } = -1;
    internal int Count => entries.Length;
    internal EventPresentation this[int index] => entries[index];
    internal bool Playing { get; private set; }
    internal EventPresentation? Current => Index >= 0 ? entries[Index] : null;

    internal void Replace(IReadOnlyList<EventPresentation> value, bool animate)
    {
        entries = value.ToArray();
        Index = entries.Length == 0 ? -1 : animate ? 0 : EventCuePlanner.SettledCueIndex(entries);
        restartOnPlay = !animate;
        Playing = animate && entries.Length > 0;
    }

    internal bool Advance()
    {
        if (!Playing) return false;
        if (Index + 1 < entries.Length)
        {
            Index++;
            return true;
        }
        Playing = false;
        restartOnPlay = true;
        return false;
    }

    internal void Move(int delta)
    {
        Playing = false;
        restartOnPlay = false;
        if (entries.Length > 0) Index = Math.Clamp(Index + delta, 0, entries.Length - 1);
    }

    internal void Pause() => Playing = false;

    internal void Resume()
    {
        if (entries.Length == 0) return;
        if (restartOnPlay || Index == entries.Length - 1) Index = 0;
        restartOnPlay = false;
        Playing = true;
    }

    internal void Finish()
    {
        Playing = false;
        Index = EventCuePlanner.SettledCueIndex(entries);
        restartOnPlay = true;
    }

    internal void Clear() => Replace([], false);
}
