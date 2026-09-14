using Godot;

namespace Marvel.Godot;

/// <summary>A visible live player lane that can receive a hand-card play gesture.</summary>
internal sealed record BoardDropTarget(int Seat, Control Control);
