using System.Globalization;

namespace Marvel.Rules.State;

/// <summary>Owns the card dataset's star notation for printed values.</summary>
public static class PrintedAttributeNotation
{
    /// <summary>Ally ATK and THW stars describe consequential damage, not player scaling.</summary>
    public static bool IsConsequential(CardKind kind, string attribute) =>
        kind == CardKind.Ally && attribute is "ATK" or "THW";

    /// <summary>Returns the separately printed consequential marks for this attribute.</summary>
    public static int ConsequentialDamage(CardKind kind, string attribute, string printed) =>
        IsConsequential(kind, attribute) ? printed.Count(character => character == '*') : 0;

    /// <summary>Identifies a per-player mark without evaluating the value.</summary>
    public static bool IsPerPlayer(CardKind kind, string attribute, string printed) =>
        !IsConsequential(kind, attribute) && printed.Contains('*');
    /// <summary>Evaluates the dataset notation after consequential marks have been separated.</summary>
    public static long Evaluate(string printed, int players, long fallback = 0)
    {
        ArgumentNullException.ThrowIfNull(printed);

        int stars = printed.Count(character => character == '*');
        string digits = printed.TrimEnd('*');
        if (!long.TryParse(digits, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture,
                           out long value))
        {
            return fallback;
        }

        for (int multiplied = 0; multiplied < stars; multiplied++)
        {
            value *= players;
        }

        return value;
    }

}
