using Godot;

namespace Marvel.Godot;

/// <summary>Connects the review drawer to the same occurrences and selection as the rail.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "The scene tree owns the native controls.")]
internal sealed class HistoryAccountController(Main main, EventTimelineController timeline)
{
    private HistoryOccurrenceList? list;

    internal void Render()
    {
        if (list is null)
        {
            list = new HistoryOccurrenceList();
            list.Configure(main, timeline);
            main.eventLog.GetParent().AddChild(list);
            main.eventLog.GetParent().MoveChild(list, main.eventLog.GetIndex());
            timeline.SelectionChanged += list.RefreshSelection;
            timeline.DetailRequested += () =>
            {
                list.ExpandDetails();
                TableHistoryDrawer.Open(main);
            };
        }
        main.eventLog.Hide();
        list.Present(timeline.Entries, timeline.Selected);
    }
}
