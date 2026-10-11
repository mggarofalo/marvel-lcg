using Godot;

namespace Marvel.Godot;

/// <summary>Keeps repeated activation in one gesture from reaching replacement table controls.</summary>
internal sealed class TableInputGestureBoundary
{
    private bool discardMouseRelease;

    internal bool Consume(InputEvent input, Control? hovered = null)
    {
        if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left } mouse)
        {
            if (mouse.Pressed)
            {
                discardMouseRelease = mouse.DoubleClick && IsMutation(hovered);
                return discardMouseRelease;
            }

            bool discard = discardMouseRelease;
            discardMouseRelease = false;
            return discard;
        }

        return false;
    }

    internal static bool IsMutation(Control? control)
    {
        for (Node? node = control; node is not null; node = node.GetParent())
            if (node.HasMeta("table_mutation")) return node.GetMeta("table_mutation").AsBool();
        return false;
    }
}
