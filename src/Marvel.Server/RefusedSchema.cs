namespace Marvel.Server;

/// <summary>The holders of a save written under another session schema.</summary>
/// <remarks>The save body cannot be read, so only its label and authority are kept to
/// answer those holders with <c>save_incompatible</c>.</remarks>
public sealed record RefusedSchema(string GameId, IReadOnlyList<StoredAuthority> Authorities, string Category);
