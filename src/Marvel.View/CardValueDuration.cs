namespace Marvel.View;

/// <summary>The stated bounds of a contribution, without executable conditions.</summary>
public sealed record CardValueDuration(
    string? Until,
    string? OnCondition,
    int? Uses,
    bool WhileInPlay);
