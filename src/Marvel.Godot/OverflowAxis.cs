namespace Marvel.Godot;

/// <summary>Which way an <see cref="OverflowCue"/>'s hidden rest lies.</summary>
internal enum OverflowAxis
{
    /// <summary>Beyond the right edge of a sideways-scrolling row.</summary>
    Across,

    /// <summary>Below the bottom edge of a vertically scrolling stack.</summary>
    Down,
}
