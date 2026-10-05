using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Adds upright captions that remain independent from rotated table objects.</summary>
internal static class SpatialTableLabel
{
    internal static void Persistent(CardControl control, string zone,
        IReadOnlyList<BoardAreaPresentation> areas, BoardCardPresentation card,
        bool hasDrawer = false)
    {
        if (zone is not ("SupportsArea" or "UpgradesArea")) return;
        string? host = areas.FirstOrDefault(area => area.Host >= 0
            && area.Cards.Any(candidate => candidate.TargetId == card.TargetId))?.HostedBy;
        if (hasDrawer && string.IsNullOrWhiteSpace(host)) return;
        string caption = string.IsNullOrWhiteSpace(host)
            ? zone == "SupportsArea" ? "Supports" : "Upgrades"
            : $"Attached to {host}";
        SpatialCardSidecar.Caption(control, caption);
    }

    internal static void Add(
        Control surface,
        string name,
        string text,
        Vector2 position,
        float width = 0,
        int z = 18)
    {
        surface.AddChild(new Label
        {
            Name = name,
            Text = text,
            Position = position,
            ThemeTypeVariation = GodotThemeVariations.Caption,
            ZIndex = z,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Size = width > 0 ? new Vector2(width, 22) : Vector2.Zero,
            CustomMinimumSize = width > 0 ? new Vector2(width, 22) : Vector2.Zero,
            ClipText = width > 0,
            TextOverrunBehavior = width > 0
                ? TextServer.OverrunBehavior.TrimEllipsis
                : TextServer.OverrunBehavior.NoTrimming,
        });
    }
}
