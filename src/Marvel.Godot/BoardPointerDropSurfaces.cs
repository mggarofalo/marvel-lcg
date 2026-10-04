using Godot;

namespace Marvel.Godot;

/// <summary>Owns drop-surface hit testing and temporary pointer cues.</summary>
internal sealed class BoardPointerDropSurfaces
{
    private readonly List<BoardDropTarget> dropTargets = [];
    private Control? mulliganDiscard;
    private string mulliganDiscardVariation = string.Empty;

    internal void Register(int seat, Control control) => dropTargets.Add(new BoardDropTarget(seat, control));

    internal bool Contains(int seat, Vector2 position) => dropTargets.Any(target => target.Seat == seat
        && InteractionControl.IsUsable(target.Control) && target.Control.GetGlobalRect().HasPoint(position));

    internal void RegisterMulligan(Control control)
    {
        mulliganDiscard = control;
        mulliganDiscardVariation = control.ThemeTypeVariation;
    }

    internal bool ContainsMulligan(Vector2 position) => InteractionControl.IsUsable(mulliganDiscard)
        && mulliganDiscard!.GetGlobalRect().HasPoint(position);

    internal void SetActive(bool active) {
        foreach (BoardDropTarget target in dropTargets.Where(target =>
                     InteractionControl.IsUsable(target.Control)))
        {
            target.Control.ThemeTypeVariation = active
                ? GodotThemeVariations.SpatialDropTarget : GodotThemeVariations.SpatialPlayerMat;
            target.Control.SetMeta("spatial_drop_active", active);
        }
    }

    internal void SetMulliganActive(bool active) {
        if (!InteractionControl.IsUsable(mulliganDiscard)) return;
        mulliganDiscard!.ThemeTypeVariation = active
            ? GodotThemeVariations.SpatialDropTarget : mulliganDiscardVariation;
        mulliganDiscard.SetMeta("spatial_drop_active", active);
    }
}
