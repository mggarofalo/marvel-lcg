using Marvel.Rules.Events;
using Marvel.Rules.State;
using static Marvel.View.EventSubjectDescriptions;

namespace Marvel.View;

/// <summary>Describes only already-filtered events; no rule calculation or historical art recovery.</summary>
internal static class EventActivityProjection
{
    internal static EventActivityDescriptor From(GameEvent happened, WorldDescriptor world,
        IReadOnlyList<int> anchors)
    {
        ActivitySubject[] subjects = happened switch
        {
            AttackCompleted attack => [Subject(happened, world, attack.Enemy, ActivitySubjectRole.Source),
                Subject(happened, world, attack.Target, ActivitySubjectRole.Target),
                .. attack.Defender >= 0 ? new[] { Subject(happened, world, attack.Defender, ActivitySubjectRole.Defender) } : []],
            BoostResolved boost => [Subject(happened, world, boost.Card, ActivitySubjectRole.Boost),
                Subject(happened, world, boost.Enemy, ActivitySubjectRole.Target)],
            WhenRevealedCanceled canceled => [
                .. canceled.Source is { } source ? new[] { Subject(happened, world, source, ActivitySubjectRole.Source) } : [],
                Subject(happened, world, canceled.Card, ActivitySubjectRole.Target)],
            CardAttached attached => [Subject(happened, world, attached.Card, ActivitySubjectRole.Subject),
                Subject(happened, world, attached.Host, ActivitySubjectRole.Target)],
            CardDetached detached => [Subject(happened, world, detached.Card, ActivitySubjectRole.Subject),
                Subject(happened, world, detached.Host, ActivitySubjectRole.Target)],
            _ => anchors.Select(id => Subject(happened, world, id, ActivitySubjectRole.Subject)).ToArray(),
        };
        ActivityQuantity[] quantities = happened switch
        {
            FieldSet field => [new(field.Field, field.From, field.To)],
            BoostResolved boost => [new("boost_icons", null, boost.Icons),
                new(boost.Attacking ? "ATK" : "SCH", null, boost.Strength)],
            AttackCompleted { DamageDealt: { } damage } => [new("attack_damage", null, damage)],
            _ => [],
        };
        var projected = new EventActivityDescriptor(subjects, quantities)
        {
            Operation = happened.Verb,
            StatusChange = happened switch
            {
                CardsCreated created when created.Area.Zone == "StatusArea" => ActivityStatusChange.Applied,
                CardsMoved added when added.To.Zone == "StatusArea" => ActivityStatusChange.Applied,
                CardsMoved removed when removed.From.Zone == "StatusArea" => ActivityStatusChange.Removed,
                _ => ActivityStatusChange.None,
            },
            From = happened is CardsMoved moved ? Area(moved.From, world) : string.Empty,
            To = happened is CardsMoved arrived ? Area(arrived.To, world) : string.Empty,
        };
        return ActivityContextProjection.With(projected, happened, world);
    }
    private static ActivitySubject Subject(GameEvent happened, WorldDescriptor world, int id, ActivitySubjectRole role)
    {
        string name = Card(id, world, happened);
        CardDescriptor? card = world.Areas.SelectMany(area => area.Cards.Concat(area.Removed))
            .FirstOrDefault(card => card.Id == id);
        BoardCardPresentation? reference = happened.SubjectFaces?.TryGetValue(id, out string? faceId) == true
            ? world.EventFaces.GetValueOrDefault(faceId) : null;
        return new ActivitySubject(id, name, role)
        {
            Landscape = reference is not null
                ? reference.Kind is "MAIN SCHEME" or "ENCOUNTER SIDE SCHEME"
                : card?.Face?.Title == name
                    && card.Face.Kind is CardKind.MainScheme or CardKind.EncounterSideScheme,
            Reference = reference,
        };
    }

}
