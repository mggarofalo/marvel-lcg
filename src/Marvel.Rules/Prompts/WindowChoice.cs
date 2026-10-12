namespace Marvel.Rules.Prompts;

/// <summary>One engine-offered complete optional-window intention; a wire type.</summary>
public sealed record WindowChoice(int? TargetId, string Label, string? Description)
{
    /// <summary>Public card identities named by the instruction.</summary>
    public IReadOnlyList<int> RecipientIds { get; init; } = [];
}
