namespace Marvel.Godot;

/// <summary>Product-chosen surface colors for commitments, choices and utilities.</summary>
public static class ButtonMaterials
{
    public static VisualColor Primary => VisualSystem.Palette.Accent;
    public static VisualColor PrimaryHover => VisualColor.FromRgb(0xF8CF6B);
    public static VisualColor PrimaryPressed => VisualColor.FromRgb(0xD9A738);
    public static VisualColor PrimaryEdge => VisualColor.FromRgb(0xC79835);
    public static VisualColor Secondary => VisualSystem.Palette.RaisedSurface;
    public static VisualColor SecondaryHover => VisualColor.FromRgb(0x263E4C);
    public static VisualColor SecondaryPressed => VisualColor.FromRgb(0x142631);
    public static VisualColor SecondaryEdge => VisualColor.FromRgb(0x4A6470);
    public static VisualColor HoverEdge => VisualColor.FromRgb(0x7795A1);
    public static VisualColor SelectedSurface => VisualColor.FromRgb(0x313F3F);
    public static VisualColor UnavailableSurface => VisualColor.FromRgb(0x111F28);
    public static VisualColor UnavailableEdge => VisualColor.FromRgb(0x30434E);
}
