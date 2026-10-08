using Godot;

namespace Marvel.Godot;

/// <summary>Pages whole native action rows using their resolved font and container measurements.</summary>
internal sealed class TableActionPages : IDisposable
{
    private readonly VBoxContainer frame;
    private readonly Control viewport;
    private readonly VBoxContainer rows;
    private readonly HBoxContainer navigation;
    private readonly Button earlier;
    private readonly Button more;
    private readonly Button complete;
    private readonly HashSet<Control> measuredRows = [];
    private bool queued;
    private bool disposed;
    private int page;
    private TableActionPageLayout layout = new([], false);

    private TableActionPages(Control parent, Action<Control> openCompleteChoices)
    {
        frame = new VBoxContainer
        {
            Name = "ContextualActionPages", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        parent.AddChild(frame);
        viewport = new Control
        {
            Name = "ContextualActionScroll", ClipContents = true,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
        };
        frame.AddChild(viewport);
        rows = new VBoxContainer
        {
            Name = "ContextualActionObjects", ThemeTypeVariation = GodotThemeVariations.TightStack,
            MouseFilter = Control.MouseFilterEnum.Pass,
        };
        viewport.AddChild(rows);
        navigation = new HBoxContainer
        {
            Name = "OverflowNavigation", ThemeTypeVariation = GodotThemeVariations.TightStack,
            Visible = false,
        };
        frame.AddChild(navigation);
        earlier = TableNavigationButton.Create(navigation, "Earlier", "Earlier complete choices");
        more = TableNavigationButton.Create(navigation, "More", "More complete choices");
        complete = TableNavigationButton.Create(frame, "ActionPageCompleteChoices",
            "Open complete choices because this choice needs more room");
        complete.Text = "Open complete choices\nThis choice needs more room";
        complete.Visible = false;
        complete.Pressed += () => openCompleteChoices(complete);
        earlier.Pressed += () => Move(-1);
        more.Pressed += () => Move(1);
        frame.Resized += Queue;
        viewport.Resized += Queue;
        navigation.MinimumSizeChanged += Queue;
        rows.ChildEnteredTree += Entered;
        rows.ChildExitingTree += Exiting;
        frame.TreeExiting += Dispose;
        rows.VisibilityChanged += Queue;
        Queue();
    }

    internal static VBoxContainer Create(Control parent, float width, Action<Control> openCompleteChoices)
    {
        var pages = new TableActionPages(parent, openCompleteChoices);
        pages.frame.SizeFlagsHorizontal = Control.SizeFlags.Fill;
        pages.frame.CustomMinimumSize = new Vector2(width, 0);
        return pages.rows;
    }

    private void Entered(Node node)
    {
        if (node is Control row)
        {
            measuredRows.Add(row);
            row.MinimumSizeChanged += Queue;
            row.SetMeta("action_page_row", true);
        }
        page = 0;
        Queue();
    }

    private void Exiting(Node node)
    {
        if (node is Control row && measuredRows.Remove(row)) row.MinimumSizeChanged -= Queue;
        page = 0;
        Queue();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        // The scene tree owns the nodes; this controller releases its subscriptions.
        frame.Resized -= Queue;
        frame.TreeExiting -= Dispose;
        viewport.Resized -= Queue;
        navigation.MinimumSizeChanged -= Queue;
        rows.ChildEnteredTree -= Entered;
        rows.ChildExitingTree -= Exiting;
        rows.VisibilityChanged -= Queue;
        foreach (Control row in measuredRows) row.MinimumSizeChanged -= Queue;
        measuredRows.Clear();
    }

    private void Queue()
    {
        if (disposed || queued || !InteractionControl.IsUsable(frame)) return;
        queued = true;
        Callable.From(() =>
        {
            queued = false;
            if (!disposed && InteractionControl.IsUsable(frame)) Refresh();
        }).CallDeferred();
    }

    private void Refresh()
    {
        Control? focused = frame.GetViewport().GuiGetFocusOwner();
        bool focusedRow = focused is not null && rows.IsAncestorOf(focused);
        Control[] children = [.. rows.GetChildren().OfType<Control>().Where(InteractionControl.IsUsable)];
        float width = viewport.Size.X;
        rows.Size = new Vector2(width, viewport.Size.Y);
        foreach (Control child in children)
            child.Size = new Vector2(width, child.GetCombinedMinimumSize().Y);
        float separation = rows.GetThemeConstant("separation");
        float navigationHeight = Math.Max(navigation.GetCombinedMinimumSize().Y,
            Math.Max(earlier.GetCombinedMinimumSize().Y, more.GetCombinedMinimumSize().Y));
        layout = TableActionPageLayout.Measure([.. children.Select(child => child.GetCombinedMinimumSize().Y)],
            frame.Size.Y, navigationHeight, separation, frame.GetThemeConstant("separation"));
        page = layout.ClampPage(page);
        PresentPage(children);
        rows.Size = new Vector2(width, viewport.Size.Y);
        if (focusedRow && !focused!.IsVisibleInTree()) FocusCurrentPage();
    }

    private void PresentPage(Control[] children)
    {
        bool enabled = rows.Visible;
        complete.Visible = enabled && layout.RequiresCompleteChoices;
        viewport.Visible = !layout.RequiresCompleteChoices;
        navigation.Visible = enabled && !layout.RequiresCompleteChoices && layout.Pages.Count > 1;
        earlier.Visible = page > 0;
        more.Visible = page + 1 < layout.Pages.Count;
        TableActionPage? current = layout.RequiresCompleteChoices ? null : layout.Pages[page];
        for (int index = 0; index < children.Length; index++)
            children[index].Visible = current is not null && index >= current.Start && index < current.Start + current.Count;
        frame.SetMeta("action_page", page);
        frame.SetMeta("action_pages", layout.Pages.Count);
    }

    private void Move(int direction)
    {
        page = layout.ClampPage(page + direction);
        Refresh();
        FocusCurrentPage();
    }

    private void FocusCurrentPage()
    {
        Callable.From(() =>
        {
            if (!InteractionControl.IsUsable(rows) || !rows.Visible) return;
            if (complete.Visible) { complete.GrabFocus(); return; }
            Control? first = rows.GetChildren().OfType<Control>().Where(child => child.Visible)
                .SelectMany(Focusable).FirstOrDefault();
            (first ?? (more.Visible ? more : earlier)).GrabFocus();
        }).CallDeferred();
    }

    private static IEnumerable<Control> Focusable(Control row)
    {
        if (row.FocusMode == Control.FocusModeEnum.All && row is not BaseButton { Disabled: true }) yield return row;
        foreach (Control child in row.GetChildren().OfType<Control>().Where(child => child.Visible))
            foreach (Control target in Focusable(child)) yield return target;
    }
}
