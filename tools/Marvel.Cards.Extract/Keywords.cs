using System.Globalization;
using System.Text.RegularExpressions;

namespace Marvel.Cards.Extract;

/// <summary>
/// The keywords a card prints, read out of its text box — <c>rr:keywords</c>.
/// </summary>
/// <remarks>
/// <para>
/// "A keyword is a card ability that is summarised by a single word or phrase.
/// [...] Keywords are listed at the top of a card's text box." So they are
/// printed text, not a stat, and upstream carries them nowhere else.
/// </para>
/// <para>
/// <b>Anchored to the keyword line, not searched for in prose.</b>
/// <c>rr:keywords.1</c> puts them at the top of the box, each its own sentence.
/// A card that merely says the word — "if a character is <i>stunned</i>", "the
/// villain is <i>guarded</i>" — is not printing the keyword, and a bare
/// substring search would give it one. The line ends at the first sentence
/// that is not a keyword.
/// </para>
/// </remarks>
internal static partial class Keywords
{
    /// <summary>
    /// The keywords that stand alone, and the attribute each fills.
    /// </summary>
    /// <remarks>
    /// Every one of these is a bare word on the keyword line, so the attribute
    /// is a flag: it is either printed or it is not.
    /// </remarks>
    private static readonly (string Word, string Key)[] Bare =
    [
        ("Toughness", "Toughness"),
        ("Guard", "Guard"),
        ("Surge", "Surge"),
        ("Villainous", "Villainous"),
        ("Quickstrike", "Quickstrike"),
        ("Stalwart", "Stalwart"),
        ("Peril", "Peril"),
        ("Steady", "Steady"),
        ("Vulnerable", "Vulnerable"),
        ("Assault", "Assault"),
        ("Temporary", "Temporary"),
        ("Permanent", "Permanent"),
        ("Setup", "Setup"),
        ("Patrol", "Patrol"),
        ("Incite", "Incite"),
        ("Restricted", "Restricted"),
        ("Alliance", "Alliance"),
    ];

    /// <summary>
    /// The keywords that carry a number, and the attribute each fills.
    /// </summary>
    private static readonly (string Word, string Key)[] Counted =
    [
        ("Retaliate", "Retaliate"),
        ("Hinder", "Hinder"),
        ("Victory", "Victory"),
        ("Incite", "Incite"),
        ("Patrol", "Patrol"),
    ];

    /// <summary>Fills every keyword attribute a card's text prints.</summary>
    /// <param name="text">The text box, markup removed.</param>
    /// <param name="into">Where to record them.</param>
    public static void Read(string text, SortedDictionary<string, string> into)
    {
        string line = Line(text);

        foreach (var (word, key) in Counted)
        {
            if (WithNumber(word).Match(line) is { Success: true } match)
            {
                // `rr:hinder-x` and `rr:victory-x` both print a per-player
                // icon on some cards, which upstream writes into the text as
                // `[per_hero]` and `Printed.Plain` leaves as `per_hero`.
                into[key] = match.Groups[1].Value
                    + (match.Groups[2].Success ? "*" : string.Empty);
            }
        }

        foreach (var (word, key) in Bare)
        {
            if (!into.ContainsKey(key) && Standalone(word).IsMatch(line))
            {
                into[key] = "1";
            }
        }

        // Read from the whole box rather than from the keyword line. Every one
        // of these prints its argument in brackets straight after the keyword,
        // and no card's prose contains `Uses (`, `Teamwork (` or `Team-Up (` --
        // so there is nothing for the line to protect them from, and several
        // cards print them after a `Max N per deck` that the line stops at.
        Parenthesised(text, into);
        Limits(text, into);
    }

    // `Uses (3 web counters)`, `Teamwork ([[ACOLYTE]])`, `Requirement
    // ([mental])` -- keywords whose argument is printed in brackets after
    // them. Each is its own shape, so each is its own pattern rather than one
    // that would match the others' arguments as well.
    private static void Parenthesised(string line, SortedDictionary<string, string> into)
    {
        if (Growing().Match(line) is { Success: true } growing)
        {
            // `rr:uses-x-type` with a per-player half: "Uses (1 fury counter,
            // plus 1[per_hero] additional fury counters)". Two numbers, and the
            // second is multiplied by the table size, so they cannot be added
            // up here.
            into["Uses"] =
                $"{growing.Groups[1].Value}+{growing.Groups[2].Value}"
                + $"{(growing.Groups[3].Success ? "*" : "")},{growing.Groups[4].Value}";
        }
        else if (Uses().Match(line) is { Success: true } uses)
        {
            // `rr:uses-x-type` -- "uses (X [type] counters)". The count and the
            // word are one attribute because a card's own ability spends them
            // by name, and the count alone would not say which pile.
            string type = uses.Groups[3].Success ? uses.Groups[3].Value : string.Empty;
            into["Uses"] = $"{uses.Groups[1].Value}{(uses.Groups[2].Success ? "*" : "")},{type}";
        }

        if (Teamwork().Match(line) is { Success: true } teamwork)
        {
            // `rr:teamwork` names a trait, and the engine spells traits the way
            // `Printed.Traits` does.
            into["Teamwork"] = teamwork.Groups[1].Value.Trim().ToUpperInvariant();
        }

        if (TeamUp().Match(line) is { Success: true } teamUp)
        {
            // `rr:team-up.2` matches "either its title or subtitle", and the
            // card names two identities. Semicolons because a title can hold a
            // comma and nothing else in this dataset uses one.
            into["TeamUp"] = string.Join(
                ';',
                teamUp.Groups[1].Value.Split(" and ", StringSplitOptions.TrimEntries));
        }

        if (Requirement().Match(line) is { Success: true } requirement)
        {
            // `rr:requirement-resources` prints resource icons; `Printed.Plain`
            // has already turned each into its upstream word.
            into["Requirement"] = string.Concat(
                requirement.Groups[1].Value
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(Letter)
                    .Where(letter => letter is not null));
        }

        if (Linked().Match(line) is { Success: true } linked)
        {
            into["Linked"] = linked.Groups[1].Value.Trim();
        }

        if (Form().Match(line) is { Success: true } form)
        {
            // `rr:form-change-form.6` -- "[type] form" is a keyword and a
            // sentence of its own, which is what separates it from prose
            // naming a form ("if you are in Archangel form, place 2 threat").
            into["Form"] = form.Groups[1].Value;
        }
    }

    // Deck limits and the two "give this to somebody" phrasings. These are
    // sentences anywhere in the box rather than keyword-line entries, so they
    // are read from the whole text.
    private static void Limits(string text, SortedDictionary<string, string> into)
    {
        if (PerDeck().Match(text) is { Success: true } deck)
        {
            into["MaxPerDeck"] = deck.Groups[1].Value;
        }

        if (PerUnit().Match(text) is { Success: true } unit)
        {
            // The number and its unit are separate facts. "Max 1 per player"
            // limits a controller's play area; "Max 1 per enemy" limits one
            // attachment host. Collapsing those sentences to the same number
            // makes one rule enforce the other.
            into["MaxPerUnit"] = unit.Groups[1].Value;
            into["MaxPerUnitKind"] = unit.Groups[2].Value.ToLowerInvariant();
        }

        if (UnitCost().Match(text) is { Success: true } cost)
        {
            into["UnitCost"] = cost.Groups[1].Value;
        }

        if (GiveTo().Match(text) is { Success: true } give)
        {
            into["GiveTo"] = give.Groups[1].Value.Trim();
        }

        if (Corresponding().Match(text) is { Success: true } corresponding)
        {
            into["CorrespondingCard"] = corresponding.Groups[1].Value.Trim();
        }
    }

    /// <summary>
    /// The keyword line: the sentences before the first that is not a keyword.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>rr:keywords.1</c> puts every keyword at the top of the text box, so
    /// the line ends at the first sentence carrying none. That boundary is what
    /// separates a printed keyword from a card that merely says the word —
    /// <c>04067</c> Full Auto's "<b>When Revealed (Alter-Ego)</b>: Surge." is
    /// an ability whose effect is a surge, and the card does not have the
    /// keyword.
    /// </para>
    /// <para>
    /// A sentence ends at a full stop <b>or a line break</b>. Several cards
    /// print a keyword with no stop after it — <c>07009</c> Escaped Convict's
    /// text box is the single word "Surge" — and reading to the next stop would
    /// swallow the ability below it.
    /// </para>
    /// </remarks>
    /// <param name="text">The text box, markup removed.</param>
    public static string Line(string text)
    {
        var kept = new List<string>();
        foreach (string sentence in text.Split(['.', '\n'], StringSplitOptions.TrimEntries))
        {
            // Expansion inserts use a star to mark a keyword with scenario-
            // specific rules. `Printed.Plain` spells the icon `star`, which is
            // not part of the keyword and must not make the lower-case guard
            // reject the sentence. The rulebook names the star's meaning; the
            // engine chooses this spelling for upstream's icon token.
            string candidate = sentence.StartsWith("star ", StringComparison.Ordinal)
                ? sentence["star ".Length..].TrimStart()
                : sentence;

            // The closing half of a reminder-text parenthesis, left behind
            // because the stop inside it was a split point.
            if (candidate.Length == 0 || candidate.All(letter => !char.IsLetterOrDigit(letter)))
            {
                continue;
            }

            if (!Looks(candidate))
            {
                break;
            }

            kept.Add(candidate);
        }

        return string.Join(". ", kept) + ".";
    }

    // What this keeps out is the first sentence of an ability. Three things
    // separate the two, and a card in the pool turns on each:
    private static bool Looks(string sentence)
    {
        // `rr:reminder-text` -- "reminder text has no effect on gameplay". It
        // sits between keywords and is not one, so it neither counts nor ends
        // the line.
        if (sentence.StartsWith('(') || sentence.StartsWith('['))
        {
            return true;
        }

        // A bold timing trigger is followed by a colon -- `rr:ability.5` -- and
        // no keyword prints one.
        if (sentence.Contains(':', StringComparison.Ordinal))
        {
            return false;
        }

        if (!char.IsUpper(sentence[0]))
        {
            return false;
        }

        // Three words is the longest keyword *name* printed. What follows it
        // in brackets is its argument and can be as long as two identities'
        // titles -- "Team-Up (Black Panther/T'Challa and Black Panther/Shuri)"
        // -- so the count stops at the bracket.
        int bracket = sentence.IndexOf('(', StringComparison.Ordinal);
        string name = bracket < 0 ? sentence : sentence[..bracket];
        return name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length <= 3;
    }

    private static string? Letter(string icon) => icon switch
    {
        "physical" => "R",
        "energy" => "Y",
        "mental" => "B",
        "wild" => "G",
        _ => null,
    };

    // A keyword stands alone, so what follows it is a stop, a comma, the
    // opening bracket of its reminder text, or the end of the line. What must
    // not follow it is more of a word: `Guarded` is not `Guard`.
    private static Regex Standalone(string word) =>
        new(
            $@"(?:^|\.\s|\s){Regex.Escape(word)}(?=[.,(]|\s|$)",
            RegexOptions.None,
            TimeSpan.FromSeconds(1));

    private static Regex WithNumber(string word) =>
        new($@"\b{Regex.Escape(word)}\s+(\d+)(per_hero)?", RegexOptions.None, TimeSpan.FromSeconds(1));

    // `rr:uses-x-type` -- "Uses (3 web counters)". The type word is optional:
    // `12028` Size Increase prints "Uses (3 counters)" and names the pile in
    // its ability instead, which is not something this can read off the box.
    [GeneratedRegex(
        @"\bUses\s*\((\d+)(per_hero)?\s+(?:(\S+)\s+)?counters?\.?\)",
        RegexOptions.IgnoreCase)]
    private static partial Regex Uses();

    [GeneratedRegex(
        @"\bUses\s*\((\d+)\s+\S+\s+counters?,\s*plus\s+(\d+)(per_hero)?\s+additional\s+(\S+)\s+counters?\.?\)",
        RegexOptions.IgnoreCase)]
    private static partial Regex Growing();

    [GeneratedRegex(@"\bTeamwork\s*\(([^)]+)\)")]
    private static partial Regex Teamwork();

    [GeneratedRegex(@"\bTeam-Up\s*\(([^)]+)\)")]
    private static partial Regex TeamUp();

    [GeneratedRegex(@"\bRequirement\s*\(([^)]+)\)")]
    private static partial Regex Requirement();

    [GeneratedRegex(@"\bLinked\s*\(([^)]+)\)")]
    private static partial Regex Linked();

    [GeneratedRegex(@"(?:^|\.\s)([A-Z][A-Za-z-]*) form(?=\.|$)")]
    private static partial Regex Form();

    [GeneratedRegex(@"\bMax (\d+) per deck\b", RegexOptions.IgnoreCase)]
    private static partial Regex PerDeck();

    // `rr:max-maximum` -- "max" limits how many of a card may be in play or
    // played, and the unit is what the limit is per. "Max 1 per round" is a
    // limit on the *phase* rather than on a game element, which is a different
    // question and one the engine does not ask.
    [GeneratedRegex(
        @"\bMax (\d+) per (player|hero|enemy|character|villain|identity|ally|minion|group|side scheme|scheme)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex PerUnit();

    [GeneratedRegex(@"\bUnit Cost (\d+)")]
    private static partial Regex UnitCost();

    [GeneratedRegex(@"\bGive to the (.+?) player\b", RegexOptions.IgnoreCase)]
    private static partial Regex GiveTo();

    [GeneratedRegex(@"^(.+?)'s Side Scheme\.", RegexOptions.Multiline)]
    private static partial Regex Corresponding();
}
