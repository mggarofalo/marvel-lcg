using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Behavior;

/// <summary>Creates one rules-provided status card on an in-play character.</summary>
public sealed record GiveSceneStatus(SceneCard Host, string Status)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "give-status";
}
