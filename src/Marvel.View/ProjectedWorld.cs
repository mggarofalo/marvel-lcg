using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>
/// One world already filtered for a scope, awaiting the event list whose
/// visible card faces complete its description.
/// </summary>
public sealed class ProjectedWorld
{
    private readonly ICardFacts facts;
    private readonly WorldDescriptor visible;
    private readonly HashSet<int> addressable;
    private readonly HashSet<int> readable;

    internal ProjectedWorld(
        ICardFacts facts,
        WorldDescriptor visible,
        Prompt? prompt,
        HashSet<int> addressable,
        HashSet<int> readable)
    {
        this.facts = facts;
        this.visible = visible;
        Prompt = prompt;
        this.addressable = addressable;
        this.readable = readable;
    }

    internal Prompt? Prompt { get; }

    /// <summary>Describes the world with the faces visible in one event list.</summary>
    public WorldDescriptor WithEvents(IReadOnlyList<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        return WithSafeEvents(SafeEvents(events));
    }

    internal List<GameEvent> SafeEvents(IReadOnlyList<GameEvent> events) =>
        VisibilityEventFilter.Filter(events, addressable, readable);

    internal WorldDescriptor WithSafeEvents(IReadOnlyList<GameEvent> safeEvents) =>
        visible with { EventFaces = EventFaceProjection.From(safeEvents, facts) };
}
