using Marvel.View;

namespace Marvel.Godot;

/// <summary>Places an event's visible subjects in the current desktop workspace before its cue appears.</summary>
internal static class EventCueBoardFocus
{
    internal static void Present(Main main, EventPresentation entry)
    {
        main.eventCueSummary.Text = entry.Summary;
        main.boardController.FocusEventAnchors(entry.Anchors);
    }
}
