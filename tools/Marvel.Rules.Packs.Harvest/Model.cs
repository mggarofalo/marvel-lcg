namespace Marvel.Rules.Packs.Harvest;

public sealed record Span(string Text, bool Bold, bool Italic);

public sealed record PackLine(IReadOnlyList<Span> Spans, bool Heading)
{
    public string Text => string.Concat(Spans.Select(span => span.Text));
}

public sealed class NamedRule(string heading)
{
    public string Heading { get; } = heading;

    public List<string> Paragraphs { get; } = [];

    public string Text => string.Join("\n\n", Paragraphs);
}

public sealed class Section(string heading, int page)
{
    public string Heading { get; } = heading;

    public int Page { get; } = page;

    public List<string> Paragraphs { get; } = [];

    public List<NamedRule> Rules { get; } = [];

    public string Text => string.Join(
        "\n\n",
        Paragraphs.Concat(Rules.SelectMany(rule => new[] { rule.Heading }.Concat(rule.Paragraphs))));
}

public sealed record PackDocument(
    string Path,
    string Code,
    string Kind,
    string Title,
    IReadOnlyList<Section> Sections);
