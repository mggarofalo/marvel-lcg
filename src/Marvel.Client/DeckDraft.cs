using System.Collections.Immutable;

namespace Marvel.Client;

/// <summary>An editable library value, independent of any running game.</summary>
public sealed record DeckDraft
{
    /// <summary>Captures exact printed codes, including unsupported imported codes.</summary>
    public DeckDraft(string name, string heroCode, IReadOnlyList<string> cards)
    {
        Name = name;
        HeroCode = heroCode;
        Cards = cards.ToImmutableArray();
    }

    /// <summary>The player's library label.</summary>
    public string Name { get; }
    /// <summary>The imported or selected printed identity face, never inferred from a title.</summary>
    public string HeroCode { get; }
    /// <summary>Exact card faces, preserving each copy.</summary>
    public ImmutableArray<string> Cards { get; }
}
