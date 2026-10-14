using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Changes the printed face at impact while preserving its physical slot and native text.</summary>
internal static class EventActivityFaceRendering
{
    internal static void Apply(CardControl card, BoardCardPresentation presentation)
    {
        if (card.GetNodeOrNull<Control>("CardSurface") is not Control surface) return;
        Control? old = surface.GetChildren().OfType<Control>()
            .FirstOrDefault(child => child.Name == "CardFace" || child.Name == "CardBack");
        if (old is null) return;
        CardLayoutMetrics layout = CardControl.LayoutFor(presentation, CardDisplaySize.Board, InterfaceScale.Standard)
            with { Width = (int)card.Size.X, MinimumHeight = (int)card.Size.Y,
                ReadingScale = (InterfaceScale)card.GetMeta("card_reading_scale", 100).AsInt32() };
        int index = old.GetIndex();
        surface.RemoveChild(old);
        old.QueueFree();
        Control body = CardFaceRendering.CreateBody(presentation, CardDisplaySize.Board, layout,
            InterfaceScale.Standard, card.FaceArt);
        surface.AddChild(body);
        surface.MoveChild(body, index);
        card.SetMeta("activity_face", presentation.FaceId ?? "");
    }
}
