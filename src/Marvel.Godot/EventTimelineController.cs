using Godot;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>A persistent, zoomable native chronology with one transient occurrence preview.</summary>
internal sealed class EventTimelineController(Main main) : IDisposable
{
    private readonly TimelineArchive archive = new();
    private readonly TimelineViewport viewport = new();
    private PanelContainer? panel;
    private Control? rail;
    private Label? caption;
    private HBoxContainer? references;
    private EventPresentation? referenceEntry;
    private PanelContainer? popup;
    private Label? detail;
    private EventPresentation? active;
    private readonly List<Button> navigation = [];
    private Button? disclosure;
    private bool expanded;
    private int lastSelection = -1;
    internal IReadOnlyList<TimelineOccurrence> Entries => archive.Entries;
    internal int Selected => viewport.Selected;
    internal event Action<int>? SelectionChanged;
    internal event Action? DetailRequested;

    internal void SelectOccurrence(int index)
    {
        viewport.Select(index);
        Refresh();
        DetailRequested?.Invoke();
    }

    internal void PresentBeat(EventActivitySequence sequence)
    {
        active = sequence.Active ? sequence.Current : null;
        if (panel is not null)
        {
            panel.SetMeta("activity_running", sequence.Active);
            panel.SetMeta("activity_index", sequence.Index);
        }
        Refresh();
    }

    internal void Clear()
    {
        archive.Clear();
        active = null;
        viewport.SetCount(0);
        viewport.Latest();
        panel?.Hide();
        HidePreview();
    }

    internal void Record(EngineResponse response, IReadOnlyList<EventPresentation> entries, bool reset)
    {
        archive.Record(response, entries, reset);
        viewport.SetCount(archive.Entries.Count);
        Refresh();
    }

    internal void Fit()
    {
        Ensure();
        Control? anchor = main.boardAreas.FindChild("EventActivity", true, false) as Control;
        panel!.Visible = main.board.Visible && anchor is not null && archive.Entries.Count > 0;
        if (!panel.Visible) HidePreview();
        if (anchor is null) return;
        Refresh();
        panel.ResetSize();
        panel.Size = new Vector2(anchor.Size.X, expanded ? 76 : 44);
        panel.GlobalPosition = new Vector2(anchor.GlobalPosition.X,
            anchor.GetGlobalRect().End.Y - panel.Size.Y);
        if (TableHistoryDrawer.IsExpanded(main) && !CardPaymentWorkspaceLayout.Active(main))
        {
            float width = TableHistoryDrawer.ExpandedWidth(main);
            panel.Size = new Vector2(width, panel.Size.Y);
            panel.GlobalPosition = new Vector2(main.GetViewportRect().Size.X - width - 16, 88);
        }
        panel.SetMeta("timeline_count", archive.Entries.Count);
    }

    private void Ensure()
    {
        if (panel is not null) return;
        panel = new PanelContainer { Name = "EventTimeline", ZIndex = 90,
            MouseFilter = Control.MouseFilterEnum.Stop };
        panel.AddThemeStyleboxOverride("panel", Frame(4));
        main.AddChild(panel);
        var row = new HBoxContainer();
        panel.AddChild(row);
        AddButton(row, "<", "Previous event", () => SelectOccurrence(viewport.Selected - 1));
        AddButton(row, ">", "Next event", () => SelectOccurrence(viewport.Selected + 1));
        var body = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 0);
        row.AddChild(body);
        caption = new Label { Name = "TimelineCaption", TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        caption.AddThemeFontSizeOverride("font_size", 14);
        var headline = new HBoxContainer();
        body.AddChild(headline);
        caption.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        headline.AddChild(caption);
        references = new HBoxContainer { Name = "ActivityReference" };
        headline.AddChild(references);
        rail = new Control { Name = "TimelineRail", CustomMinimumSize = new Vector2(120, 24),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, FocusMode = Control.FocusModeEnum.All,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand };
        body.AddChild(rail);
        rail.Draw += () => TimelineRailDrawing.Paint(rail, viewport, archive.Entries);
        var input = new TimelineRailInput(rail, viewport, Refresh, Preview, () => DetailRequested?.Invoke());
        rail.GuiInput += input.Handle;
        rail.MouseExited += HidePreview;
        rail.FocusExited += HidePreview;
        rail.FocusEntered += () => { rail.QueueRedraw(); Preview(viewport.Selected); };
        AddButton(row, "-", "Zoom out", () => viewport.Zoom(1 / .7, .5));
        AddButton(row, "+", "Zoom in", () => viewport.Zoom(.7, .5));
        AddButton(row, "|>", "Latest event", viewport.Latest);
        disclosure = new Button { Name = "TimelineDisclosure", Text = "Timeline",
            AccessibilityName = "Expand timeline", CustomMinimumSize = new Vector2(44, 44) };
        disclosure.Pressed += ToggleDisclosure;
        row.AddChild(disclosure);
        CreatePreview();
    }

    private void ToggleDisclosure()
    {
        expanded = !expanded;
        HidePreview();
        Fit();
        HistoryOverlayLayout.Configure(main, DesktopTabletop.Uses(main.GetViewportRect().Size),
            CardPaymentWorkspaceLayout.Active(main));
    }

    private void AddButton(Control row, string symbol, string name, Action action)
    {
        var button = new Button { Text = symbol, AccessibilityName = name, TooltipText = name,
            CustomMinimumSize = new Vector2(44, 44), MouseFilter = Control.MouseFilterEnum.Stop };
        button.Pressed += () => { action(); HidePreview(); Refresh(); };
        navigation.Add(button);
        row.AddChild(button);
    }

    private void CreatePreview()
    {
        popup = new PanelContainer { Name = "TimelinePreview", Visible = false, ZIndex = 100,
            MouseFilter = Control.MouseFilterEnum.Ignore, CustomMinimumSize = new Vector2(380, 0) };
        popup.AddThemeStyleboxOverride("panel", Frame(10));
        detail = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(360, 0), MouseFilter = Control.MouseFilterEnum.Ignore };
        popup.AddChild(detail);
        main.AddChild(popup);
    }

    private void Refresh()
    {
        if (panel is null || rail is null || caption is null) return;
        RefreshDisclosure();
        RefreshSelection();
    }

    private void RefreshDisclosure()
    {
        if (rail is null || panel is null) return;
        rail.Visible = expanded;
        foreach (Button button in navigation) button.Visible = expanded;
        if (disclosure is not null)
        {
            disclosure.Text = expanded ? "−" : "Timeline";
            disclosure.AccessibilityName = expanded ? "Collapse timeline" : "Expand timeline";
            disclosure.TooltipText = expanded ? "Collapse timeline" : string.Empty;
        }
        panel.SetMeta("timeline_expanded", expanded);
    }

    private void RefreshSelection()
    {
        if (panel is null || rail is null || caption is null) return;
        int selected = viewport.Selected;
        string text = selected < 0 ? "" : Copy(archive.Entries[selected].Event);
        if (viewport.Following && active is not null) text = Copy(active);
        caption.Text = text;
        RefreshReferences(selected);
        rail.AccessibilityName = text;
        panel.SetMeta("timeline_selected", selected);
        panel.SetMeta("timeline_span", viewport.Span);
        panel.SetMeta("timeline_following", viewport.Following);
        rail.QueueRedraw();
        if (lastSelection != selected)
        {
            lastSelection = selected;
            SelectionChanged?.Invoke(selected);
        }
    }

    private void RefreshReferences(int selected)
    {
        EventPresentation? shown = viewport.Following && active is not null ? active
            : selected >= 0 ? archive.Entries[selected].Event : null;
        if (references is not null && !ReferenceEquals(referenceEntry, shown))
        {
            referenceEntry = shown;
            ActivityReferenceChips.Present(references, main, shown, compact: true);
        }
    }

    private void Preview(int index)
    {
        if (index < 0) { HidePreview(); return; }
        if (popup is null || detail is null || panel is null) return;
        detail.Text = Copy(archive.Entries[index].Event);
        popup.Show();
        popup.ResetSize();
        Vector2 size = main.GetViewportRect().Size;
        float x = rail?.HasFocus() == true ? rail.GetGlobalRect().GetCenter().X : main.GetGlobalMousePosition().X;
        popup.GlobalPosition = new Vector2(Math.Clamp(x - 190, 8, Math.Max(8, size.X - 388)),
            Math.Max(8, panel.GlobalPosition.Y - popup.Size.Y - 8));
    }

    private static string Copy(EventPresentation entry) => entry.ResultSummary is { } result
        ? $"{entry.CueSummary ?? entry.Summary} · {result}"
        : string.IsNullOrWhiteSpace(entry.Intention)
        || entry.Motion is not (EventMotionKind.Damage or EventMotionKind.Threat or EventMotionKind.Heal)
        ? entry.Summary : entry.Intention.TrimEnd('.') + " · " + entry.Summary;

    private void HidePreview() => popup?.Hide();

    private static StyleBoxFlat Frame(int inset) => new()
    {
        BgColor = CardFaceStyle.Ink, BorderColor = ClientTheme.ToGodot(VisualSystem.Palette.Accent),
        BorderWidthBottom = 1, ContentMarginLeft = inset, ContentMarginRight = inset,
        ContentMarginTop = 2, ContentMarginBottom = 2,
    };

    public void Dispose()
    {
        panel?.QueueFree();
        popup?.QueueFree();
    }
}
