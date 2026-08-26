using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Marvel.Cards.Extract;

/// <summary>
/// The printed facts the engine reads, derived from the vendored snapshot.
/// </summary>
/// <remarks>
/// <para>
/// <b>Two sources and one authority.</b> A card's stat box is structured data
/// upstream — <c>attack</c>, <c>health</c>, <c>base_threat</c> — and its
/// keywords are sentences in the text box. Both are transcriptions of the same
/// printed card, so both are read here, and neither is an engine's opinion of
/// what a card does.
/// </para>
/// <para>
/// <b>What the keys are called is our choice.</b> <c>HP</c>, <c>ATK</c>,
/// <c>SCH</c>, <c>ATK+</c> — the Rules Reference names the values but not the
/// spelling of a JSON key, and <c>StateFields</c> is written against these. The
/// only property they have to keep is holding still.
/// </para>
/// </remarks>
internal static partial class Printed
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
    /// Types that carry a class — <c>rr:aspect-card</c>, <c>rr:hero-card</c>.
    /// </summary>
    /// <remarks>
    /// The player card types, and not the identity: a hero's aspect is chosen
    /// by the deck, not printed on the identity. An encounter-faction ally
    /// still carries one, because the class is a property of the card and
    /// "Encounter" is one of the answers.
    /// </remarks>
    private static readonly string[] Classed =
        ["Ally", "Event", "Resource", "Support", "Upgrade", "PlayerSideScheme"];

    private static readonly Dictionary<string, string> Types = new(StringComparer.Ordinal)
    {
        ["hero"] = "Hero",
        ["alter_ego"] = "AlterEgo",
        ["ally"] = "Ally",
        ["event"] = "Event",
        ["resource"] = "Resource",
        ["support"] = "Support",
        ["upgrade"] = "Upgrade",
        ["attachment"] = "Attachment",
        ["obligation"] = "Obligation",
        ["treachery"] = "Treachery",
        ["minion"] = "Minion",
        ["main_scheme"] = "MainScheme",
        ["villain"] = "Villain",
        ["side_scheme"] = "SideScheme",
        ["environment"] = "Environment",
        ["player_side_scheme"] = "PlayerSideScheme",
        ["leader"] = "Leader",
        ["evidence_means"] = "Evidence",
        ["evidence_motive"] = "Evidence",
        ["evidence_opportunity"] = "Evidence",
    };

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

    /// <summary>The engine's name for a card type, or null for one it has none for.</summary>
    /// <param name="typeCode">Upstream's <c>type_code</c>.</param>
    public static string? Kind(string? typeCode) =>
        typeCode is not null && Types.TryGetValue(typeCode, out var kind) ? kind : null;

    /// <summary>
    /// A card's traits, as the engine spells them.
    /// </summary>
    /// <remarks>
    /// Upstream writes them as one sentence-cased string ending in a full stop
    /// — "Avenger. Tiny." — so they split on the stop <i>and a space</i> rather
    /// than on the stop alone. <c>S.H.I.E.L.D.</c> is why: split on the
    /// character and a trait becomes six.
    /// </remarks>
    /// <param name="traits">Upstream's <c>traits</c>.</param>
    public static IReadOnlyList<string> Traits(string? traits)
    {
        if (string.IsNullOrWhiteSpace(traits))
        {
            return [];
        }

        return
        [
            .. Trait()
                .Split(traits.Trim())
                .Select(trait => trait.Trim().TrimEnd('.'))
                .Where(trait => trait.Length > 0)
                .Select(trait => trait.ToUpperInvariant()),
        ];
    }

    /// <summary>
    /// Whose nemesis set a card belongs to — <c>rr:nemesis-encounter-set</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// "Each hero has a nemesis encounter set [...] shuffled into the encounter
    /// deck when that hero is in the game." Nothing on the card says whose it
    /// is; what says so is the set it is printed in, which upstream codes as
    /// the hero's set with <c>_nemesis</c> appended.
    /// </para>
    /// <para>
    /// <b>The alter-ego's name and not the hero's.</b> The cards ask for it
    /// that way — "Give to the <i>T'Challa</i> player" is the same fact on the
    /// obligation in the same set — because <c>rr:you-your.5</c> puts what
    /// happens to a player on their identity, and a nemesis set arrives while
    /// its owner may be in either form.
    /// </para>
    /// </remarks>
    /// <param name="all">Every card, by code.</param>
    /// <returns>Set code to alter-ego name.</returns>
    public static Dictionary<string, string> Nemeses(IReadOnlyDictionary<string, SdbCard> all)
    {
        var identities = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var card in all.Values)
        {
            if (string.Equals(card.Text("type_code"), "alter_ego", StringComparison.Ordinal)
                && card.Text("set_code") is { Length: > 0 } set
                && card.Text("name") is { Length: > 0 } name)
            {
                identities[set] = name;
            }
        }

        var found = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string set in all.Values
            .Select(card => card.Text("set_code"))
            .OfType<string>()
            .Distinct(StringComparer.Ordinal))
        {
            const string Suffix = "_nemesis";
            if (set.EndsWith(Suffix, StringComparison.Ordinal)
                && identities.TryGetValue(set[..^Suffix.Length], out string? owner))
            {
                found[set] = owner;
            }
        }

        return found;
    }

    /// <summary>Everything the engine reads off one card's face.</summary>
    /// <param name="card">The upstream record, with any reprint resolved.</param>
    /// <param name="kind">The engine's name for its type.</param>
    /// <param name="nemeses">Which set belongs to which identity.</param>
    /// <returns>The attributes, in the order they are written.</returns>
    public static SortedDictionary<string, string> Attributes(
        SdbCard card, string kind, IReadOnlyDictionary<string, string> nemeses)
    {
        var printed = new SortedDictionary<string, string>(StringComparer.Ordinal);
        string text = Plain(card.Text("text"));

        StatBox(card, kind, printed);
        Threat(card, printed);
        Cost(card, kind, printed);
        Extract.Keywords.Read(text, printed);
        Scheme(card, printed);

        if (card.Text("stage") is { Length: > 0 } stage)
        {
            // Upstream writes a villain's stage as a roman numeral and a main
            // scheme's as "1A"/"1B" — the same number with the face appended,
            // and the face is which side of the card is up rather than which
            // stage it is. Both become the number.
            printed["Stage"] = Numbered(stage);
        }

        if (Classed.Contains(kind, StringComparer.Ordinal)
            && card.Text("faction_code") is { Length: > 0 } faction)
        {
            // Deadpool's class is printed `'Pool`, apostrophe and all, and
            // upstream codes it `pool`. Capitalising the first letter is right
            // for the other eight.
            printed["Class"] = string.Equals(faction, "pool", StringComparison.Ordinal)
                ? "'Pool"
                : char.ToUpperInvariant(faction[0]) + faction[1..];
        }

        if (card.Text("set_code") is { } set && nemeses.TryGetValue(set, out string? owner))
        {
            printed["Nemesis"] = owner;
        }

        if (card.Text("type_code") is { } code && code.StartsWith("evidence_", StringComparison.Ordinal))
        {
            // `rr:evidence` gives one card type three printed subtypes, and the
            // type alone cannot tell them apart.
            string subtype = code["evidence_".Length..];
            printed["Subtype"] = char.ToUpperInvariant(subtype[0]) + subtype[1..];
        }

        return printed;
    }

    /// <summary>The text box with upstream's markup taken out.</summary>
    /// <param name="text">Upstream's <c>text</c>.</param>
    public static string Plain(string? text)
    {
        if (text is null)
        {
            return string.Empty;
        }

        // `[[Infinite]]` marks a trait reference and `[per_hero]`, `[energy]`
        // and the rest are icon glyphs. The brackets are upstream's notation
        // rather than anything printed, and what they wrap is the printed word.
        string plain = Markup().Replace(text, string.Empty);
        plain = Icon().Replace(plain, match => match.Groups[1].Value);
        return plain.Replace("[[", string.Empty, StringComparison.Ordinal)
                    .Replace("]]", string.Empty, StringComparison.Ordinal);
    }

    private static void StatBox(SdbCard card, string kind, SortedDictionary<string, string> into)
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

    // A character's stat box. Upstream omits the key on a card with no such
    // box and writes null on one whose box holds something that is not a
    // number, so the key's presence is what decides whether the attribute
    // exists at all -- see `SdbCard.Has`.
    private static void Value(
        SdbCard card, string field, string key, SortedDictionary<string, string> into,
        bool perPlayer = false, int stars = 0)
    {
        // Upstream writes `null` where a character has no such box at all --
        // the Hulk prints no THW, and `rr:thwart.1` gives him nothing to do
        // with one. A zero would be a printed zero, which is a different card.
        if (card.Number(field) is not { } printed)
        {
            return;
        }

        // Upstream writes -1 where a box is printed with a dash. Zero is what
        // the game reads a dash as, and `rr:attack-player-ability-type` has
        // nothing to say about a negative one.
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

    private static void Threat(SdbCard card, SortedDictionary<string, string> into)
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

    private static void Cost(SdbCard card, string kind, SortedDictionary<string, string> into)
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

    private static void Scheme(SdbCard card, SortedDictionary<string, string> into)
    {
        // The four icons a scheme can print. Upstream carries them as counts
        // beside the stat box rather than in the text, which is where the
        // engine's `StateFields` expects to find them.
        Icons(card, "scheme_acceleration", "Acceleration", into);
        Icons(card, "scheme_hazard", "Hazard", into);
        Icons(card, "scheme_crisis", "Crisis", into);
        Icons(card, "scheme_amplify", "Amplify", into);

        if (card.Flag("permanent"))
        {
            into["Permanent"] = "1";
        }
    }

    private static void Icons(
        SdbCard card, string field, string key, SortedDictionary<string, string> into)
    {
        if (card.Number(field) is { } count && count > 0)
        {
            into[key] = count.ToString(CultureInfo.InvariantCulture);
        }
    }

    // Upstream's stage is "I", "1A" or "A2" depending on the card type. The
    // number is what the engine wants; the face letter is which side is up.
    private static string Numbered(string stage)
    {
        string roman = stage switch
        {
            "I" => "1",
            "II" => "2",
            "III" => "3",
            "IV" => "4",
            "V" => "5",
            _ => stage,
        };

        // "1A" and "1B" are the two sides of one stage, and the letter is
        // which side is up rather than which stage it is. A bare letter is
        // something else -- Wrecking Crew prints four villains with an A and a
        // B stage each -- and stays as printed.
        var match = Face().Match(roman);
        return match.Success ? match.Groups[1].Value : roman;
    }

    [GeneratedRegex(@"\.(?:\s+|$)")]
    private static partial Regex Trait();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex Markup();

    [GeneratedRegex(@"\[([a-z_]+)\]")]
    private static partial Regex Icon();

    [GeneratedRegex(@"^(\d+)[AB]$")]
    private static partial Regex Face();
}
