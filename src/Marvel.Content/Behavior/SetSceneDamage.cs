using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Behavior;

/// <summary>Sets the damage already on an in-play character.</summary>
public sealed record SetSceneDamage(SceneCard Card, long Damage)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "set-damage";
}
