using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace Marvel.Rulings.Harvest;

internal static partial class Html
{
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

    public static string? Observed(string attribution, string? section)
    {
        Match date = Date().Match(attribution);
        if (date.Success && DateTime.TryParse(
            date.Value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out DateTime parsed))
        {
            return parsed.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        }

        Match month = Month().Match(section ?? "");
        if (month.Success && DateTime.TryParse(
            month.Value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces,
            out parsed))
        {
            return parsed.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        }

        return null;
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
}
