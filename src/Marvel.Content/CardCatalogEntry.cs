using Marvel.Rules.State;

namespace Marvel.Content;

/// <summary>One decoded face and its structured printed facts.</summary>
internal sealed record CardCatalogEntry(
    CardKind Kind,
    string Set,
    IReadOnlyList<string> LinkedTo,
    IReadOnlyList<string> Traits,
    IReadOnlyList<string> PrintedTraits,
    IReadOnlyDictionary<string, string> Attributes,
    string Title,
    string Subtitle,
    string Text,
    string FormattedText,
    IReadOnlyList<string> Keywords,
    IReadOnlyList<string> CounterTypes,
    IReadOnlyDictionary<string, long> CounterMaximums)
{
    internal IReadOnlyDictionary<string, PrintedStatValue> PrintedStats { get; init; } =
        new Dictionary<string, PrintedStatValue>();
}
