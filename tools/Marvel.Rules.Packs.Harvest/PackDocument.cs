namespace Marvel.Rules.Packs.Harvest;

public sealed record PackDocument(
    string Path,
    string Code,
    string Kind,
    string Title,
    IReadOnlyList<Section> Sections);
