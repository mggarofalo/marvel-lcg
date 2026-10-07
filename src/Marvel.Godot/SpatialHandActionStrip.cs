using Godot;

namespace Marvel.Godot;

/// <summary>Places upright symbols at the hand card's edge or resource corner.</summary>
internal static class SpatialHandActionStrip
{
    internal static bool Place(CardControl card, Button button)
    {
        Control? overlay = card.GetNodeOrNull<Control>("HandActionOverlay");
        if (overlay is null)
        {
            overlay = new Control
            {
                Name = "HandActionOverlay", MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            card.AddChild(overlay);
        }
        button.ZAsRelative = true;
        button.ZIndex = 0;
        CardSymbolButtonStyle.Apply(button);
        overlay.AddChild(button);
        card.HideInteractionCue();
        button.SetMeta("spatial_hand_control", true);
        button.SetMeta("spatial_card_anchor", card.TargetId ?? -1);
        Callable.From(() => Refresh(card)).CallDeferred();
        return true;
    }

    internal static void Refresh(CardControl card)
    {
        if (!InteractionControl.IsUsable(card)
            || card.GetNodeOrNull<Control>("HandActionOverlay") is not { } overlay) return;
        foreach (Button button in overlay.GetChildren().OfType<Button>()) Position(card, button);
    }

    private static void Position(CardControl card, Button button)
    {
        if (!InteractionControl.IsUsable(card) || !InteractionControl.IsUsable(button)) return;
        float width = card.GetMeta("spatial_hand_exposed_width").AsSingle();
        Vector2 offset = new(Math.Max(0, (width - CardSymbolButtonStyle.HitSize) / 2), -28);
        if (button.GetMeta("card_control_intent", "").AsString() == "Generator"
            && card.HasMeta("card_resource_rect"))
        {
            Rect2 resources = card.GetMeta("card_resource_rect").AsRect2();
            offset = new Vector2(Math.Min(width - 44, resources.End.X - 16), resources.GetCenter().Y - 22);
        }
        button.Rotation = -card.Rotation;
        button.Position = ((Control)button.GetParent()).GetGlobalTransform().AffineInverse()
            * (((Control)card.GetParent()).GetGlobalTransform()
                * (card.Position + offset));
        button.Size = new Vector2(44, 44);
    }
}
