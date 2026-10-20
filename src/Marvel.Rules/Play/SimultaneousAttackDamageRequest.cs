using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>One attack ability's simultaneous damage to distinct selected characters.</summary>
public sealed record SimultaneousAttackDamageRequest(
    Card Attacker, Card Source, IReadOnlyList<Card> Targets, long Amount, string Trigger, string Verb)
{
    /// <summary>Whether to settle each attacked character's retaliation in this procedure.</summary>
    public bool Retaliate { get; init; } = true;
}
