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
        ActivitySubject Subject(int id, ActivitySubjectRole role)
        {
            string name = Card(id, world, happened);
            CardFaceDescriptor? face = world.Areas.SelectMany(area => area.Cards.Concat(area.Removed))
                .FirstOrDefault(card => card.Id == id)?.Face;
            return new ActivitySubject(id, name, role) { Landscape = face?.Title == name
                && face.Kind is CardKind.MainScheme or CardKind.EncounterSideScheme };
        }
        ActivitySubject[] subjects = happened switch
        {
            AttackCompleted attack => [Subject(attack.Enemy, ActivitySubjectRole.Source),
                Subject(attack.Target, ActivitySubjectRole.Target),
                .. attack.Defender >= 0 ? new[] { Subject(attack.Defender, ActivitySubjectRole.Defender) } : []],
            BoostResolved boost => [Subject(boost.Card, ActivitySubjectRole.Boost),
                Subject(boost.Enemy, ActivitySubjectRole.Target)],
            WhenRevealedCanceled canceled => [
                .. canceled.Source is { } source ? new[] { Subject(source, ActivitySubjectRole.Source) } : [],
                Subject(canceled.Card, ActivitySubjectRole.Target)],
            CardAttached attached => [Subject(attached.Card, ActivitySubjectRole.Subject),
                Subject(attached.Host, ActivitySubjectRole.Target)],
            CardDetached detached => [Subject(detached.Card, ActivitySubjectRole.Subject),
                Subject(detached.Host, ActivitySubjectRole.Target)],
            _ => anchors.Select(id => Subject(id, ActivitySubjectRole.Subject)).ToArray(),
        };
        ActivityQuantity[] quantities = happened switch
        {
            FieldSet field => [new(field.Field, field.From, field.To)],
            BoostResolved boost => [new("boost_icons", null, boost.Icons),
                new(boost.Attacking ? "ATK" : "SCH", null, boost.Strength)],
            AttackCompleted { DamageDealt: { } damage } => [new("attack_damage", null, damage)],
            _ => [],
        };
        return new EventActivityDescriptor(subjects, quantities)
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
    }
}
