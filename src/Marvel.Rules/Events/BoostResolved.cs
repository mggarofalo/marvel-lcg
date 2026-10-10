namespace Marvel.Rules.Events;

/// <summary>An activation's authorized boost contribution after its Boost ability resolves.</summary>
/// <param name="Card">The revealed boost card.</param>
/// <param name="Enemy">The activating enemy.</param>
/// <param name="Icons">The effective icons applied, including modifiers.</param>
/// <param name="Attacking">Whether the contribution is to ATK rather than SCH.</param>
/// <param name="Strength">The enemy's current modified stat after this contribution.</param>
public sealed record BoostResolved(int Card, int Enemy, long Icons, bool Attacking, long Strength) : GameEvent;
