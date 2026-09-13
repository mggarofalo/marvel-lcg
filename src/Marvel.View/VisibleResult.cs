using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>The complete response payload after visibility enforcement.</summary>
public sealed record VisibleResult(
    WorldDescriptor World,
    Prompt? Prompt,
    IReadOnlyList<GameEvent> Events);
