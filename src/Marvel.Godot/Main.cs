using Godot;

namespace Marvel.Godot;

/// <summary>The desktop client's composition boundary and root scene.</summary>
public sealed partial class Main : Control
{
    /// <inheritdoc />
    public override void _Ready()
    {
        GetWindow().MinSize = new Vector2I(960, 600);
    }
}
