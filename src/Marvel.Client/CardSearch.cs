using System.Globalization;
using Marvel.Protocol;

namespace Marvel.Client;

/// <summary>MarvelCDB-style public catalog queries; matching never establishes deck legality.</summary>
public static class CardSearch
{
    /// <summary>Search keys offered by the MarvelCDB card search form and query language.</summary>
    public static IReadOnlyDictionary<string, string> Fields { get; } = new Dictionary<string, string>
    {
        [""] = "Name", ["x"] = "Rules text", ["v"] = "Flavor text", ["f"] = "Aspect", ["t"] = "Type",
        ["b"] = "Subtype", ["k"] = "Trait", ["e"] = "Pack", ["u"] = "Unique (1 or 0)", ["o"] = "Cost",
        ["qt"] = "Pack quantity", ["at"] = "Attack", ["th"] = "Thwart", ["df"] = "Defense", ["rc"] = "Recovery",
        ["rp"] = "Physical resources", ["rm"] = "Mental resources", ["re"] = "Energy resources", ["rw"] = "Wild resources",
        ["h"] = "Health", ["m"] = "Encounter set", ["l"] = "Illustrator", ["r"] = "Release date",
    }.AsReadOnly();
    private static readonly HashSet<string> Numeric = ["ep", "o", "qt", "at", "th", "df", "rc", "rp", "rm", "re", "rw", "h", "u"];

    /// <summary>Returns matches in input order and reports invalid syntax without changing a draft.</summary>
    public static IReadOnlyList<DeckCard> Find(IEnumerable<DeckCard> cards, string query)
    {
        var source = cards.ToArray();
        var conditions = CardSearchCondition.Parse(query).Select(condition => PackOrder(condition, source)).ToArray();
        foreach (var condition in conditions) Validate(condition);
        return source.Where(card => conditions.All(condition => Matches(card, condition))).ToArray();
    }

    private static CardSearchCondition PackOrder(CardSearchCondition condition, IReadOnlyList<DeckCard> cards)
    {
        if (condition.Field != "e" || condition.Operator is not ('<' or '>')) return condition;
        var values = condition.Values.Select(value => cards.FirstOrDefault(card => card.SearchFields.GetValueOrDefault("e") == value)
            ?.SearchFields.GetValueOrDefault("ep") ?? throw new FormatException($"Unknown pack '{value}'.")).ToArray();
        return condition with { Field = "ep", Values = values };
    }

    private static void Validate(CardSearchCondition condition)
    {
        if (Numeric.Contains(condition.Field) && condition.Values.Any(value => !decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out _)))
            throw new FormatException($"{Fields[condition.Field]} needs a number.");
        if (condition.Field == "r" && condition.Values.Any(value => value != "now" && !DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)))
            throw new FormatException("Release date needs YYYY-MM-DD or now.");
        if (condition.Operator is '<' or '>' && !Numeric.Contains(condition.Field) && condition.Field != "r")
            throw new FormatException($"Use : or ! for {Fields[condition.Field]}.");
    }

    private static bool Matches(DeckCard card, CardSearchCondition condition)
    {
        string? actual = condition.Field switch
        {
            "" => card.Title + " " + card.Subtitle,
            "x" => card.Text,
            "o" => card.Cost,
            _ => card.SearchFields.GetValueOrDefault(condition.Field),
        };
        if (condition.Field == "k")
            return Negate(condition, condition.Values.Any(value => card.Traits.Contains(value, StringComparer.OrdinalIgnoreCase)));
        if (actual is null) return false;
        if (Numeric.Contains(condition.Field))
            return decimal.TryParse(actual, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal number)
                && Negate(condition, condition.Values.Any(value => Compare(number, decimal.Parse(value, CultureInfo.InvariantCulture), condition.Operator)));
        if (condition.Field == "r") return DateMatches(actual, condition);
        return Negate(condition, condition.Values.Any(value => condition.Field switch
        {
            "" => actual.Contains(value, StringComparison.OrdinalIgnoreCase) || card.Face.Equals(value, StringComparison.OrdinalIgnoreCase),
            "x" or "v" => actual.Contains(value, StringComparison.OrdinalIgnoreCase),
            _ => actual.Equals(value, StringComparison.OrdinalIgnoreCase),
        }));
    }

    private static bool DateMatches(string actual, CardSearchCondition condition)
    {
        if (!DateOnly.TryParseExact(actual, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return condition.Operator == '>';
        return Negate(condition, condition.Values.Any(value =>
        {
            var expected = value == "now" ? DateOnly.FromDateTime(DateTime.UtcNow) : DateOnly.ParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            return condition.Operator switch { '<' => date <= expected, '>' => date > expected, _ => date == expected };
        }));
    }

    private static bool Negate(CardSearchCondition condition, bool match) => condition.Operator == '!' ? !match : match;
    private static bool Compare(decimal actual, decimal expected, char operation) => operation switch
    {
        '<' => actual < expected, '>' => actual > expected, _ => actual == expected,
    };
}
