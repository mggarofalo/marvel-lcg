using Godot;

namespace Marvel.Godot;

/// <summary>Defines the fixed desktop workspace boundary for an active game.</summary>
internal static class DesktopTabletop
{
    internal static bool Uses(Vector2 viewport) => viewport.X >= 1920 && viewport.Y >= 1080;
}
