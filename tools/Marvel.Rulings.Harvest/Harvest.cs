using System.Text.RegularExpressions;

namespace Marvel.Rulings.Harvest;

public static partial class Harvest
{
    public static IReadOnlyList<Ruling> Read(string html, Page page)
        => new PageReader(page).Read(html);

    private sealed class PageReader(Page page)
    {
        private readonly List<Ruling> result = [];
        private readonly List<Pending> pending = [];
        private string section = "Unsectioned";
        private Pending? current;
        private Pending? bareQuestion;

        internal List<Ruling> Read(string html)
        {
            foreach (Block block in Html.Blocks(EntryContent(html))) Process(block);
            return result;
        }

        private void Process(Block block)
        {
            string text = Html.Text(block.Body);
            if (block.Tag == "h2") ReadHeading(text);
            else if (block.Tag == "p" && IsAttribution(text)) Finish(text);
            else if (string.IsNullOrWhiteSpace(text)) return;
            else if (block.Tag == "blockquote") ReadBlockquote(block.Body);
            else ReadParagraph(block.Tag, block.Body, text);
        }

        private void ReadHeading(string text)
        {
            if (!string.IsNullOrWhiteSpace(text)) section = text;
            bareQuestion = null;
        }

        private void Finish(string attribution)
        {
            result.AddRange(pending.SelectMany(question =>
                Harvest.Finish(question, attribution, page)));
            pending.Clear();
            current = null;
            bareQuestion = null;
        }

        private void ReadBlockquote(string body)
        {
            if (bareQuestion is not null)
            {
                current = bareQuestion;
                pending.Add(current);
                AddAnswer(current, body);
                bareQuestion = null;
                return;
            }
            if (current is not null && current.Answers.Count == 0
                && Html.ListItems(body).Count > 0)
            {
                AddAnswer(current, body);
                return;
            }
            current = new Pending(section, body);
            pending.Add(current);
        }

        private void ReadParagraph(string tag, string body, string text)
        {
            if (current is not null)
            {
                AddAnswer(current, body);
                return;
            }
            if (tag != "p") return;
            if (bareQuestion is not null && !LooksLikeQuestion(text))
            {
                current = bareQuestion;
                pending.Add(current);
                current.Answers.Add(text);
                current.AnswerHtml.Add(body);
                bareQuestion = null;
                return;
            }
            if (LooksLikeQuestion(text))
                bareQuestion = new Pending(section, "<p>" + body + "</p>");
        }
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
        string? observed = Observed(page, pending.Section, attribution);

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

    private static void AddAnswer(Pending pending, string html)
    {
        var items = Html.ListItems(html);
        if (items.Count > 0)
        {
            pending.Answers.AddRange(items);
        }
        else
        {
            var paragraphs = Html.Paragraphs(html);
            pending.Answers.AddRange(paragraphs.Count > 0 ? paragraphs : [Html.Text(html)]);
        }

        pending.AnswerHtml.Add(html);
    }

    private static bool LooksLikeQuestion(string text) => text.Contains('?');

    private static string? Observed(Page page, string section, string attribution)
    {
        if (page.Shape == PageShape.Compendium)
        {
            return null;
        }

        string? attributed = Html.AttributionMonth(attribution);
        string? grouped = Html.SectionMonth(section);
        if (attributed is null || grouped is null || string.Equals(attributed, grouped, StringComparison.Ordinal))
        {
            return attributed ?? grouped;
        }

        if (string.CompareOrdinal(attributed, grouped) > 0)
        {
            // The chronological feeds do not always add a new heading when a
            // later month is appended. A forward-moving byline is explicit;
            // keep it while still rejecting a date that moves backwards.
            return attributed;
        }

        string correction = string.Join('|', page.Name, section, Html.Text(attribution));
        if (string.Equals(
            correction,
            "post-rrg-1-7|February, 2026|-Alex – February 20, 2025",
            StringComparison.Ordinal))
        {
            // Hall of Heroes groups this ruling under February 2026 between
            // February 2026 rulings, but its byline says February 20, 2025.
            // The section is the audited authority for this transcription typo.
            return "2026-02";
        }

        throw new InvalidDataException(
            $"attribution month {attributed} disagrees with section {grouped} on {page.Name}");
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

    [GeneratedRegex("https?://(?:www\\.)?marvelcdb\\.com/card/([0-9a-z]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CardLink();

    [GeneratedRegex("(?:[-–—]\\s*)?(?:January|February|March|April|May|June|July|August|September|October|November|December)\\s+\\d{1,2},\\s+20\\d{2}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex AttributionDate();
}
