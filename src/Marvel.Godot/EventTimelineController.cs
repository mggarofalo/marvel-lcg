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
    private PanelContainer? popup;
    private Label? detail;
    private EventPresentation? active;

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
        if (anchor is null) return;
        panel.Size = new Vector2(anchor.Size.X, Math.Max(44, anchor.Size.Y));
        panel.GlobalPosition = new Vector2(anchor.GlobalPosition.X,
            anchor.GetGlobalRect().End.Y - panel.Size.Y);
        panel.SetMeta("timeline_count", archive.Entries.Count);
        Refresh();
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
        AddButton(row, "<", "Previous event", () => viewport.Select(viewport.Selected - 1));
        AddButton(row, ">", "Next event", () => viewport.Select(viewport.Selected + 1));
        var body = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("separation", 0);
        row.AddChild(body);
        caption = new Label { Name = "TimelineCaption", TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        caption.AddThemeFontSizeOverride("font_size", 12);
        body.AddChild(caption);
        rail = new Control { Name = "TimelineRail", CustomMinimumSize = new Vector2(120, 24),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, FocusMode = Control.FocusModeEnum.All,
            MouseDefaultCursorShape = Control.CursorShape.PointingHand };
        body.AddChild(rail);
        rail.Draw += () => TimelineRailDrawing.Paint(rail, viewport, archive.Entries);
        var input = new TimelineRailInput(rail, viewport, Refresh, Preview);
        rail.GuiInput += input.Handle;
        rail.MouseExited += HidePreview;
        rail.FocusExited += HidePreview;
        rail.FocusEntered += () => { rail.QueueRedraw(); Preview(viewport.Selected); };
        AddButton(row, "-", "Zoom out", () => viewport.Zoom(1 / .7, .5));
        AddButton(row, "+", "Zoom in", () => viewport.Zoom(.7, .5));
        AddButton(row, "|>", "Latest event", viewport.Latest);
        CreatePreview();
    }

    private void AddButton(Control row, string symbol, string name, Action action)
    {
        var button = new Button { Text = symbol, AccessibilityName = name, TooltipText = name,
            CustomMinimumSize = new Vector2(44, 44), MouseFilter = Control.MouseFilterEnum.Stop };
        button.Pressed += () => { action(); HidePreview(); Refresh(); };
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
        int selected = viewport.Selected;
        string text = selected < 0 ? "" : Copy(archive.Entries[selected].Event);
        if (viewport.Following && active is not null) text = Copy(active);
        caption.Text = text;
        rail.AccessibilityName = text;
        panel.SetMeta("timeline_selected", selected);
        panel.SetMeta("timeline_span", viewport.Span);
        panel.SetMeta("timeline_following", viewport.Following);
        rail.QueueRedraw();
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

    private static string Copy(EventPresentation entry) => string.IsNullOrWhiteSpace(entry.Intention)
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
