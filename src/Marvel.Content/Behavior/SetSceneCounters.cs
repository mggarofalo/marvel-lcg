using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Behavior;

/// <summary>Sets scheme threat or one printed all-purpose counter type to an exact value.</summary>
public sealed record SetSceneCounters(SceneCard Card, string Type, long Count)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "set-counters";
}
