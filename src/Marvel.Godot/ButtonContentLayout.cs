using Godot;

namespace Marvel.Godot;

/// <summary>Fits composed button content to its available width and pointer floor.</summary>
internal static class ButtonContentLayout
{
    internal static void Attach(Button button, Control content)
    {
        float minimumHeight = button.CustomMinimumSize.Y;
        var margin = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        margin.AddThemeConstantOverride("margin_left", 12);
        margin.AddThemeConstantOverride("margin_right", 12);
        margin.AddThemeConstantOverride("margin_top", 6);
        margin.AddThemeConstantOverride("margin_bottom", 6);
        margin.AddChild(content);
        button.AddChild(margin);
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        CardInspectorFocus.IgnoreMouseRecursively(content, interactiveRules: false);
        void Fit()
        {
            if (!InteractionControl.IsUsable(button) || !InteractionControl.IsUsable(content)) return;
            button.CustomMinimumSize = new Vector2(button.CustomMinimumSize.X,
                Math.Max(minimumHeight, content.GetCombinedMinimumSize().Y + 12));
        }
        content.MinimumSizeChanged += () => Callable.From(Fit).CallDeferred();
        content.Resized += () => Callable.From(Fit).CallDeferred();
        Fit();
    }
}
