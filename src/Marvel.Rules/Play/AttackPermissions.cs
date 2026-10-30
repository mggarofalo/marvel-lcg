using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Character-specific prohibitions on attacking particular enemies.</summary>
public static class AttackPermissions
{
    /// <summary>Engine-selected derived modifier forbidding its character to attack any Kang.</summary>
    /// <remarks>
    /// Fear of Kang (11049): "You cannot attack Kang." Ruling af0e9693cc645666:
    /// "the 'you' in 'Fear of Kang' only refers to the identity, and you could
    /// attack Kang with allies." The modifier is therefore granted to one
    /// identity and read for the attacking character, never for its player.
    /// </remarks>
    public const string KangProhibitedField = "kang_attack_prohibited";

    /// <summary>Whether <paramref name="attacker"/> may attack <paramref name="enemy"/>.</summary>
    /// <remarks>
    /// rr:cannot: "The word cannot is absolute, and cannot be countermanded by
    /// other abilities or effects." A stunned attacker attacks nothing, so this
    /// is only consulted for an enemy that would actually be attacked.
    /// </remarks>
    public static bool CanAttack(World world, Card attacker, Card enemy)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(enemy);
        return !IsKang(world.Facts, enemy)
            || StateFields.Modified(world, attacker, KangProhibitedField, world.Facts, world.Players) <= 0;
    }

    /// <summary>
    /// Whether an enemy is "Kang" for a card that names him without a subtitle.
    /// </summary>
    /// <remarks>
    /// Ruling 8da857fcc70a81a2: "'(The Conqueror)' is not a subtitle, but part of
    /// his title", so every Kang villain and Kang (Master of Time) print a
    /// different title. Ruling 7b48d5829d4d0f62 confirms the minion is "a
    /// different character" from each Kang villain. MARVEL-894 (Q2) reads Fear
    /// of Kang's "Kang" as every enemy titled Kang, the minion included (ruling
    /// 13454feafb65378b discusses him under Fear of Kang). The engine therefore
    /// matches the bare title and every parenthesised Kang title; the spelling of
    /// that match is ours.
    /// </remarks>
    internal static bool IsKang(ICardFacts facts, Card enemy)
    {
        if (EffectiveCards.HasProfile(enemy)) return false;
        string title = facts.Title(enemy.FaceId);
        return string.Equals(title, "Kang", StringComparison.Ordinal)
            || title.StartsWith("Kang (", StringComparison.Ordinal);
    }
}
