using Godot;

namespace Marvel.Godot;

/// <summary>A continuously scrollable native chronology with a viewport-sized pool of event cards.</summary>
internal sealed partial class HistoryOccurrenceList : VBoxContainer
{
    private readonly ScrollContainer scroll = new() { Name = "OccurrenceScroll",
        HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, SizeFlagsVertical = SizeFlags.Fill };
    private readonly Control canvas = new() { Name = "Occurrences", SizeFlagsHorizontal = SizeFlags.ExpandFill };
    private readonly List<HistoryOccurrenceRow> rows = [];
    private IReadOnlyList<TimelineOccurrence> entries = [];
    private EventTimelineController timeline = null!;
    private HistoryOccurrenceDetail detail = null!;
    private int selected = -1;
    private int previousCount;
    private bool following = true;

    internal void Configure(Main main, EventTimelineController controller)
    {
        Name = "HistoryOccurrences";
        timeline = controller;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        CustomMinimumSize = new Vector2(0, 260);
        AddChild(scroll);
        scroll.AddChild(canvas);
        detail = new HistoryOccurrenceDetail(main);
        AddChild(detail.Root);
        Resized += FitScroll;
        detail.Root.MinimumSizeChanged += FitScroll;
        scroll.GetVScrollBar().ValueChanged += _ => { following = AtEnd(); RenderRows(); };
        scroll.Resized += RenderRows;
        scroll.GetVScrollBar().Changed += () => Callable.From(KeepSelectionVisible).CallDeferred();
        canvas.Resized += RenderRows;
    }

    internal void Present(IReadOnlyList<TimelineOccurrence> source, int selection)
    {
        bool appended = source.Count > previousCount;
        entries = source;
        previousCount = source.Count;
        selected = selection;
        canvas.CustomMinimumSize = new Vector2(0, entries.Count * HistoryVisibleRange.RowHeight);
        SetMeta("occurrence_count", entries.Count);
        SetMeta("chronology", string.Join("\n", entries.SelectMany(item => Chronology(item.Event))));
        if (following && appended) scroll.SetDeferred("scroll_vertical", (int)canvas.CustomMinimumSize.Y);
        RenderRows();
        detail.Present(selected >= 0 && selected < entries.Count ? entries[selected] : null);
        FitScroll();
    }

    private void FitScroll()
    {
        float height = Math.Min(Size.Y, CustomMinimumSize.Y);
        detail.FitHeight(height * .55f);
        float available = Math.Max(80, height - detail.Root.GetCombinedMinimumSize().Y
            - GetThemeConstant("separation"));
        scroll.CustomMinimumSize = new Vector2(0,
            Math.Min(canvas.CustomMinimumSize.Y, available));
    }

    private static IEnumerable<string> Chronology(Marvel.View.EventPresentation entry) =>
        entry.Details.Count == 0 ? [entry.Summary] : entry.Details.SelectMany(Chronology);

    internal void RefreshSelection(int index)
    {
        selected = index;
        if (index >= 0)
        {
            int top = index * HistoryVisibleRange.RowHeight;
            if (top < scroll.ScrollVertical || top + HistoryVisibleRange.RowHeight > scroll.ScrollVertical + scroll.Size.Y)
                scroll.ScrollVertical = top;
        }
        RenderRows();
        detail.Present(index >= 0 && index < entries.Count ? entries[index] : null);
    }

    internal void ExpandDetails() => detail.Expand();

    private void KeepSelectionVisible()
    {
        if (!IsInsideTree() || !detail.Expanded || selected < 0) return;
        int top = selected * HistoryVisibleRange.RowHeight;
        double height = Math.Min(scroll.Size.Y, scroll.GetVScrollBar().Page);
        if (height <= 0) return;
        if (top < scroll.ScrollVertical || top + HistoryVisibleRange.RowHeight > scroll.ScrollVertical + height)
            scroll.ScrollVertical = top;
    }

    private bool AtEnd() => scroll.GetVScrollBar().Value >= scroll.GetVScrollBar().MaxValue - scroll.GetVScrollBar().Page - 2;

    private void RenderRows()
    {
        if (!IsInsideTree()) return;
        (int first, int count) = HistoryVisibleRange.At(entries.Count, scroll.ScrollVertical, scroll.Size.Y);
        while (rows.Count < count) AddRow();
        for (int slot = 0; slot < rows.Count; slot++)
        {
            HistoryOccurrenceRow row = rows[slot];
            row.Visible = slot < count;
            if (!row.Visible) continue;
            int index = first + slot;
            row.Position = new Vector2(0, index * HistoryVisibleRange.RowHeight);
            row.Size = new Vector2(Math.Max(1, canvas.Size.X), HistoryVisibleRange.RowHeight - 4);
            row.Present(index, entries[index].Event, index == selected);
        }
        SetMeta("allocated_rows", rows.Count);
    }

    private void AddRow()
    {
        var row = new HistoryOccurrenceRow();
        row.Configure(index => timeline.SelectOccurrence(index));
        canvas.AddChild(row);
        rows.Add(row);
    }
}
