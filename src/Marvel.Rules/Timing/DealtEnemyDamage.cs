using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>Damage one player's identity dealt to one enemy, with the excess beyond its remaining hit points.</summary>
/// <param name="Dealer">The identity credited with dealing the damage (rr:you-your.12-.14).</param>
/// <param name="Recipient">The enemy dealt the damage, captured before any defeat.</param>
/// <param name="Dealt">The damage dealt, independent of prevention (rr:damage.3.2).</param>
/// <param name="Excess">Damage dealt beyond the enemy's remaining hit points (rr:excess-damage).</param>
public sealed record DealtEnemyDamage(OccurrenceCard Dealer, Attacked Recipient, long Dealt, long Excess);
