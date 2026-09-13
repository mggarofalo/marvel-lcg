using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Behavior;

/// <summary>Sets rules-provided acceleration tokens beside the main scheme.</summary>
public sealed record SetSceneAccelerationTokens(long Count) : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "set-acceleration-tokens";
}
