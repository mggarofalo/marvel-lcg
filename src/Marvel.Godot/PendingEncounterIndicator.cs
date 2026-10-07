using Godot;

namespace Marvel.Godot;

/// <summary>Shows a public waiting-card count without retaining concealed card identities.</summary>
internal static class PendingEncounterIndicator
{
    internal static void Add(Control parent, Rect2 bounds, int count, int seat)
    {
        if (count == 0) return;
        string description = Description(count, seat);
        var indicator = new Control
        {
            Name = "PendingEncounterCards", Position = bounds.Position, Size = bounds.Size,
            MouseFilter = Control.MouseFilterEnum.Stop, TooltipText = description,
            AccessibilityName = description, ZIndex = 8,
        };
        parent.AddChild(indicator);
        float height = bounds.Size.Y - 6;
        float width = height * 0.7f;
        int shown = Math.Min(count, 3);
        for (int index = shown - 1; index >= 0; index--)
        {
            var back = new Panel
            {
                Name = $"EncounterBack{index}", Position = new Vector2(index * 4, index * 2),
                Size = new Vector2(width, height), MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            using var style = new StyleBoxFlat
            {
                BgColor = new Color("331b27"), BorderColor = new Color("e85b62"),
                BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
                CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4,
                CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4,
            };
            back.AddThemeStyleboxOverride("panel", style);
            indicator.AddChild(back);
            var mark = new Label { Text = "!", HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore };
            mark.AddThemeFontSizeOverride("font_size", 26);
            back.AddChild(mark);
            mark.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        }
        var caption = new Label
        {
            Name = "PendingEncounterCount", Text = $"{count}  Encounter\nWaiting to reveal",
            Position = new Vector2(width + 20, 0), Size = new Vector2(bounds.Size.X - width - 20, height),
            VerticalAlignment = VerticalAlignment.Center, MouseFilter = Control.MouseFilterEnum.Ignore,
            AccessibilityName = description,
        };
        caption.AddThemeFontSizeOverride("font_size", 15);
        indicator.AddChild(caption);
    }

    internal static string Compact(int count) => count == 0 ? string.Empty : $" · ▧ {count}";

    internal static string Description(int count, int seat) => count == 0 ? string.Empty
        : $"Player {seat + 1}: {count} encounter {(count == 1 ? "card" : "cards")} waiting to reveal";
}
