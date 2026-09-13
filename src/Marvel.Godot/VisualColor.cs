namespace Marvel.Godot;

/// <summary>An sRGB color whose channels do not depend on a UI framework.</summary>
public readonly record struct VisualColor(byte Red, byte Green, byte Blue)
{
    /// <summary>Creates a color from the conventional 24-bit RRGGBB representation.</summary>
    public static VisualColor FromRgb(uint rgb) => new(
        (byte)(rgb >> 16),
        (byte)(rgb >> 8),
        (byte)rgb);
}
