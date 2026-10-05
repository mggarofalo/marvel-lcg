using Godot;

namespace Marvel.Godot;

/// <summary>Places direct selectors in a reserved hand strip or an upright table sidecar.</summary>
internal static class CardInteractionControlPlacement
{
    internal static bool Place(
        CardControl card,
        Button button)
    {
        if (card.GetParent() is not Control) return false;
        if (card.HasMeta("spatial_hand_index"))
            return SpatialHandActionStrip.Place(card, button);
        if (!card.HasMeta("spatial_resting_z"))
            return card.AddInteractionControl(button);
        VBoxContainer sidecar = SpatialCardSidecar.For(card);
        sidecar.AddChild(button);
        TableCompactButtonStyle.Apply(button);
        button.CustomMinimumSize = new Vector2(0, 44);
        button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        button.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        button.ZAsRelative = true;
        button.ZIndex = 0;
        button.SetMeta("spatial_upright_control", true);
        button.SetMeta("spatial_card_anchor", card.TargetId ?? -1);
        Callable.From(() => SpatialCardSidecar.Place(card)).CallDeferred();
        return true;
    }
}
