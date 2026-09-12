namespace Marvel.Rules.Packs.Harvest;

public sealed class NamedRule(string heading)
{
    public string Heading { get; } = heading;

    public List<string> Paragraphs { get; } = [];

    public string Text => string.Join("\n\n", Paragraphs);
}
