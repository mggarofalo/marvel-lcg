using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace Marvel.Rulings.Harvest;

internal static partial class Html
{
    public static IReadOnlyList<Block> Blocks(string html)
    {
        var blocks = new List<Block>();
        int offset = 0;
        while (OpenBlock().Match(html, offset) is { Success: true } opening)
        {
            string tag = opening.Groups[1].Value.ToLowerInvariant();
            int depth = 0;
            Match? closing = null;
            foreach (Match candidate in BlockTag().Matches(html, opening.Index))
            {
                if (!string.Equals(candidate.Groups[2].Value, tag, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                depth += candidate.Groups[1].Success ? -1 : 1;
                if (depth == 0)
                {
                    closing = candidate;
                    break;
                }
            }

            if (closing is null)
            {
                throw new InvalidDataException($"unclosed <{tag}> block");
            }

            blocks.Add(new Block(tag, html[opening.End()..closing.Index]));
            offset = closing.Index + closing.Length;
        }

        return blocks;
    }

    public static string Text(string html)
    {
        string withBreaks = BreakTag().Replace(html, " ");
        string withoutTags = Tag().Replace(withBreaks, " ");
        return string.Join(' ', WebUtility.HtmlDecode(withoutTags)
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    public static IReadOnlyList<string> Paragraphs(string html) =>
        Paragraph().Matches(html).Select(match => Text(match.Groups[1].Value)).ToList();

    public static IReadOnlyList<string> ListItems(string html)
    {
        var items = new List<string>();
        var tags = ListItemTag().Matches(html);
        int depth = 0;
        int start = -1;
        foreach (Match tag in tags)
        {
            bool closing = tag.Groups[1].Success;
            if (!closing)
            {
                if (depth == 0)
                {
                    start = tag.Index + tag.Length;
                }

                depth += 1;
            }
            else if (depth > 0)
            {
                depth -= 1;
                if (depth == 0 && start >= 0)
                {
                    items.Add(Text(html[start..tag.Index]));
                    start = -1;
                }
            }
        }

        return items;
    }

    public static string? AttributionMonth(string attribution)
    {
        Match date = Date().Match(attribution);
        return date.Success && DateTime.TryParse(
            date.Value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out DateTime parsed)
            ? parsed.ToString("yyyy-MM", CultureInfo.InvariantCulture)
            : null;
    }

    public static string? SectionMonth(string section)
    {
        Match month = Month().Match(section);
        return month.Success && DateTime.TryParse(
            month.Value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out DateTime parsed)
            ? parsed.ToString("yyyy-MM", CultureInfo.InvariantCulture)
            : null;
    }

    [GeneratedRegex("<br\\s*/?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BreakTag();

    [GeneratedRegex("<[^>]+>", RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex Tag();

    [GeneratedRegex("<p\\b[^>]*>(.*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex Paragraph();

    [GeneratedRegex("<(/)?li\\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ListItemTag();

    [GeneratedRegex("(?:January|February|March|April|May|June|July|August|September|October|November|December)\\s+\\d{1,2},\\s+20\\d{2}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Date();

    [GeneratedRegex("(?:January|February|March|April|May|June|July|August|September|October|November|December)[,:]?\\s+20\\d{2}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Month();

    [GeneratedRegex("<(h2|blockquote|p|ol|ul)\\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OpenBlock();

    [GeneratedRegex("<(/)?(h2|blockquote|p|ol|ul)\\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex BlockTag();
}

internal sealed record Block(string Tag, string Body);

internal static class MatchExtensions
{
    public static int End(this Match match) => match.Index + match.Length;
}
