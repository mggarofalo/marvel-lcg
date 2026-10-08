namespace Marvel.View;

/// <summary>An authorized originating face, optionally linked to the same live copy.</summary>
public sealed record CardValueSourceDescriptor(
    string FaceId,
    string Title,
    int? CardId,
    bool Historical)
{
    /// <summary>Authorized printed source text; empty for replacement identities.</summary>
    public string RulesText { get; init; } = string.Empty;

    /// <summary>The same text with the printed formatting vocabulary.</summary>
    public string RulesMarkup { get; init; } = string.Empty;
}
