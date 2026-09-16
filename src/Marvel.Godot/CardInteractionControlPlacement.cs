using Godot;

namespace Marvel.Godot;

internal static class CardInteractionControlPlacement
{
    internal static bool Place(
        CardControl card,
        Button button,
        IReadOnlyDictionary<CardControl, List<Button>> controls)
    {
        if (card.GetParent() is not Control table)
        {
            return false;
        }

        table.AddChild(button);
        SizeButton(card, button);
        int ordinal = controls.GetValueOrDefault(card)?.Count ?? 0;
        button.Position = card.HasMeta("spatial_hand_index")
            ? PlaceOnHandCard(card, button, ordinal)
            : AvoidOverlap(table, button, controls, Desired(card, button, ordinal));
        button.Rotation = 0;
        button.SetMeta("spatial_upright_control", true);
        button.SetMeta("spatial_card_anchor", card.TargetId ?? -1);
        return true;
    }

    private static void SizeButton(CardControl card, Button button)
    {
        Vector2 minimum = button.GetCombinedMinimumSize();
        float authoredWidth = card.HasMeta("spatial_hand_index")
            ? 80
            : button.Name.ToString().EndsWith("Generator", StringComparison.Ordinal)
                ? 104
                : button.Name.ToString().EndsWith("Submit", StringComparison.Ordinal)
                    ? 108
                    : 112;
        button.Size = new Vector2(Math.Max(authoredWidth, minimum.X), Math.Max(48, minimum.Y));
        button.CustomMinimumSize = button.Size;
    }

    private static Vector2 Desired(CardControl card, Button button, int ordinal)
    {
        float x = card.HasMeta("spatial_exhausted")
            ? card.Position.X + card.CustomMinimumSize.Y + 10
            : card.Position.X + Math.Max(8, (card.CustomMinimumSize.X - button.Size.X) / 2);
        float y = card.HasMeta("spatial_exhausted")
            ? card.Position.Y + 18 + ordinal * 48
            : card.Position.Y - 56 - ordinal * 56;
        return new Vector2(x, y);
    }

    private static Vector2 PlaceOnHandCard(CardControl card, Button button, int ordinal)
    {
        Control overlay = card.GetNodeOrNull<Control>("SpatialControls") ?? new Control
        {
            Name = "SpatialControls",
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 600,
        };
        if (overlay.GetParent() is null)
        {
            card.AddChild(overlay);
        }
        button.Reparent(overlay);
        button.ZAsRelative = false;
        return new Vector2(8,
            card.CustomMinimumSize.Y + 4 - ordinal * (button.Size.Y + 8));
    }

    private static Vector2 AvoidOverlap(
        Control table,
        Button button,
        IReadOnlyDictionary<CardControl, List<Button>> controls,
        Vector2 desired)
    {
        Vector2 position = new(
            desired.X, Math.Min(desired.Y, table.CustomMinimumSize.Y - 56));
        Button[] occupied = [.. controls.Values.SelectMany(value => value)
            .Where(InteractionControl.IsUsable)];
        for (int attempt = 0; attempt <= occupied.Length; attempt++)
        {
            Rect2 rect = new(position, button.Size);
            Button? overlap = occupied.FirstOrDefault(existing =>
                rect.Intersects(new Rect2(existing.Position, existing.Size)));
            if (overlap is null)
            {
                return position;
            }
            position.X = overlap.Position.X + overlap.Size.X + 8;
        }
        return new Vector2(
            Math.Min(position.X, table.CustomMinimumSize.X - button.Size.X),
            position.Y);
    }
}
