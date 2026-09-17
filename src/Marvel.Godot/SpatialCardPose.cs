using Godot;

namespace Marvel.Godot;

/// <summary>Persists and restores an authored card pose across local pointer treatments.</summary>
internal static class SpatialCardPose
{
    internal static void Store(Control control)
    {
        control.SetMeta("spatial_resting_position", control.Position);
        control.SetMeta("spatial_resting_rotation", control.Rotation);
        control.SetMeta("spatial_resting_card_z", control.ZIndex);
    }

    internal static void Restore(CardControl card)
    {
        if (!InteractionControl.IsUsable(card)
            || !card.HasMeta("spatial_resting_position")) return;
        card.Position = card.GetMeta("spatial_resting_position").AsVector2();
        card.Rotation = (float)card.GetMeta("spatial_resting_rotation").AsDouble();
        card.ZIndex = card.GetMeta("spatial_resting_card_z").AsInt32();
    }

    internal static int RestingZ(Control control, int fallback) =>
        control.HasMeta("spatial_interaction_z")
            ? Math.Max(fallback, control.GetMeta("spatial_interaction_z").AsInt32())
            : fallback;
}
