using Godot;

namespace Marvel.Godot;

/// <summary>Defines panel treatments that distinguish the table's semantic surfaces.</summary>
internal static class ClientThemeSurfaces
{
    internal static void Define(
        Theme theme, Color surface, Color raised, Color outline, Color amber, Color encounter, Color hero)
    {
        Panel(theme, GodotThemeVariations.ShellPanel, Flat(surface, Alpha(outline, 0.46f), 1, 18, M(22, 18, 22, 18)));
        Panel(theme, GodotThemeVariations.SurfacePanel, Flat(raised, Alpha(outline, 0.38f), 1, 12, M(16, 14, 16, 14)));
        Panel(theme, GodotThemeVariations.TabletopDock, Flat(raised, Alpha(outline, 0.38f), 1, 12, M(16, 12, 16, 12)));
        Panel(theme, GodotThemeVariations.TabletopShelf, Flat(raised, Alpha(outline, 0.38f), 1, 12, M(16, 8, 16, 8)));
        Panel(theme, GodotThemeVariations.TabletopSeatStrip, Flat(raised, Alpha(outline, 0.5f), 1, 10, M(16, 0, 16, 0)));
        Panel(theme, GodotThemeVariations.StatusPanel, Flat(Alpha(amber, 0.14f), Alpha(amber, 0.62f), 1, 8, M(14, 9, 14, 9), borderLeft: 5));
        Panel(theme, GodotThemeVariations.DangerStatusPanel, Flat(Alpha(encounter, 0.2f), encounter, 2, 8, M(14, 9, 14, 9), borderLeft: 7));
        Panel(theme, GodotThemeVariations.BoardArea, Flat(raised, Alpha(outline, 0.5f), 1, 10, M(16, 14, 16, 14)));
        Panel(theme, GodotThemeVariations.BoardCard, Flat(raised.Lightened(0.06f), Alpha(amber, 0.58f), 1, 6, M(11, 9, 11, 9), borderLeft: 4));
        Panel(theme, GodotThemeVariations.ConcealedCard, Flat(surface.Darkened(0.2f), Alpha(outline, 0.55f), 1, 6, M(11, 9, 11, 9)));
        Panel(theme, GodotThemeVariations.FocusedCard, Flat(raised.Lightened(0.12f), amber, 3, 6, M(11, 9, 11, 9), borderLeft: 7));
        Panel(theme, GodotThemeVariations.IdentityCard, Flat(raised.Lightened(0.06f), hero, 1, 12, M(16, 14, 16, 14), borderLeft: 8));
        Panel(theme, GodotThemeVariations.PlayerCard, Flat(raised.Lightened(0.06f), amber, 1, 12, M(16, 14, 16, 14), borderLeft: 8));
        Panel(theme, GodotThemeVariations.EnemyCard, Flat(raised.Lightened(0.02f), encounter, 2, 12, M(16, 14, 16, 14), borderLeft: 10));
        Panel(theme, GodotThemeVariations.SchemeCard, Flat(surface.Lightened(0.08f), encounter, 2, 12, M(16, 14, 16, 14), borderBottom: 8));
        Panel(theme, GodotThemeVariations.EnvironmentCard, Flat(surface.Lightened(0.05f), outline, 2, 12, M(16, 14, 16, 14), borderLeft: 5));
        Panel(theme, GodotThemeVariations.CardArtWell, Flat(surface.Darkened(0.18f), Alpha(outline, 0.55f), 1, 7, M(8, 8, 8, 8)));
    }

    private static void Panel(Theme theme, string variation, StyleBoxFlat style)
    {
        theme.SetTypeVariation(variation, "PanelContainer");
        theme.SetStylebox("panel", variation, style);
        style.Dispose();
    }

    private static StyleBoxFlat Flat(
        Color background, Color border, int width, int radius,
        Vector4 margins, int? borderLeft = null, int? borderBottom = null) => new()
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = borderLeft ?? width,
            BorderWidthTop = width,
            BorderWidthRight = width,
            BorderWidthBottom = borderBottom ?? width,
            CornerRadiusTopLeft = radius,
            CornerRadiusTopRight = radius,
            CornerRadiusBottomLeft = radius,
            CornerRadiusBottomRight = radius,
            ContentMarginLeft = margins.X,
            ContentMarginTop = margins.Y,
            ContentMarginRight = margins.Z,
            ContentMarginBottom = margins.W,
        };

    private static Vector4 M(float left, float top, float right, float bottom) =>
        new(left, top, right, bottom);

    private static Color Alpha(Color color, float alpha) => new(color.R, color.G, color.B, alpha);
}
