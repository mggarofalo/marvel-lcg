using Godot;

namespace Marvel.Godot;

/// <summary>Bounds each hand card's upright controls to its exposed fan segment.</summary>
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
        float width = Math.Max(44, card.GetMeta("spatial_hand_exposed_width").AsSingle() - 12);
        button.ZAsRelative = true;
        button.ZIndex = 0;
        button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        button.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        button.CustomMinimumSize = new Vector2(width, 44);
        button.Size = new Vector2(width, 44);
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
        button.Rotation = -card.Rotation;
        button.Position = ((Control)button.GetParent()).GetGlobalTransform().AffineInverse()
            * (((Control)card.GetParent()).GetGlobalTransform()
                * (card.Position + new Vector2(6, card.Size.Y - 54)));
        button.Size = new Vector2(Math.Max(44, width - 12), 44);
    }
}
