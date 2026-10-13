using Godot;

namespace Marvel.Godot;

/// <summary>Applies the decision editor's native text and accessible control metrics.</summary>
internal static class DecisionControlStyle
{
    internal static Label Text(string text, string variation, bool wrap) => new()
    {
        Text = text,
        AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off,
        ThemeTypeVariation = variation,
    };

    internal static void Button(Button button, InteractiveVisualState state, ControlMetrics metrics, bool compact)
    {
        button.ThemeTypeVariation = VisualSystem.For(state).ThemeVariation;
        button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        button.CustomMinimumSize = new Vector2(Math.Max(button.CustomMinimumSize.X,
            compact ? metrics.MinimumPointerTarget : metrics.MinimumButtonWidth), metrics.MinimumHeight);
    }
}
