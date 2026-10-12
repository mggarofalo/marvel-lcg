using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>A reusable native event card: a semantic glyph, concise result and phase context.</summary>
internal sealed partial class HistoryOccurrenceRow : Button
{
    private readonly Label title = new() { MouseFilter = MouseFilterEnum.Ignore,
        AutowrapMode = TextServer.AutowrapMode.WordSmart, MaxLinesVisible = 2,
        TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis };
    private readonly Label context = new() { MouseFilter = MouseFilterEnum.Ignore };
    private EventPresentation? entry;
    internal int Occurrence { get; private set; }

    internal void Configure(Action<int> select)
    {
        ClipContents = true;
        AddChild(title);
        AddChild(context);
        title.AddThemeFontSizeOverride("font_size", 15);
        context.AddThemeFontSizeOverride("font_size", 11);
        Pressed += () => select(Occurrence);
        Resized += Fit;
    }

    internal void Present(int index, EventPresentation presented, bool selected)
    {
        Occurrence = index;
        SetMeta("occurrence_index", index);
        SetMeta("selected_occurrence", selected);
        entry = presented;
        title.Text = presented.CueSummary ?? presented.Summary;
        context.Text = presented.ResultSummary ?? (presented.Activity is { Round: { } round, Phase.Length: > 0 } activity
            ? $"ROUND {round} · {activity.Phase.ToUpperInvariant()}" : "");
        AccessibilityName = presented.Summary;
        TooltipText = presented.Summary;
        using var frame = new StyleBoxFlat { BgColor = selected ? new Color("25384a") : CardFaceStyle.Ink,
            BorderColor = ClientTheme.ToGodot(VisualSystem.Palette.Accent), BorderWidthLeft = selected ? 4 : 1,
            ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 6, ContentMarginBottom = 6 };
        AddThemeStyleboxOverride("normal", frame);
        Fit();
        QueueRedraw();
    }

    private void Fit()
    {
        title.Position = new Vector2(50, 8);
        title.Size = new Vector2(Math.Max(1, Size.X - 64), 44);
        context.Position = new Vector2(50, 55);
        context.Size = new Vector2(Math.Max(1, Size.X - 64), 18);
    }

    public override void _Draw()
    {
        if (entry is not null) ActivityGlyphDrawing.Draw(this, entry, new Rect2(10, 20, 30, 30));
    }
}
