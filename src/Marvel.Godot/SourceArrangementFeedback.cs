using Godot;

namespace Marvel.Godot;

/// <summary>Names the exact exchange and marks its vacancy without covering source bodies.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable",
    Justification = "The tableau scene owns and frees the attached feedback controls.")]
internal sealed class SourceArrangementFeedback
{
    private readonly Dictionary<Control, Control> outlines = [];
    private readonly Label caption;
    private readonly PanelContainer frame;
    private readonly Control owner;

    internal SourceArrangementFeedback(Control owner)
    {
        this.owner = owner;
        caption = CardSourceStrip.Label("", "SourceSwapFeedback", 12, CardTypography.Bold);
        caption.AccessibilityLive = AccessibilityServer.AccessibilityLiveMode.Polite;
        frame = new PanelContainer { Name = "SourceSwapFeedbackFrame", TopLevel = true, Visible = false,
            ZIndex = 245, MouseFilter = Control.MouseFilterEnum.Ignore };
        using var background = new StyleBoxFlat { BgColor = CardFaceStyle.Paper,
            BorderColor = CardFaceStyle.Ink, BorderWidthBottom = 1, ContentMarginLeft = 5, ContentMarginRight = 5 };
        frame.AddThemeStyleboxOverride("panel", background);
        owner.AddChild(frame);
        frame.AddChild(caption);
    }

    internal void Track(Control slot)
    {
        var outline = new Control { Name = "SourceSwapOutline", MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = false, ZIndex = 241 };
        slot.AddChild(outline);
        outline.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        outline.Draw += () => outline.DrawRect(new Rect2(Vector2.One, outline.Size - Vector2.One * 2),
            CardFaceStyle.Paper, false, 2);
        outlines.Add(slot, outline);
    }

    internal void Show(Control origin, string message)
    {
        foreach (Control outline in outlines.Values) outline.Hide();
        outlines[origin].Show();
        frame.GlobalPosition = owner.GlobalPosition - new Vector2(0, 42);
        frame.Size = new Vector2(owner.Size.X, 40);
        caption.Text = message;
        caption.AccessibilityName = message;
        frame.Show();
    }

    internal void Hide()
    {
        foreach (Control outline in outlines.Values) outline.Hide();
        frame.Hide();
    }
}
