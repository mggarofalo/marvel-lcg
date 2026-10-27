using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>An attack's damage roles and the activation, if any, responsible for it.</summary>
public sealed record AttackDamageRequest(
    Card Attacker, Card Source, Card Target, long Amount, string Trigger, string Verb,
    bool Retaliate = true, int? ActivationId = null)
{
    /// <summary>Whether damage belongs to the character's basic ATK power.</summary>
    public bool Basic { get; init; }

    /// <summary>Overkill granted to this exact attack before damage resolution.</summary>
    public bool GrantedOverkill { get; init; }

    /// <summary>The exact attack occurrence retained across suspended ability execution; null uses the current occurrence.</summary>
    public Occurrence? Origin { get; init; }
    internal EnemyAttack? EnemyAttack { get; init; }
    internal DamageReplacement? Replacement { get; init; }
    internal PreparedAttackDamage? Prepared { get; init; }
}
