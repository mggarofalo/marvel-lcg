using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Explains the active attack keyword using the same query as damage resolution.</summary>
internal static class AttackOverkillDescription
{
    internal static string Context(World world, ICardFacts facts, Card enemy) =>
        Keywords.Has(world, enemy, Keywords.Overkill, facts)
            ? "Overkill: excess damage from a defeated ally hits its controller's identity. "
            : string.Empty;

    internal static string Defender(World world, ICardFacts facts, Card enemy, Card ally)
    {
        if (!Keywords.Has(world, enemy, Keywords.Overkill, facts)) return string.Empty;
        // rr:overkill.1 sends excess to "the identity of that ally's controller".
        Card identity = world.Seats[ally.Area.PlayArea.Player].IdentityCard;
        return $"Overkill: if {EffectiveCards.Title(ally, facts)} is defeated, excess damage hits "
            + $"{EffectiveCards.Title(identity, facts)}. ";
    }
}
