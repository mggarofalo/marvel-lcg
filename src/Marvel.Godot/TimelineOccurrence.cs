using Marvel.View;

namespace Marvel.Godot;

/// <summary>An authorized occurrence at a server history boundary.</summary>
internal sealed record TimelineOccurrence(int Cursor, EventPresentation Event)
{
    /// <summary>
    /// A summary restored from the server's history in place of the occurrences it stands for, which carry no
    /// activity facts; what it hides between it and earlier entries is unknown.
    /// </summary>
    public bool Restored { get; init; }
}
