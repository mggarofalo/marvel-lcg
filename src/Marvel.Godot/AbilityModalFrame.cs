using Godot;

namespace Marvel.Godot;

/// <summary>Owns the opaque ability sheet and its bounded native controls.</summary>
internal sealed class AbilityModalFrame : IDisposable
{
    private readonly Main main;
    private readonly CanvasLayer layer;
    private readonly PanelContainer frame;
    private readonly Control overlay;
    private readonly VBoxContainer shell;
    private readonly VBoxContainer footer;
    private readonly HBoxContainer headingRow;
    private readonly HBoxContainer? pairedChoices;
    private readonly bool pinCurrentChoices;
    private float fittedWidth = 560;
    private bool disposed;
    internal VBoxContainer Content { get; }
    internal VBoxContainer Footer => footer;
    internal Control Frame => frame;

    internal AbilityModalFrame(Main main, string name, Theme? theme, int currentChoices = 0)
    {
        this.main = main;
        pinCurrentChoices = currentChoices is > 0 and <= 2;
        layer = new CanvasLayer { Name = name + "Layer", Layer = 210 };
        main.AddChild(layer);
        AbilitySituationOwnership.Change(main, 1);
        overlay = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(overlay);
        overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        frame = new PanelContainer
        {
            Name = name, Theme = theme,
            ThemeTypeVariation = GodotThemeVariations.SurfacePanel,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        overlay.AddChild(frame);
        shell = new VBoxContainer { ThemeTypeVariation = GodotThemeVariations.TightStack };
        frame.AddChild(shell);
        headingRow = new HBoxContainer { ThemeTypeVariation = GodotThemeVariations.TightStack };
        shell.AddChild(headingRow);
        var scroll = new ScrollContainer
        {
            Name = name + "Viewport", FollowFocus = true,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
        };
        shell.AddChild(scroll);
        Content = new VBoxContainer
        {
            Name = name + "Content", ThemeTypeVariation = GodotThemeVariations.TightStack,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        scroll.AddChild(Content);
        footer = new VBoxContainer { ThemeTypeVariation = GodotThemeVariations.TightStack };
        shell.AddChild(footer);
        if (currentChoices == 2)
        {
            pairedChoices = new HBoxContainer { ThemeTypeVariation = GodotThemeVariations.TightStack };
            footer.AddChild(pairedChoices);
        }
        footer.MinimumSizeChanged += QueueFit;
        overlay.Resized += QueueFit;
        main.interfaceScaleSlider.ValueChanged += ScaleChanged;
        Content.MinimumSizeChanged += QueueFit;
        QueueFit();
    }

    internal void AddText(string name, string text, string variation)
    {
        if (string.IsNullOrWhiteSpace(text)) return;
        var label = DecisionPanel.Text(text, variation, wrap: true);
        label.Name = name;
        label.MouseFilter = Control.MouseFilterEnum.Ignore;
        if (variation == GodotThemeVariations.Heading)
        {
            label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            headingRow.AddChild(label);
        }
        else if (name == "AbilityWindowAnswerer")
        {
            label.AutowrapMode = TextServer.AutowrapMode.Off;
            label.VerticalAlignment = VerticalAlignment.Center;
            headingRow.AddChild(label);
        }
        else if (name == "PendingDecision") footer.AddChild(label);
        else Content.AddChild(label);
    }

    internal Button AddButton(string name, string text, Action action, bool disabled = false)
    {
        var button = new Button
        {
            Name = name, Text = text, AccessibilityName = text,
            CustomMinimumSize = new Vector2(0, 44), Disabled = disabled,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            ThemeTypeVariation = GodotThemeVariations.ChoiceButton,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        button.Pressed += action;
        ButtonParent(name).AddChild(button);
        return button;
    }

    private Container ButtonParent(string name)
    {
        if (name is "WindowPass" or "AcknowledgeForcedAbility") return footer;
        if (pairedChoices is not null) return pairedChoices;
        return pinCurrentChoices ? footer : Content;
    }

    internal void SetVisible(bool visible) => layer.Visible = visible;
    private void QueueFit() => Callable.From(Fit).CallDeferred();
    private void ScaleChanged(double _) => QueueFit();

    private void Fit()
    {
        if (!InteractionControl.IsUsable(frame)) return;
        frame.Theme = main.decisions.Theme;
        float preferredHeight = shell.GetCombinedMinimumSize().Y + Content.GetCombinedMinimumSize().Y + 32;
        Rect2 bounds = Bounds(preferredHeight);
        if (preferredHeight > bounds.Size.Y + 1 && fittedWidth < 760)
        {
            fittedWidth = Math.Min(760, main.GetViewportRect().Size.X - 80);
            bounds = Bounds(preferredHeight);
        }
        frame.Position = bounds.Position;
        frame.Size = bounds.Size;
    }

    private Rect2 Bounds(float preferredHeight)
    {
        Vector2 viewport = main.GetViewportRect().Size;
        Control? hand = main.FindChild("HandDropArea", true, false) as Control;
        Control? timeline = main.FindChild("EventTimeline", true, false) as Control;
        return AbilityModalLayout.Fit(viewport,
            hand?.GetGlobalRect().Position.Y ?? viewport.Y - 250,
            timeline?.GetGlobalRect().End.Y ?? viewport.Y * .4f, preferredHeight, fittedWidth);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        layer.Visible = false;
        AbilitySituationOwnership.Change(main, -1);
        overlay.Resized -= QueueFit;
        main.interfaceScaleSlider.ValueChanged -= ScaleChanged;
        Content.MinimumSizeChanged -= QueueFit;
        footer.MinimumSizeChanged -= QueueFit;
        layer.QueueFree();
    }
}
