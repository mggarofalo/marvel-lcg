using Godot;

namespace Marvel.Godot;

/// <summary>Decodes compact action symbols immediately without moving their hit areas.</summary>
internal static class CardActionHint
{
    internal static void Bind(Button button, string action)
    {
        var hint = new PanelContainer
        {
            Name = "ActionVerbHint", TopLevel = true, Visible = false, ZIndex = 900,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ThemeTypeVariation = GodotThemeVariations.SurfacePanel,
        };
        var label = new Label
        {
            Text = action, ThemeTypeVariation = GodotThemeVariations.Caption,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        hint.AddChild(label);
        button.AddChild(hint);
        bool hovered = false;
        void Refresh()
        {
            hint.Visible = button.IsVisibleInTree() && (hovered || button.HasFocus());
            if (!hint.Visible) return;
            hint.ResetSize();
            Rect2 bounds = button.GetGlobalRect();
            Vector2 size = hint.GetCombinedMinimumSize();
            Vector2 viewport = button.GetViewportRect().Size;
            hint.GlobalPosition = new Vector2(
                Math.Clamp(bounds.GetCenter().X - size.X / 2, 0, Math.Max(0, viewport.X - size.X)),
                Math.Max(0, bounds.Position.Y - size.Y - 4));
        }
        button.MouseEntered += () => { hovered = true; Refresh(); };
        button.MouseExited += () => { hovered = false; Refresh(); };
        button.FocusEntered += Refresh;
        button.FocusExited += Refresh;
        button.VisibilityChanged += Refresh;
        button.Resized += Refresh;
    }
}
