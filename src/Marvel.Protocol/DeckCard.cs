namespace Marvel.Protocol;

/// <summary>Public printed catalog facts, never a live or concealed game-card identity.</summary>
public sealed record DeckCard(string Face, string Title, string Kind, string Aspect,
    IReadOnlyList<string> Traits, string Text, string? Cost)
{
    /// <summary>Public MarvelCDB search fields from the offline metadata snapshot.</summary>
    public IReadOnlyDictionary<string, string> SearchFields { get; init; } = new Dictionary<string, string>();

    /// <summary>Whether this face belongs to the host's admitted Core player pool.</summary>
    public bool RuntimeSupported { get; init; }

    /// <summary>The printed subtitle, when any.</summary>
    public string Subtitle { get; init; } = "";

    /// <summary>The complete public printed face for card preview; never a live game target.</summary>
    public Marvel.View.BoardCardPresentation? Preview { get; init; }
}
