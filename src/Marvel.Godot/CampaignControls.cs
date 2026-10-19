using Godot;
using Marvel.Protocol;
namespace Marvel.Godot;

/// <summary>A compact campaign journal with one current commitment and persistent roster context.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "Preview controls are owned and freed by the Main scene tree.")]
internal sealed class CampaignControls
{
    internal Window Window { get; } = new() { Name = "CampaignTracker", Title = "Campaigns", Visible = false, Size = new(1200, 800),
        MinSize = new(1000, 620), Transient = true, Exclusive = true, GuiEmbedSubwindows = true };
    internal VBoxContainer Content { get; } = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    internal HBoxContainer Navigation { get; } = new();
    internal OptionButton Saved { get; } = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    internal Button Open { get; } = new() { Text = "Resume" };
    internal Button New { get; } = new() { Text = "New campaign" };
    internal Button Refresh { get; } = new() { Text = "Refresh" };
    internal VBoxContainer Body { get; } = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    private readonly VBoxContainer preview = new() { CustomMinimumSize = new(410, 0) };
    internal Label Status { get; } = new() { AutowrapMode = TextServer.AutowrapMode.WordSmart };
    internal Button Retry { get; } = new() { Text = "Recover pending choice", Visible = false };

    internal CampaignControls(Main main)
    {
        Window.Theme = main.Theme;
        main.AddChild(Window);
        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        foreach (string side in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, 20);
        Window.AddChild(margin);
        margin.AddChild(Content);
        Content.AddThemeConstantOverride("separation", 16);
        Body.AddThemeConstantOverride("separation", 8);
        Navigation.AddChild(Saved); Navigation.AddChild(Open); Navigation.AddChild(New); Navigation.AddChild(Refresh);
        Content.AddChild(Navigation);
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        var workspace = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill };
        workspace.AddThemeConstantOverride("separation", 20);
        Content.AddChild(workspace);
        workspace.AddChild(scroll); scroll.AddChild(Body);
        var previewScroll = new ScrollContainer { CustomMinimumSize = new(430, 0),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        workspace.AddChild(previewScroll); previewScroll.AddChild(preview);
        preview.AddChild(Text("Point to a card name, or focus it with the keyboard, to read its full card."));
        Content.AddChild(Status); Content.AddChild(Retry);
        Window.CloseRequested += Window.Hide;
    }

    internal void SetBusy(bool busy) => SetBusy(Body, busy);

    private static void SetBusy(Node parent, bool busy)
    {
        foreach (Node child in parent.GetChildren())
        {
            if (child is BaseButton button)
            {
                const string key = "campaign_prior_disabled";
                if (busy && !button.HasMeta(key)) { button.SetMeta(key, button.Disabled); button.Disabled = true; }
                else if (!busy && button.HasMeta(key)) { button.Disabled = button.GetMeta(key).AsBool(); button.RemoveMeta(key); }
            }
            SetBusy(child, busy);
        }
    }

    internal void Inspect(DeckCard card)
    {
        foreach (Node child in preview.GetChildren()) { preview.RemoveChild(child); child.QueueFree(); }
        preview.AddChild(card.Preview is { } face ? DeckCardPreview.Create(face) : Text(card.Title + "\n" + card.Text));
    }

    internal void Clear()
    {
        foreach (Node child in Body.GetChildren()) { Body.RemoveChild(child); child.QueueFree(); }
    }

    internal static Label Text(string value)
    {
        var label = new Label { Text = value, AutowrapMode = TextServer.AutowrapMode.WordSmart,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        label.AddThemeFontSizeOverride("font_size", 20);
        return label;
    }
    internal static Button Action(VBoxContainer parent, string text, Action action)
    {
        var button = new Button { Text = text, Alignment = HorizontalAlignment.Left };
        button.AddThemeFontSizeOverride("font_size", 18);
        button.Pressed += action;
        parent.AddChild(button);
        return button;
    }
}
