using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>One attack ability's simultaneous damage to distinct selected characters.</summary>
public sealed record SimultaneousAttackDamageRequest(
    Card Attacker, Card Source, IReadOnlyList<Card> Targets, long Amount, string Trigger, string Verb)
{
    /// <summary>Whether to settle each attacked character's retaliation in this procedure.</summary>
    public bool Retaliate { get; init; } = true;

    /// <summary>Each target's own share, parallel to <see cref="Targets"/>; null gives every target <see cref="Amount"/>.</summary>
    public IReadOnlyList<long>? Amounts { get; init; }

    /// <summary>Whether the damage belongs to the attacker's basic ATK power.</summary>
    public bool Basic { get; init; }

    /// <summary>Overkill granted to this exact attack before damage resolution.</summary>
    public bool GrantedOverkill { get; init; }

    internal long AmountFor(int index) => Amounts is null ? Amount : Amounts[index];
}
