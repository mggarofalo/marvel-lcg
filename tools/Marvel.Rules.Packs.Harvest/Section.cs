namespace Marvel.Rules.Packs.Harvest;

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
