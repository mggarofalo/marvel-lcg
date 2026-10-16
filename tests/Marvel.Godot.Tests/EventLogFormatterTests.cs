using Marvel.Protocol;
using Marvel.Server;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class EventLogFormatterTests
{
    [Fact]
    public void CollapsedAccountKeepsActionOrderAndOffersDetailsWithoutHidingUndo()
    {
        HistoryEntryDescriptor[] actions =
        [
            new(0, "Spider-Man changed form.", ["Peter Parker became Spider-Man."]),
            new(1, "Spider-Man attacked.", ["Rhino took 2 damage."]),
        ];
        string collapsed = EventLogFormatter.FormatActions(actions, [1], "f0a030", new HashSet<int>());
        Assert.DoesNotContain("Peter Parker became", collapsed);
        Assert.DoesNotContain("Rhino took", collapsed);
        Assert.Contains("[url=details:0]> Expand details[/url]", collapsed);
        Assert.Contains("[url=undo:1]Undo to before this action[/url]", collapsed);
        Assert.True(collapsed.IndexOf("changed form", StringComparison.Ordinal)
            < collapsed.IndexOf("attacked", StringComparison.Ordinal));

        string expanded = EventLogFormatter.FormatActions(actions, [1], "f0a030", new HashSet<int> { 1 });
        Assert.Contains("[url=details:1]< Collapse details[/url]", expanded);
        Assert.Contains("Rhino took 2 damage.", expanded);
        Assert.DoesNotContain("Peter Parker became", expanded);
        Assert.DoesNotContain("[url=undo:0]", expanded);
    }

    [Fact]
    public void ActionHistoryIncludesDetailsAndOnlyOfferedUndoLinks()
    {
        HistoryEntryDescriptor[] actions =
        [
            new(0, "Spider-Man changed form.", ["Peter Parker became Spider-Man."]),
            new(1, "Spider-Man attacked.", []),
        ];

        string formatted = EventLogFormatter.FormatActions(actions, [1], "f0a030");

        Assert.Equal(
            "[color=#f0a030]001[/color]  Spider-Man changed form."
            + Environment.NewLine
            + "     Peter Parker became Spider-Man."
            + Environment.NewLine
            + "[color=#f0a030]002[/color]  Spider-Man attacked."
            + Environment.NewLine
            + "     [url=undo:1]Undo to before this action[/url]"
            + Environment.NewLine,
            formatted);
    }

    [Fact]
    public void OpenActionWithoutCompletedHistoryHasProgressCopy()
    {
        Assert.Equal(
            "Action in progress.",
            EventLogFormatter.FormatActions([], [], "f0a030"));
    }

    [Fact]
    public void EventChronologyUsesOneBasedPaddedSequenceNumbers()
    {
        EventPresentation[] entries =
        [
            new("A card entered play.", "Test", [], EventMotionKind.Create),
            new("A card took damage.", "Test", [], EventMotionKind.Damage),
        ];

        string formatted = EventLogFormatter.FormatChronology(entries, "f0a030");

        Assert.Equal(
            "[color=#f0a030]001[/color]  A card entered play."
            + Environment.NewLine
            + "[color=#f0a030]002[/color]  A card took damage."
            + Environment.NewLine,
            formatted);
    }

    [Fact]
    public void ChronologySectionKeepsCurrentEventsVisibleBesideActionHistory()
    {
        EventPresentation[] entries =
        [
            new("Spider-Man changed form.", "Test", [], EventMotionKind.State),
        ];

        string formatted = EventLogFormatter.FormatChronologySection(
            entries, "f0a030");

        Assert.DoesNotContain("EVENT CHRONOLOGY", formatted, StringComparison.Ordinal);
        Assert.Contains("Spider-Man changed form.", formatted, StringComparison.Ordinal);
    }
}
