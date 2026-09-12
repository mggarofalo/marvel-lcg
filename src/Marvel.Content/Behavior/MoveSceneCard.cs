using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Behavior;

/// <summary>Moves one existing physical card without changing its ownership.</summary>
public sealed record MoveSceneCard(SceneCard Card, SceneDestination Destination)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "move-card";
}
