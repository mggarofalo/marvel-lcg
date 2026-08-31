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
        if (low.Contains("campaign_log", StringComparison.Ordinal)
            || low.Contains("campaignlog", StringComparison.Ordinal)
            || low.Contains("campaign-log", StringComparison.Ordinal)
            || low.Contains("rulesreference", StringComparison.Ordinal))
        {
            return null;
        }

        Match match = PackCode().Match(low);
        string code = match.Success ? match.Groups[1].Value : Path.GetFileNameWithoutExtension(low)[..Math.Min(12, Path.GetFileNameWithoutExtension(low).Length)];
        string kind = low.Contains("learn_to_play", StringComparison.Ordinal)
            || low.Contains("learntoplay", StringComparison.Ordinal)
                ? "learn-to-play"
            : low.Contains("rules_insert", StringComparison.Ordinal)
                || low.Contains("rulesinsert", StringComparison.Ordinal)
                || low.Contains("rules_website", StringComparison.Ordinal)
                ? "insert"
            : low.Contains("rulesheet", StringComparison.Ordinal)
                ? "rulesheet"
            : low.Contains("rulebook", StringComparison.Ordinal)
                || low.Contains("_rules_", StringComparison.Ordinal)
                || low.Contains("_rules-", StringComparison.Ordinal)
                ? "rulebook"
            : "other";
        return (code, kind);
    }

    public static IReadOnlyList<string> Sources(string library) => Directory.Exists(library)
        ? Directory.EnumerateFiles(library)
            .Where(path => string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase))
            .Where(path => Classify(Path.GetFileName(path)) is not null)
            .Order(StringComparer.Ordinal).ToList()
        : [];

    public static PackDocument Read(string path)
    {
        string filename = Path.GetFileName(path);
        var (code, kind) = Classify(filename)
            ?? throw new InvalidDataException($"{filename} is outside the pack-rules corpus");
        var sections = new List<Section>();
        Section? section = null;
        NamedRule? rule = null;
        var buffer = new List<string>();
        var headingBuffer = new List<string>();
        string title = string.Empty;

        void Flush()
        {
            if (section is not null && buffer.Count > 0)
            {
                string paragraph = Clean(string.Join(' ', buffer));
                if (paragraph.Length > 0)
                {
                    (rule?.Paragraphs ?? section.Paragraphs).Add(paragraph);
                }
            }

            buffer.Clear();
        }

        void CloseHeading()
        {
            if (headingBuffer.Count == 0)
            {
                return;
            }

            string heading = Clean(string.Join(' ', headingBuffer));
            headingBuffer.Clear();
            if (section is not null && heading.Length > 0)
            {
                rule = new NamedRule(heading);
                section.Rules.Add(rule);
            }
        }

        using var pdf = PdfDocument.Open(path);
        for (int page = 1; page <= pdf.NumberOfPages; page++)
        {
            foreach (PackLine line in Pages.Read(pdf.GetPage(page)))
            {
                string text = line.Text.Trim();
                if (text.Length == 0)
                {
                    continue;
                }

                if (line.Heading)
                {
                    string heading = Clean(text);
                    if (heading.Length == 0 || NumericFurniture().IsMatch(heading))
                    {
                        continue;
                    }

                    Flush();
                    headingBuffer.Clear();
                    rule = null;
                    section = new Section(heading, page);
                    sections.Add(section);
                    if (title.Length == 0)
                    {
                        title = heading;
                    }

                    continue;
                }

                if (section is null)
                {
                    continue;
                }

                if (line.Spans.Count > 0
                    && line.Spans.All(span => span.Italic || string.IsNullOrWhiteSpace(span.Text)))
                {
                    continue;
                }

                if (line.Spans.Count > 0
                    && line.Spans.All(span => span.Bold || string.IsNullOrWhiteSpace(span.Text)))
                {
                    if (headingBuffer.Count == 0)
                    {
                        Flush();
                    }

                    headingBuffer.Add(text);
                    continue;
                }

                CloseHeading();
                buffer.Add(text);
            }
        }

        CloseHeading();
        Flush();
        sections = sections.Where(candidate =>
            (candidate.Paragraphs.Count > 0 || candidate.Rules.Count > 0)
            && !NotRules.Contains(candidate.Heading.ToUpperInvariant())).ToList();
        return new PackDocument(filename, code, kind, title, sections);
    }

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

    private static string Clean(string text) =>
        Undouble(Whitespace().Replace(text, " ").Trim());

    [GeneratedRegex("^(mc\\d+|mvc\\d+)", RegexOptions.CultureInvariant)]
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
