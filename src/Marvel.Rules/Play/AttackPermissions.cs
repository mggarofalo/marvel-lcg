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
    /// <para>
    /// Ruling 8da857fcc70a81a2: "'(The Conqueror)' is not a subtitle, but part of
    /// his title", so every Kang villain and Kang (Master of Time) print a
    /// different title. Ruling 7b48d5829d4d0f62 confirms the minion is "a
    /// different character" from each Kang villain. MARVEL-894 (Q2) reads Fear
    /// of Kang's "Kang" as every enemy titled Kang, the minion included (ruling
    /// 13454feafb65378b discusses him under Fear of Kang). Every printed Kang
    /// title is "Kang (...)", so the engine matches that prefix; the spelling of
    /// the match is ours.
    /// </para>
    /// <para>
    /// A facedown Drone minion answers with its profile, not its face. Ruling
    /// 8da857fcc70a81a2 (part 5): "The facedown side of a Drone minion essentially
    /// does not matter as that side is not in play"; rr:in-play-and-out-of-play.13:
    /// "If a card is double-sided ..., the facedown side is out of play."
    /// </para>
    /// </remarks>
    internal static bool IsKang(ICardFacts facts, Card enemy) =>
        !EffectiveCards.HasProfile(enemy)
        && facts.Title(enemy.FaceId).StartsWith("Kang (", StringComparison.Ordinal);
}
