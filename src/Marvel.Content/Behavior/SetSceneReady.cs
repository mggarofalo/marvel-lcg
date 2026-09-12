using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Behavior;

/// <summary>Sets whether an in-play card is ready.</summary>
public sealed record SetSceneReady(SceneCard Card, bool Ready)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "set-ready";
}
