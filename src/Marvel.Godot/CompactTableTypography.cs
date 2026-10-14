namespace Marvel.Godot;

/// <summary>Native type grows within compact table slots while physical cards retain their table geometry.</summary>
internal static class CompactTableTypography
{
    internal static int Size(int standard, InterfaceScale requested) => standard
        + Math.Clamp(((int)requested - 100) / 20, 0, 2);
}
