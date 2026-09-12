using UglyToad.PdfPig;

namespace Marvel.Rules.Packs.Harvest;

internal sealed class PackDocumentReader(string path)
{
    private readonly List<Section> sections = [];
    private readonly List<string> buffer = [];
    private readonly List<string> headingBuffer = [];
    private Section? section;
    private NamedRule? rule;
    private string title = string.Empty;

    internal PackDocument Read()
    {
        string filename = Path.GetFileName(path);
        var (code, kind) = Harvest.Classify(filename)
            ?? throw new InvalidDataException(
                $"{filename} is outside the pack-rules corpus");
        using var pdf = PdfDocument.Open(path);
        for (int page = 1; page <= pdf.NumberOfPages; page++) ReadPage(pdf, page);
        CloseHeading();
        Flush();
        return new PackDocument(
            filename, code, kind, title,
            sections.Where(Harvest.IsRulesSection).ToList());
    }

    private void ReadPage(PdfDocument pdf, int page)
    {
        foreach (PackLine line in Pages.Read(pdf.GetPage(page)))
            ReadLine(line, page);
    }

    private void ReadLine(PackLine line, int page)
    {
        string text = line.Text.Trim();
        if (text.Length == 0) return;
        if (line.Heading)
        {
            StartSection(text, page);
            return;
        }
        if (section is null || IsItalicAside(line)) return;
        if (IsRuleHeading(line))
        {
            if (headingBuffer.Count == 0) Flush();
            headingBuffer.Add(text);
            return;
        }
        CloseHeading();
        buffer.Add(text);
    }

    private void StartSection(string text, int page)
    {
        string heading = Harvest.Clean(text);
        if (heading.Length == 0 || Harvest.IsNumericFurniture(heading)) return;
        Flush();
        headingBuffer.Clear();
        rule = null;
        section = new Section(heading, page);
        sections.Add(section);
        if (title.Length == 0) title = heading;
    }

    private static bool IsItalicAside(PackLine line) =>
        line.Spans.Count > 0
        && line.Spans.All(span => span.Italic || string.IsNullOrWhiteSpace(span.Text));

    private static bool IsRuleHeading(PackLine line) =>
        line.Spans.Count > 0
        && line.Spans.All(span => span.Bold || string.IsNullOrWhiteSpace(span.Text));

    private void Flush()
    {
        if (section is not null && buffer.Count > 0)
        {
            string paragraph = Harvest.Clean(string.Join(' ', buffer));
            if (paragraph.Length > 0)
                (rule?.Paragraphs ?? section.Paragraphs).Add(paragraph);
        }
        buffer.Clear();
    }

    private void CloseHeading()
    {
        if (headingBuffer.Count == 0) return;
        string heading = Harvest.Clean(string.Join(' ', headingBuffer));
        headingBuffer.Clear();
        if (section is null || heading.Length == 0) return;
        rule = new NamedRule(heading);
        section.Rules.Add(rule);
    }
}
