using Godot;
namespace Marvel.Godot;

/// <summary>A compact campaign journal with one current commitment and persistent roster context.</summary>
internal sealed class CampaignControls
{
    internal Window Window { get; } = new() { Name = "CampaignTracker", Title = "Campaigns", Size = new(1000, 760),
        MinSize = new(780, 560), Transient = true, Exclusive = true, GuiEmbedSubwindows = true };
    internal VBoxContainer Content { get; } = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    internal HBoxContainer Navigation { get; } = new();
    internal OptionButton Saved { get; } = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    internal Button Open { get; } = new() { Text = "Resume" };
    internal Button New { get; } = new() { Text = "New campaign" };
    internal Button Refresh { get; } = new() { Text = "Refresh" };
    internal VBoxContainer Body { get; } = new() { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
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
        Navigation.AddChild(Saved); Navigation.AddChild(Open); Navigation.AddChild(New); Navigation.AddChild(Refresh);
        Content.AddChild(Navigation);
        var scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        Content.AddChild(scroll); scroll.AddChild(Body);
        Content.AddChild(Status); Content.AddChild(Retry);
        Window.CloseRequested += Window.Hide;
    }

    internal void Clear()
    {
        foreach (Node child in Body.GetChildren()) { Body.RemoveChild(child); child.QueueFree(); }
    }

    internal static Label Text(string value) => new() { Text = value, AutowrapMode = TextServer.AutowrapMode.WordSmart,
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
    internal static Button Action(VBoxContainer parent, string text, Action action)
    {
        var button = new Button { Text = text, Alignment = HorizontalAlignment.Left };
        button.Pressed += action;
        parent.AddChild(button);
        return button;
    }
}
