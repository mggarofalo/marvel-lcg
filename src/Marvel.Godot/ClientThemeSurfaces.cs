using Godot;

namespace Marvel.Godot;

/// <summary>Defines the scale-aware panel frames in the desktop theme.</summary>
internal static class ClientThemeSurfaces
{
    internal static void Define(Theme theme, DensityMetrics density)
    {
        ClientTheme.Panel(theme, GodotThemeVariations.ShellPanel, ClientTheme.Flat(
            ClientTheme.Surface, ClientTheme.Alpha(ClientTheme.Outline, 0.46f),
            new StyleFrame(1, 18, density.ShellHorizontal, density.ShellVertical)));
        ClientTheme.Panel(theme, GodotThemeVariations.SurfacePanel, ClientTheme.Flat(
            ClientTheme.Raised, ClientTheme.Alpha(ClientTheme.Outline, 0.38f),
            new StyleFrame(1, 12, density.SurfaceHorizontal, density.SurfaceVertical)));
        ClientTheme.Panel(theme, GodotThemeVariations.StatusPanel, ClientTheme.Flat(
            ClientTheme.Alpha(ClientTheme.Amber, 0.14f), ClientTheme.Alpha(ClientTheme.Amber, 0.62f),
            new StyleFrame(1, 8, density.StatusHorizontal, density.StatusVertical, LeftBorder: 5)));
        ClientTheme.Panel(theme, GodotThemeVariations.DangerStatusPanel, ClientTheme.Flat(
            ClientTheme.Alpha(ClientTheme.Encounter, 0.2f), ClientTheme.Encounter,
            new StyleFrame(2, 8, density.StatusHorizontal, density.StatusVertical, LeftBorder: 7)));
        ClientTheme.Panel(theme, GodotThemeVariations.BoardArea, ClientTheme.Flat(
            ClientTheme.Raised, ClientTheme.Alpha(ClientTheme.Outline, 0.5f),
            new StyleFrame(1, 10, density.BoardHorizontal, density.BoardVertical)));
        ClientTheme.Panel(theme, GodotThemeVariations.BoardCard, ClientTheme.Flat(
            ClientTheme.Raised.Lightened(0.06f), ClientTheme.Alpha(ClientTheme.Amber, 0.58f),
            new StyleFrame(1, 6, density.CompactCardHorizontal, density.CompactCardVertical, LeftBorder: 4)));
        ClientTheme.Panel(theme, GodotThemeVariations.ConcealedCard, ClientTheme.Flat(
            ClientTheme.Surface.Darkened(0.2f), ClientTheme.Alpha(ClientTheme.Outline, 0.55f),
            new StyleFrame(1, 6, density.CompactCardHorizontal, density.CompactCardVertical)));
        ClientTheme.Panel(theme, GodotThemeVariations.FocusedCard, ClientTheme.Flat(
            ClientTheme.Raised.Lightened(0.12f), ClientTheme.Amber,
            new StyleFrame(3, 6, density.CompactCardHorizontal, density.CompactCardVertical, LeftBorder: 7)));
        ClientTheme.Panel(theme, GodotThemeVariations.IdentityCard, ClientTheme.Flat(
            ClientTheme.Raised.Lightened(0.06f), ClientTheme.Hero,
            new StyleFrame(1, 12, density.FullCardHorizontal, density.FullCardVertical, LeftBorder: 8)));
        ClientTheme.Panel(theme, GodotThemeVariations.PlayerCard, ClientTheme.Flat(
            ClientTheme.Raised.Lightened(0.06f), ClientTheme.Amber,
            new StyleFrame(1, 12, density.FullCardHorizontal, density.FullCardVertical, LeftBorder: 8)));
        ClientTheme.Panel(theme, GodotThemeVariations.EnemyCard, ClientTheme.Flat(
            ClientTheme.Raised.Lightened(0.02f), ClientTheme.Encounter,
            new StyleFrame(2, 12, density.FullCardHorizontal, density.FullCardVertical, LeftBorder: 10)));
        ClientTheme.Panel(theme, GodotThemeVariations.SchemeCard, ClientTheme.Flat(
            ClientTheme.Surface.Lightened(0.08f), ClientTheme.Encounter,
            new StyleFrame(2, 12, density.FullCardHorizontal, density.FullCardVertical, BottomBorder: 8)));
        ClientTheme.Panel(theme, GodotThemeVariations.EnvironmentCard, ClientTheme.Flat(
            ClientTheme.Surface.Lightened(0.05f), ClientTheme.Outline,
            new StyleFrame(2, 12, density.FullCardHorizontal, density.FullCardVertical, LeftBorder: 5)));
        ClientTheme.Panel(theme, GodotThemeVariations.CardArtWell, ClientTheme.Flat(
            ClientTheme.Surface.Darkened(0.18f), ClientTheme.Alpha(ClientTheme.Outline, 0.55f),
            new StyleFrame(1, 7, density.ArtWellInset, density.ArtWellInset)));
    }
}
