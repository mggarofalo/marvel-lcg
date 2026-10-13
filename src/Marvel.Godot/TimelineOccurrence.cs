using Marvel.View;

namespace Marvel.Godot;

/// <summary>An authorized occurrence at a server history boundary.</summary>
internal sealed record TimelineOccurrence(int Cursor, EventPresentation Event);
