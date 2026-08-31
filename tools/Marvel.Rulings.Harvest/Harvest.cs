using System.Text.RegularExpressions;

namespace Marvel.Rulings.Harvest;

public static partial class Harvest
{
    public static IReadOnlyList<Ruling> Read(string html, Page page)
    {
        var result = new List<Ruling>();
        string section = "Unsectioned";
        var pending = new List<Pending>();
        Pending? current = null;

        foreach (Match block in Block().Matches(EntryContent(html)))
        {
            string tag = block.Groups[1].Value.ToLowerInvariant();
            string body = block.Groups[2].Value;
            string text = Html.Text(body);
            if (tag == "h2")
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    section = text;
                }

                continue;
            }

            if (tag == "blockquote")
            {
                current = new Pending(section, body);
                pending.Add(current);
                continue;
            }

            if (current is null || string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (tag == "p" && IsAttribution(text))
            {
                result.AddRange(pending.SelectMany(question => Finish(question, text, page)));
                pending.Clear();
                current = null;
                continue;
            }

            if (tag is "ol" or "ul")
            {
                var items = Html.ListItems(body);
                current.Answers.AddRange(items);
                current.AnswerHtml.Add(body);
            }
            else if (tag == "p")
            {
                current.Answers.Add(text);
                current.AnswerHtml.Add(body);
            }
        }

        return result;
    }

    private static IEnumerable<Ruling> Finish(Pending pending, string attribution, Page page)
    {
        var questions = Html.Paragraphs(pending.QuestionHtml).Where(value => value.Length > 0).ToList();
        var answers = pending.Answers.Where(value => value.Length > 0).ToList();
        if (questions.Count == 0 || answers.Count == 0)
        {
            yield break;
        }

        string question = string.Join(" ", questions);
        string answer = string.Join(" ", answers);

        string source = Source(attribution);
        string? observed = page.Shape == PageShape.Chronological
            ? Html.Observed(attribution, pending.Section)
            : null;

        var cards = CardLink().Matches(pending.QuestionHtml + " " + string.Join(" ", pending.AnswerHtml))
            .Select(match => match.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToList();
        yield return Ruling.Create(
            question,
            answer,
            source,
            page,
            pending.Section,
            observed,
            cards);
    }

    public static string Source(string attribution)
    {
        string source = attribution.Trim().TrimStart('-', '–', '—').Trim();
        int dated = AttributionDate().Match(source) is { Success: true } match ? match.Index : -1;
        if (dated >= 0)
        {
            source = source[..dated].Trim().TrimEnd('-', '–', '—').Trim();
        }

        if (source.Contains("Caleb", StringComparison.OrdinalIgnoreCase))
        {
            return "Caleb Grace (Marvel Champions LCG designer)";
        }

        if (source.Contains("Alex", StringComparison.OrdinalIgnoreCase))
        {
            return "Alex Werner (FFG Game Rules Specialist)";
        }

        // The compendium transcribes one Star-Lord attribution as "Ale".
        // Hall of Heroes chose that spelling; the surrounding rulings and the
        // answerer's established byline identify it as the same attribution.
        if (string.Equals(source, "Ale", StringComparison.OrdinalIgnoreCase))
        {
            return "Alex Werner (FFG Game Rules Specialist)";
        }

        if (source.Contains("Boggs", StringComparison.OrdinalIgnoreCase))
        {
            return "Michael Boggs (Marvel Champions Card Game developer)";
        }

        if (source.Contains("Tony", StringComparison.OrdinalIgnoreCase))
        {
            return "Tony Fanchi (FFG Game Rules Specialist)";
        }

        return source;
    }

    private static bool IsAttribution(string text) =>
        text.StartsWith('-')
        || text.StartsWith('–')
        || text.StartsWith('—');

    private static string EntryContent(string html)
    {
        Match start = Entry().Match(html);
        if (!start.Success)
        {
            throw new InvalidDataException("page has no entry-content element");
        }

        int end = html.IndexOf("<footer class=\"entry-footer\"", start.Index, StringComparison.OrdinalIgnoreCase);
        return end < 0 ? html[start.Index..] : html[start.Index..end];
    }

    private sealed record Pending(string Section, string QuestionHtml)
    {
        public List<string> Answers { get; } = [];

        public List<string> AnswerHtml { get; } = [];
    }

    [GeneratedRegex("<div\\s+class=\"entry-content\"[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Entry();

    [GeneratedRegex("<(h2|blockquote|p|ol|ul)\\b[^>]*>(.*?)</\\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant)]
    private static partial Regex Block();

    [GeneratedRegex("https?://(?:www\\.)?marvelcdb\\.com/card/([0-9a-z]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CardLink();

    [GeneratedRegex("(?:[-–—]\\s*)?(?:January|February|March|April|May|June|July|August|September|October|November|December)\\s+\\d{1,2},\\s+20\\d{2}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AttributionDate();
}
