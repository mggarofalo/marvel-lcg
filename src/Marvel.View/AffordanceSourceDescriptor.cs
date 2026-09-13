using Marvel.Rules.Prompts;

namespace Marvel.View;

/// <summary>An authorized object from which an affordance originates.</summary>
/// <remarks>
/// A null source means the engine offered a complete ungrouped fallback, not that a
/// client should guess a source from the affordance label.
/// </remarks>
public sealed record AffordanceSourceDescriptor(
    AffordanceAnchorKind AnchorKind,
    int? CardId,
    int? AreaId,
    int Controller);
