using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Damage already placed by a simultaneous batch, awaiting defeat and attack aftermath.</summary>
internal sealed record PreparedAttackDamage(
    Damage.PlacedDamage Placement, long RemainingHealth, CardKind Kind,
    int Controller, int Incarnation, AttackDamagePolicy Policy);
