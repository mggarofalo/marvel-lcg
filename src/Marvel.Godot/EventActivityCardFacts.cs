using System.Globalization;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Applies exact receipt values to a card's temporary display, never to a world.</summary>
internal static class EventActivityCardFacts
{
    internal static IEnumerable<EventPresentation> Facts(EventPresentation entry)
    {
        if (entry.Details.Count == 0 || entry.Activity?.StatusChange is ActivityStatusChange.Applied or ActivityStatusChange.Removed)
            yield return entry;
        foreach (EventPresentation detail in entry.Details)
            foreach (EventPresentation fact in Facts(detail)) yield return fact;
    }

    internal static BoardCardPresentation Apply(BoardCardPresentation card, EventActivityDescriptor activity)
    {
        if (card.Concealed || !activity.Subjects.Any(subject => subject.Card == card.TargetId)) return card;
        foreach (ActivityQuantity quantity in activity.Quantities)
            card = ApplyQuantity(card, quantity);
        card = ApplyStatuses(card, activity);
        return activity.Operation == "Ready" ? card with { Status = "READY" } : card;
    }

    private static BoardCardPresentation ApplyQuantity(BoardCardPresentation card, ActivityQuantity quantity)
    {
        if (quantity.After is not { } after) return card;
        string value = after.ToString(CultureInfo.InvariantCulture);
        return quantity.Field switch
        {
            "health" => card with { Fields = Replace(card.Fields, "HEALTH", Health(card, value)), DamageKnown = false },
            "k_threat" => card with { Fields = Replace(card.Fields, "THREAT", value) },
            "is_exhaust" => card with { Status = after == 1 ? "EXHAUSTED" : "READY" },
            "k_damage" => card with { Damage = after, DamageKnown = true },
            _ when quantity.Field.StartsWith("c_", StringComparison.Ordinal) => card with
                { Counters = Replace(card.Counters, quantity.Field[2..].ToUpperInvariant(), value) },
            _ => card,
        };
    }

    private static string Health(BoardCardPresentation card, string remaining) =>
        card.EffectiveValues.TryGetValue("HP", out CardEffectiveValue? maximum)
            ? $"{remaining}/{maximum.CurrentValue.ToString(CultureInfo.InvariantCulture)}" : remaining;

    private static BoardCardPresentation ApplyStatuses(BoardCardPresentation card, EventActivityDescriptor activity)
    {
        if (activity.StatusChange != ActivityStatusChange.None
            && activity.Subjects.Any(subject => subject.Card == card.TargetId && subject.Role == ActivitySubjectRole.Target))
        {
            var statuses = card.Statuses.ToList();
            foreach (ActivitySubject status in activity.Subjects.Where(subject => subject.Role == ActivitySubjectRole.Subject))
            {
                if (activity.StatusChange == ActivityStatusChange.Applied) statuses.Add(status.Name);
                else
                {
                    int index = statuses.FindIndex(name => string.Equals(name, status.Name, StringComparison.OrdinalIgnoreCase));
                    if (index >= 0) statuses.RemoveAt(index);
                }
            }
            card = card with { Statuses = statuses };
        }
        return card;
    }

    private static BoardFieldPresentation[] Replace(IReadOnlyList<BoardFieldPresentation> fields, string name, string value) =>
        [.. fields.Where(field => field.Name != name), new(name, value)];
}
