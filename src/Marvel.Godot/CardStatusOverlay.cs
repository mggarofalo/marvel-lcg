using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Layers visible hosted status cards onto the character they modify.</summary>
internal static class CardStatusOverlay
{
    internal static void AddTo(VBoxContainer content, BoardCardPresentation card)
    {
        if (card.Statuses.Count == 0)
        {
            return;
        }

        var badges = new HFlowContainer
        {
            Name = "StatusOverlays",
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        foreach (string status in card.Statuses.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string label = status.ToUpperInvariant();
            badges.AddChild(new Label
            {
                Name = $"Status{label.Replace(" ", string.Empty, StringComparison.Ordinal)}",
                Text = $"◆ {label}",
                ThemeTypeVariation = GodotThemeVariations.CardState,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            });
        }
        content.AddChild(badges);
    }
}
