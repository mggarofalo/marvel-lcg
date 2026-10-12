using Marvel.Rules.Timing;

namespace Marvel.Rules.Events;

/// <summary>A forced ability's initial resolution outcome; a wire type.</summary>
/// <remarks>Occurrence/source identify the engine operation. Completed is false while a decision suspends it.
/// This is representation evidence, not an additional ability or player decision.</remarks>
public sealed record AbilityWindowResult(int Source, AbilityType Type, int Player,
    int Occurrence, string Description, bool Completed) : GameEvent
{
    /// <summary>The ability ordinal at this timing on the source card.</summary>
    public int Ordinal { get; init; }
}
