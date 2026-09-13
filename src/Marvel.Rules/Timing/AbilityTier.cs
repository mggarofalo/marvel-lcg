namespace Marvel.Rules.Timing;

/// <summary>Everything waiting at one priority, which resolves before the next.</summary>
/// <param name="Priority">The tier.</param>
/// <param name="Abilities">What is waiting in it.</param>
/// <remarks>
/// A tier holding more than one ability is a <b>decision</b>, not an ordering
/// this code can make: <c>rr:forced.5</c> gives the choice to the first player,
/// and <c>rr:simultaneous-resolution</c> says the same of any two effects
/// sharing a bold trigger. Returning the group rather than a sorted list is what
/// keeps that choice visible instead of quietly resolving it by object id.
/// </remarks>
public readonly record struct AbilityTier(TimingPriority Priority, IReadOnlyList<PendingAbility> Abilities);
