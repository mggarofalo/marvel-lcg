using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Tracks the live source and return target for one transient inspector.</summary>
internal sealed class CardInspectorState
{
    internal BoardCardPresentation? Card { get; set; }

    internal Control? ReturnFocus { get; set; }

    internal Control? Source { get; set; }
}
