namespace Marvel.Protocol;

/// <summary>Public printed catalog facts, never a live or concealed game-card identity.</summary>
public sealed record DeckCard(string Face, string Title, string Kind, string Aspect,
    IReadOnlyList<string> Traits, string Text, string? Cost)
{
    /// <summary>The printed subtitle, when any.</summary>
    public string Subtitle { get; init; } = "";
}
