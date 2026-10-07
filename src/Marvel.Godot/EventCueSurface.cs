using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns the readable event cue independently of playback timing.</summary>
internal sealed class EventCueSurface(Main main, Action previous, Action next, Action togglePlayback) : IDisposable
{
    private HBoxContainer? strip;
    private Label? summary;
    private Label? position;
    private Button? previousButton;
    private Button? nextButton;
    private Button? playbackButton;

    internal void Present(EventPresentation entry, int index, int count, bool playing)
    {
        EnsureStrip();
        main.eventCueKind.Visible = false;
        main.eventCueSummary.Text = entry.CueSummary ?? entry.Summary;
        summary!.Text = entry.CueSummary ?? entry.Summary;
        summary.TooltipText = entry.Summary;
        position!.Text = $"{index + 1}/{count}";
        position.TooltipText = "Results of the latest action. The table shows its completed state.";
        previousButton!.Disabled = index <= 0;
        nextButton!.Disabled = index + 1 >= count;
        playbackButton!.Text = playing ? "Ⅱ" : "▶";
        playbackButton.TooltipText = playing ? "Pause result playback" : "Play results";
        RefreshVisibility(true);
    }

    private void EnsureStrip()
    {
        if (strip is not null) return;
        Control spacer = main.GetNode<Control>("StatusBar/Spacer");
        strip = new HBoxContainer { Name = "EventPlayback", MouseFilter = Control.MouseFilterEnum.Pass };
        spacer.AddChild(strip);
        strip.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        previousButton = AddButton("PreviousResult", "‹", "Previous result", previous);
        playbackButton = AddButton("PlayResults", "▶", "Play results", togglePlayback);
        nextButton = AddButton("NextResult", "›", "Next result", next);
        position = new Label { Name = "ResultPosition", CustomMinimumSize = new Vector2(50, 0) };
        strip.AddChild(position);
        summary = new Label
        {
            Name = "ResultCue", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            VerticalAlignment = VerticalAlignment.Center,
        };
        summary.AddThemeFontSizeOverride("font_size", 20);
        strip.AddChild(summary);
    }

    private Button AddButton(string name, string symbol, string description, Action pressed)
    {
        var button = new Button
        {
            Name = name, Text = symbol, TooltipText = description,
            CustomMinimumSize = new Vector2(44, 44), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        };
        button.Pressed += pressed;
        strip!.AddChild(button);
        return button;
    }

    internal void Clear()
    {
        main.eventCueSummary.Text = string.Empty;
        main.eventCueSummary.TooltipText = string.Empty;
        main.eventCue.Visible = false;
        main.eventCue.Modulate = Colors.White;
        if (summary is not null) summary.Text = string.Empty;
        if (strip is not null) strip.Visible = false;
    }

    internal void RefreshVisibility(bool active)
    {
        bool desktop = DesktopTabletop.Uses(main.GetViewportRect().Size);
        main.eventCue.Visible = active && !desktop;
        if (strip is not null) strip.Visible = active && desktop;
        main.GetNode<Control>("StatusBar/Spacer").CustomMinimumSize =
            new Vector2(0, desktop ? 64 : 0);
    }
    public void Dispose()
    {
        if (InteractionControl.IsUsable(strip))
        {
            strip!.GetParent().RemoveChild(strip);
            strip.QueueFree();
        }
        strip = null;
        summary = null;
        position = null;
        previousButton = nextButton = playbackButton = null;
    }

}
