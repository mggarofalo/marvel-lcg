namespace Marvel.Rules.Packs.Harvest;

public sealed record PackLine(IReadOnlyList<Span> Spans, bool Heading)
{
    public string Text => string.Concat(Spans.Select(span => span.Text));
}
