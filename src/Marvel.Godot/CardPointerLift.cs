using Godot;

namespace Marvel.Godot;

/// <summary>Lifts a physical card out of collection clipping without reparenting its lifetime.</summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable",
    Justification = "The card scene owns the temporary shadow node, released at drag completion or with its parent.")]
internal sealed class CardPointerLift
{
    private (Control Card, bool TopLevel, Vector2 Scale)? lifted;
    private Vector2 grabOffset;
    private Control? shadow;

    internal void Begin(Control card, Vector2 pointer)
    {
        Restore();
        grabOffset = pointer - card.GlobalPosition;
        lifted = (card, card.TopLevel, card.Scale);
        Vector2 scale = card.GetGlobalTransform().Scale;
        card.TopLevel = true;
        card.Scale = scale;
        card.ZIndex = 240;
        card.Modulate = Colors.White;
        shadow = new PanelContainer { Name = "DragContactShadow", MouseFilter = Control.MouseFilterEnum.Ignore,
            ShowBehindParent = true, Position = Vector2.Zero, Size = card.Size };
        using var style = new StyleBoxFlat { BgColor = Colors.Transparent,
            ShadowColor = new Color(0, 0, 0, 0.36f), ShadowSize = 7, ShadowOffset = new Vector2(0, 6),
            CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3, CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3 };
        shadow.AddThemeStyleboxOverride("panel", style);
        card.AddChild(shadow);
    }

    internal void Move(Control card, Vector2 pointer) =>
        card.GlobalPosition = pointer - grabOffset;

    internal void Restore()
    {
        if (InteractionControl.IsUsable(shadow)) { shadow!.Hide(); shadow.QueueFree(); }
        shadow = null;
        if (lifted is not { } pose) return;
        lifted = null;
        if (!InteractionControl.IsUsable(pose.Card)) return;
        Vector2 global = pose.Card.GlobalPosition;
        pose.Card.TopLevel = pose.TopLevel;
        pose.Card.Scale = pose.Scale;
        pose.Card.GlobalPosition = global;
    }
}
