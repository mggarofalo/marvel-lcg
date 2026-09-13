using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Behavior;

/// <summary>Replaces the current villain with one later stage from its legal deck.</summary>
public sealed record SetSceneVillain(SceneCard Card) : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "set-villain";
}
