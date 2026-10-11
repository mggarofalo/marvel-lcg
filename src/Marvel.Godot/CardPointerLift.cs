using Godot;

namespace Marvel.Godot;

/// <summary>Lifts a physical card out of collection clipping without reparenting its lifetime.</summary>
internal sealed class CardPointerLift
{
    private (Control Card, bool TopLevel, Vector2 Scale)? lifted;

    internal void Begin(Control card)
    {
        Restore();
        lifted = (card, card.TopLevel, card.Scale);
        Vector2 scale = card.GetGlobalTransform().Scale;
        card.TopLevel = true;
        card.Scale = scale;
        card.ZIndex = 240;
        card.Modulate = Colors.White;
    }

    internal static void Move(Control card, Vector2 pointer) =>
        card.GlobalPosition += pointer - SpatialCardFootprint.Face(card).GetCenter();

    internal void Restore()
    {
        if (lifted is not { } pose) return;
        lifted = null;
        if (!InteractionControl.IsUsable(pose.Card)) return;
        Vector2 global = pose.Card.GlobalPosition;
        pose.Card.TopLevel = pose.TopLevel;
        pose.Card.Scale = pose.Scale;
        pose.Card.GlobalPosition = global;
    }
}
