using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Behavior;

/// <summary>One deterministic arrangement applied after the legal deal.</summary>
public abstract record CoreSceneOperation
{
    /// <summary>The stable operation name included in a construction failure.</summary>
    public abstract string Name { get; }
}
