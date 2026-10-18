namespace Marvel.Protocol;

/// <summary>A read-only assessment of an editable, possibly invalid draft.</summary>
public sealed record DeckQuery(string Hero, IReadOnlyList<string> Cards);
