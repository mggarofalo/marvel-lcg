using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>An attack's damage roles and the activation, if any, responsible for it.</summary>
public sealed record AttackDamageRequest(
    Card Attacker, Card Source, Card Target, long Amount, string Trigger, string Verb,
    bool Retaliate = true, int? ActivationId = null)
{
    internal Occurrence? Origin { get; init; }
    internal EnemyAttack? EnemyAttack { get; init; }
    internal DamageReplacement? Replacement { get; init; }
}
