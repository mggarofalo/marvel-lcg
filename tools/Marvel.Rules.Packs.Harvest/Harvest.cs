using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace Marvel.Rules.Packs.Harvest;

public static partial class Harvest
{
    private static readonly HashSet<string> NotRules = new(StringComparer.Ordinal)
    {
        "CREDITS", "PLAYTESTERS", "MARVEL", "GAME", "HERO PACK", "SCENARIO PACK",
        "EXPANSION SYMBOL", "SET SYMBOL", "S.H.I.E.L.D. BRIEFING", "THE STORY SO FAR",
        "STRATEGY TIPS", "STRATEGY TIP", "COMPONENTS", "COMPONENT LIST",
    };

    public static (string Code, string Kind)? Classify(string filename)
    {
        string low = filename.ToLowerInvariant();
        if (Excluded(low)) return null;

        Match match = PackCode().Match(low);
        if (!match.Success)
        {
            throw new InvalidDataException(
                $"{filename} does not begin with a supported mc/mvc pack code");
        }

        string code = match.Groups[1].Value;
        return (code, Kind(low));
    }

    private static bool Excluded(string filename) =>
        Contains(filename, "campaign_log")
        || Contains(filename, "campaignlog")
        || Contains(filename, "campaign-log")
        || Contains(filename, "rulesreference");

    private static string Kind(string filename)
    {
        if (Contains(filename, "learn_to_play") || Contains(filename, "learntoplay"))
            return "learn-to-play";
        if (Contains(filename, "rules_insert") || Contains(filename, "rulesinsert")
            || Contains(filename, "rules_website")) return "insert";
        if (Contains(filename, "rulesheet")) return "rulesheet";
        if (Contains(filename, "rulebook") || Contains(filename, "_rules_")
            || Contains(filename, "_rules-")) return "rulebook";
        return "other";
    }

    private static bool Contains(string value, string fragment) =>
        value.Contains(fragment, StringComparison.Ordinal);

    public static IReadOnlyList<string> Sources(string library) => Directory.Exists(library)
        ? Directory.EnumerateFiles(library)
            .Where(path => string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase))
            .Where(path => Classify(Path.GetFileName(path)) is not null)
            .Order(StringComparer.Ordinal).ToList()
        : [];

    public static PackDocument Read(string path)
        => new PackDocumentReader(path).Read();

    public static string Slug(string text)
    {
        string clean = Undouble(text).ToLowerInvariant();
        clean = IconToken().Replace(clean, " ");
        clean = NonIdentifier().Replace(clean, "-");
        return clean.Trim('-');
    }

    internal static string Undouble(string text) => string.Join(' ', text.Split(' ').Select(word =>
        word.Length >= 4
        && word.Length % 2 == 0
        && Enumerable.Range(0, word.Length / 2).All(index => word[index * 2] == word[(index * 2) + 1])
            ? string.Concat(Enumerable.Range(0, word.Length / 2).Select(index => word[index * 2]))
            : word));

    internal static string Clean(string text) =>
        Undouble(Whitespace().Replace(text, " ").Trim());

    internal static bool IsNumericFurniture(string text) =>
        NumericFurniture().IsMatch(text);

    internal static bool IsRulesSection(Section section) =>
        (section.Paragraphs.Count > 0 || section.Rules.Count > 0)
        && !NotRules.Contains(section.Heading.ToUpperInvariant());

    [GeneratedRegex("^(mc\\d+|mvc\\d+)(?:[_-]|$)", RegexOptions.CultureInvariant)]
    private static partial Regex PackCode();

    [GeneratedRegex("^[\\d\\W]+$", RegexOptions.CultureInvariant)]
    private static partial Regex NumericFurniture();

    [GeneratedRegex("\\[[a-z-]+\\]", RegexOptions.CultureInvariant)]
    private static partial Regex IconToken();

    [GeneratedRegex("[^a-z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex NonIdentifier();

    [GeneratedRegex("\\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace();
}
