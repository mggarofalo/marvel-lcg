using Godot;
using Marvel.Rules.Prompts;
namespace Marvel.Godot;

/// <summary>Presentation policy for non-spatial decisions and their bounded reading surface.</summary>
internal static class DecisionDialogLayout
{
    internal static bool Automatic(Prompt? prompt) => prompt is not null
        && (prompt.PublicKind == PublicDecisionKind.Order
            || prompt.PublicKind == PublicDecisionKind.Choice && prompt.Affordances.All(offer =>
                offer.Targets is null && offer.SelectionSourceId is null));

    internal static Rect2 Frame(Vector2 viewport, InterfaceScale scale, int choices)
    {
        float factor = (int)scale / 100f;
        float width = Math.Min(Math.Clamp(760 * factor, 640, 1000), Math.Max(240, viewport.X - 48));
        float desired = Math.Clamp(250 + Math.Min(choices, 5) * 92, 400, 710) * factor;
        float height = Math.Min(desired, Math.Max(200, viewport.Y - 96));
        return new(new Vector2(MathF.Round((viewport.X - width) / 2),
            MathF.Round(Math.Max(24, (viewport.Y - height) * 0.45f))),
            new Vector2(MathF.Round(width), MathF.Round(height)));
    }
}
