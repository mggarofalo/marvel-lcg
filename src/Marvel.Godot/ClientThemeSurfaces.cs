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
        Panel(theme, GodotThemeVariations.TabletopEncounterRail, FlatEdges(
            Alpha(encounter, 0.035f), encounter, 0, M(12, 10, 12, 16), M(0, 0, 0, 2)));
        Panel(theme, GodotThemeVariations.TabletopPlayerRail, FlatEdges(
            Alpha(hero, 0.035f), hero, 0, M(12, 16, 12, 10), M(0, 2, 0, 0)));
        Panel(theme, GodotThemeVariations.TabletopZone, Flat(
            Alpha(raised, 0.16f), Alpha(outline, 0.18f), 0, 8, M(8, 6, 8, 8)));
        Panel(theme, GodotThemeVariations.TabletopPile, FlatEdges(
            raised.Darkened(0.04f), Alpha(outline, 0.62f), 7, M(10, 8, 12, 10), M(1, 1, 5, 5)));
        Panel(theme, GodotThemeVariations.SpatialVillainMat, Flat(
            Alpha(encounter.Darkened(0.45f), 0.72f), Alpha(encounter, 0.66f), 1, 44,
            M(18, 14, 18, 14), borderBottom: 3));
        Panel(theme, GodotThemeVariations.SpatialPlayerMat, Flat(
            Alpha(hero.Darkened(0.55f), 0.74f), Alpha(hero, 0.5f), 1, 38,
            M(18, 14, 18, 14), borderLeft: 3));
        Panel(theme, GodotThemeVariations.SpatialPile, FlatEdges(
            surface.Darkened(0.12f), Alpha(outline, 0.8f), 9,
            M(8, 8, 8, 8), M(2, 2, 7, 7)));
        Panel(theme, GodotThemeVariations.SpatialDecision, Flat(
            Alpha(surface.Darkened(0.08f), 0.94f), Alpha(amber, 0.62f), 1, 9,
            M(14, 12, 14, 12), borderLeft: 5));
        Panel(theme, GodotThemeVariations.SpatialDropTarget, Flat(
            Alpha(hero, 0.12f), amber, 3, 38, M(18, 14, 18, 14)));
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
        Vector4 margins, int? borderLeft = null, int? borderBottom = null) =>
        FlatEdges(background, border, radius, margins, new Vector4(
            borderLeft ?? width, width, width, borderBottom ?? width));

    private static StyleBoxFlat FlatEdges(
        Color background, Color border, int radius, Vector4 margins, Vector4 borders) => new()
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = (int)borders.X,
            BorderWidthTop = (int)borders.Y,
            BorderWidthRight = (int)borders.Z,
            BorderWidthBottom = (int)borders.W,
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
