using System.Globalization;
using System.Text;

namespace Marvel.Cards.Extract;

/// <summary>Extracts the numeric attributes used by the engine from structured source fields.</summary>
internal static class PrintedNumbers
{
    /// <summary>Card types that have a stat box — <c>rr:character</c>.</summary>
    /// <remarks>
    /// "A character is a hero, alter-ego, ally, minion, or villain." A leader is
    /// the campaign expansions' own kind and prints the same box.
    /// </remarks>
    private static readonly string[] Characters =
        ["Hero", "AlterEgo", "Ally", "Minion", "Villain", "Leader"];

    /// <summary>Types that print a THW value: the ones that thwart.</summary>
    private static readonly string[] Thwarters = ["Hero", "Ally"];

    /// <summary>Types that print a SCH value: the ones that scheme.</summary>
    private static readonly string[] Schemers = ["Minion", "Villain", "Leader"];

    /// <summary>Types that print an ATK value. An alter-ego does not attack.</summary>
    private static readonly string[] Attackers =
        ["Hero", "Ally", "Minion", "Villain", "Leader"];

    /// <summary>
    /// The resource letters, in the order a cost is written.
    /// </summary>
    /// <remarks>
    /// <c>rr:resource</c> names four: physical, energy, mental and wild. The
    /// letters and their order are this engine's choice — <c>R</c>, <c>Y</c>,
    /// <c>B</c> and <c>G</c> for the printed colours — and a card generating
    /// two different kinds is written in this order so that one card cannot
    /// spell the same pair two ways.
    /// </remarks>
    private static readonly (string Field, char Letter)[] Resources =
    [
        ("resource_physical", 'R'),
        ("resource_energy", 'Y'),
        ("resource_mental", 'B'),
        ("resource_wild", 'G'),
    ];

    /// <summary>Whether this character type prints a field of the given kind.</summary>
    internal static bool HasStat(string kind, string name) => name switch
    {
        "ATK" => Attackers.Contains(kind, StringComparer.Ordinal),
        "THW" => Thwarters.Contains(kind, StringComparer.Ordinal),
        "SCH" => Schemers.Contains(kind, StringComparer.Ordinal),
        "DEF" or "REC" or "HS" or "HP" => Characters.Contains(kind, StringComparer.Ordinal),
        _ => false,
    };

    public static void StatBox(SdbCard card, string kind, SortedDictionary<string, string> into)
    {
        if (string.Equals(kind, "Attachment", StringComparison.Ordinal))
        {
            // `rr:attachment.1` — an attachment's printed numbers are modifiers
            // on the card it is attached to, not values of its own. A zero
            // modifier is a printed zero and is kept; upstream's -1 is its
            // "no box" marker and is not.
            Modifier(card, "attack", "ATK+", into);
            Modifier(card, "scheme", "SCH+", into);
            Modifier(card, "thwart", "THW+", into);
            return;
        }

        if (!Characters.Contains(kind, StringComparer.Ordinal))
        {
            return;
        }

        // `rr:hit-points.2.3`'s per-player icon. Upstream carries it as a flag
        // beside the number, and the engine writes it as a `*` suffix that
        // `CardCatalog.PrintedValue` multiplies.
        Value(card, "health", "HP", into, perPlayer: card.Flag("health_per_hero"));

        if (Attackers.Contains(kind, StringComparer.Ordinal))
        {
            // `rr:consequential-damage.1` — an ally's ATK and THW carry one
            // star per point of consequential damage, which upstream records
            // as a separate count rather than in the number.
            Value(card, "attack", "ATK", into, stars: card.Number("attack_cost") ?? 0);
        }

        if (Thwarters.Contains(kind, StringComparer.Ordinal))
        {
            Value(card, "thwart", "THW", into, stars: card.Number("thwart_cost") ?? 0);
        }

        if (Schemers.Contains(kind, StringComparer.Ordinal))
        {
            Value(card, "scheme", "SCH", into);
        }

        Value(card, "defense", "DEF", into);
        Value(card, "recover", "REC", into);
        Value(card, "hand_size", "HS", into);
    }

    // Numeric engine attributes omit non-numeric boxes. StatAnnotations keeps
    // their source symbols separate from these evaluated numeric bases.
    private static void Value(
        SdbCard card, string field, string key, SortedDictionary<string, string> into,
        bool perPlayer = false, int stars = 0)
    {
        if (card.Number(field) is not { } printed)
        {
            return;
        }

        // Numeric engine attributes use zero as the base for a variable.
        // StatAnnotations preserves the printed X independently.
        int value = Math.Max(0, printed);
        into[key] = value.ToString(CultureInfo.InvariantCulture)
            + (perPlayer ? "*" : string.Empty)
            + new string('*', stars);
    }

    private static void Modifier(
        SdbCard card, string field, string key, SortedDictionary<string, string> into)
    {
        if (card.Number(field) is { } value && value >= 0)
        {
            into[key] = value.ToString(CultureInfo.InvariantCulture);
        }
    }

    public static void Threat(SdbCard card, SortedDictionary<string, string> into)
    {
        // `rr:main-scheme-main-scheme-deck.1` -- a main scheme's first stage is
        // "1A", the setup side, and the threat values are printed on 1B. It is
        // flipped before the game starts. Upstream carries a zero for the A
        // side, and a target threat of zero is a scheme already complete.
        if (card.Text("stage") is { Length: > 0 } stage
            && stage.EndsWith('A')
            && stage.Length > 1)
        {
            return;
        }

        // `rr:threat.1` — a scheme's threat values are per player unless the
        // card prints a fixed number, which is the opposite way round from the
        // stat box: upstream flags the *fixed* case, so the star is the
        // default and the flag removes it.
        Amount(card, "base_threat", "base_threat_fixed", "StartingThreat", into);
        Amount(card, "threat", "threat_fixed", "TargetThreat", into);
        Amount(card, "escalation_threat", "escalation_threat_fixed", "EscalationThreat", into);
    }

    private static void Amount(
        SdbCard card, string field, string fixedField, string key,
        SortedDictionary<string, string> into)
    {
        if (card.Number(field) is not { } value)
        {
            return;
        }

        into[key] = Math.Max(0, value).ToString(CultureInfo.InvariantCulture)
            + (card.Flag(fixedField) ? string.Empty : "*");
    }

    public static void Cost(SdbCard card, string kind, SortedDictionary<string, string> into)
    {
        if (card.Number("cost") is { } cost)
        {
            // `rr:x` — "X is a variable whose value is defined by the card".
            // Upstream writes a printed X as -1.
            into["Cost"] = cost < 0
                ? "X"
                : cost.ToString(CultureInfo.InvariantCulture)
                  + (card.Flag("cost_star") ? "*" : string.Empty);
        }

        if (Resources.Aggregate(
                new StringBuilder(),
                (letters, resource) => letters.Append(
                    new string(resource.Letter, card.Number(resource.Field) ?? 0)))
            .ToString() is { Length: > 0 } generated)
        {
            into["RES"] = generated;
        }

        if (card.Number("boost") is { } boost)
        {
            // `rr:boost-boost-icon.1` — the star is not a boost icon, so the
            // count is the number alone. Upstream keeps them in two fields for
            // the same reason.
            into["Boost"] = boost.ToString(CultureInfo.InvariantCulture);
        }
    }

}
