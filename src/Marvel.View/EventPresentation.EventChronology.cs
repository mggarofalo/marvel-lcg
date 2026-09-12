using System.Globalization;
using System.Text;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;

namespace Marvel.View;

/// <summary>One visibility-safe, human-readable entry in the game chronology.</summary>

/// <summary>An ordered, resettable chronology of semantic events across responses.</summary>
public sealed class EventChronology
{
    private const int MaximumEntries = 100;
    private readonly List<EventPresentation> entries = [];

    /// <summary>All entries in response and event order.</summary>
    public IReadOnlyList<EventPresentation> Entries => entries;

    /// <summary>Clears the prior game and records one response's events.</summary>
    public void Reset(IReadOnlyList<GameEvent> events, WorldDescriptor world)
    {
        entries.Clear();
        Append(events, world);
    }

    /// <summary>Clears the prior game and records already-presented entries.</summary>
    public void Reset(IEnumerable<EventPresentation> presented)
    {
        entries.Clear();
        Append(presented);
    }

    /// <summary>Appends one response's events without changing earlier entries.</summary>
    public void Append(IReadOnlyList<GameEvent> events, WorldDescriptor world) =>
        Append(EventPresenter.PresentNarrative(events, world));

    /// <summary>Appends already-presented entries and retains the latest readable history.</summary>
    public void Append(IEnumerable<EventPresentation> presented)
    {
        ArgumentNullException.ThrowIfNull(presented);
        entries.AddRange(presented);
        if (entries.Count > MaximumEntries)
        {
            entries.RemoveRange(0, entries.Count - MaximumEntries);
        }
    }
}
