using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>One pointer operation addressed to a visible card's stable target id.</summary>
internal sealed record CardPointerGesture(
    BoardCardPresentation Card,
    Control Source,
    bool IsHandCard,
    Vector2 Position);
