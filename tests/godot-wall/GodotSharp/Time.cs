namespace Godot;

/// <summary>Enough of a Godot surface to be worth banning.</summary>
public static class Time
{
    /// <summary>Wall-clock milliseconds — one of the four AGENTS.md forbids.</summary>
    public static ulong GetTicksMsec() => 0;
}
