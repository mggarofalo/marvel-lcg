using Godot;

namespace Marvel.Godot;

/// <summary>Owns the latest receipt's bounded viewport and continuation controls.</summary>
internal static class TableLatestResult
{
    internal static Label Ensure(Main main, Control history)
    {
        if (history.FindChild("LatestResult", true, false) is Label existing) return existing;
        ScrollContainer scroll = TableScrollNavigation.Create(history, "LatestResultScroll", "result");
        Control frame = (Control)scroll.GetParent();
        frame.CustomMinimumSize = new Vector2(0, 160);
        history.MoveChild(frame, Math.Max(0, main.eventLog.GetIndex()));
        var label = new Label
        {
            Name = "LatestResult", Text = main.lastResultSummary.Text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.Body,
        };
        scroll.AddChild(label);
        label.Resized += () => Fit(label);
        return label;
    }

    internal static void Configure(Label label, bool historyExpanded)
    {
        Control frame = Frame(label);
        frame.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        frame.Visible = !string.IsNullOrEmpty(label.Text);
        Fit(label);
    }

    internal static void Present(Label label, string text)
    {
        label.Text = text;
        var scroll = (ScrollContainer)label.GetParent();
        scroll.ScrollVertical = 0;
        scroll.SetDeferred("scroll_vertical", 0);
        Frame(label).Visible = !string.IsNullOrEmpty(text);
        Callable.From(() => Fit(label)).CallDeferred();
    }

    private static void Fit(Label label)
    {
        if (!InteractionControl.IsUsable(label)) return;
        float height = Math.Clamp(label.GetLineHeight() * Math.Max(3, label.GetLineCount()) + 48, 108, 220);
        Control frame = Frame(label);
        var size = new Vector2(0, height);
        if (!frame.CustomMinimumSize.IsEqualApprox(size)) frame.CustomMinimumSize = size;
    }

    private static Control Frame(Label label) => (Control)label.GetParent().GetParent();
}
