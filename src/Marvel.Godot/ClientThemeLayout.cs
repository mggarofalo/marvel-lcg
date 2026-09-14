using Godot;

namespace Marvel.Godot;

/// <summary>Defines the shared spacing relationships for procedural containers.</summary>
internal static class ClientThemeLayout
{
    internal static void Define(Theme theme, SpacingMetrics spacing)
    {
        theme.SetTypeVariation(GodotThemeVariations.TightStack, "VBoxContainer");
        theme.SetConstant("separation", GodotThemeVariations.TightStack, spacing.Small);
        theme.SetTypeVariation(GodotThemeVariations.Stack, "VBoxContainer");
        theme.SetConstant("separation", GodotThemeVariations.Stack, spacing.Medium);
        theme.SetTypeVariation(GodotThemeVariations.WideRow, "HBoxContainer");
        theme.SetConstant("separation", GodotThemeVariations.WideRow, spacing.Large);
        theme.SetTypeVariation(GodotThemeVariations.CompactRow, "HBoxContainer");
        theme.SetConstant("separation", GodotThemeVariations.CompactRow, spacing.ExtraSmall);
        theme.SetTypeVariation(GodotThemeVariations.PlayGrid, "GridContainer");
        theme.SetConstant("h_separation", GodotThemeVariations.PlayGrid, spacing.Large);
        theme.SetConstant("v_separation", GodotThemeVariations.PlayGrid, spacing.Medium);
        theme.SetTypeVariation(GodotThemeVariations.DataGrid, "GridContainer");
        theme.SetConstant("h_separation", GodotThemeVariations.DataGrid, spacing.Large);
        theme.SetConstant("v_separation", GodotThemeVariations.DataGrid, spacing.Small);
        theme.SetConstant("h_separation", "HFlowContainer", spacing.Small);
        theme.SetConstant("v_separation", "HFlowContainer", spacing.ExtraSmall);
    }
}
