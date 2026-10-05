using Godot;

namespace Marvel.Godot;

/// <summary>Keeps repeated activation in one gesture from reaching replacement table controls.</summary>
internal sealed class TableInputGestureBoundary
{
    private bool discardMouseRelease;

    internal bool Consume(InputEvent input)
    {
        if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouse)
        {
            if (mouse.Pressed)
            {
                discardMouseRelease = mouse.DoubleClick;
                return discardMouseRelease;
            }

            bool discard = discardMouseRelease;
            discardMouseRelease = false;
            return discard;
        }

        return false;
    }
}
