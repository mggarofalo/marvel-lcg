namespace Marvel.Protocol;

/// <summary>An admitted identity and its authored signature and starter recipes.</summary>
public sealed record DeckHero(string Key, string Name, IReadOnlyList<string> IdentityFaces,
    IReadOnlyList<string> Signature, IReadOnlyList<string> Starter);
