using Marvel.Rules.Events;

namespace Marvel.View;

/// <summary>Adds the reviewed structural subjects to one presented event.</summary>
internal static class EventRelationshipProjection
{
    internal static EventPresentation WithSubjects(EventPresentation presentation, GameEvent happened) =>
        presentation with { Relationships = Subjects(presentation.Anchors, happened) };

    private static TableRelationshipDescriptor[] Subjects(
        IReadOnlyList<int> anchors,
        GameEvent happened) => happened switch
        {
            CardAttached attached => [new TableRelationshipDescriptor(
                RelationshipKind.Attachment, attached.Card, attached.Host)],
            CardDetached detached => [new TableRelationshipDescriptor(
                RelationshipKind.Attachment, detached.Card, detached.Host)],
            _ => anchors.Select(id => new TableRelationshipDescriptor(
                RelationshipKind.Result, id, Related: null)).ToArray(),
        };
}
