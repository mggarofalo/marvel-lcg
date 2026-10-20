using System.Collections.Immutable;

namespace Marvel.Rules.State;

/// <summary>Attack-completion facts retained for the keyword's shared forced-response window.</summary>
public sealed record RetaliationContext
{
    /// <summary>Captures each physical recipient once for one completed attack.</summary>
    public RetaliationContext(int attacker, int attackerIncarnation, bool ranged,
        IEnumerable<RetaliationRecipient> recipients, string trigger = "Attack")
    {
        Attacker = attacker;
        AttackerIncarnation = attackerIncarnation;
        Ranged = ranged;
        Recipients = [.. recipients.DistinctBy(item => (item.Attack.Card, item.Attack.Incarnation))];
        Trigger = trigger;
    }
    /// <summary>Retains a newly attacked physical recipient before damage settles.</summary>
    public RetaliationContext Include(Card card, ICardFacts facts) => new(
        Attacker, AttackerIncarnation, Ranged,
        Recipients.Append(new(Attacked.Capture(card, facts), card.Area.Id)), Trigger);

    /// <summary>The attacking character's original physical object.</summary>
    public int Attacker { get; }
    /// <summary>The attacking character's original incarnation.</summary>
    public int AttackerIncarnation { get; }
    /// <summary>Whether this attack ignores retaliate.</summary>
    public bool Ranged { get; }
    /// <summary>Ordered immutable physical recipients.</summary>
    public ImmutableArray<RetaliationRecipient> Recipients { get; }
    /// <summary>The originating attack's event provenance.</summary>
    public string Trigger { get; }
}
