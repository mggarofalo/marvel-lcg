using System.Globalization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.View.EventSubjectDescriptions;

namespace Marvel.View;

/// <summary>Describes authorized field changes and their presentation emphasis.</summary>
internal static class EventFieldPresentation
{
    private static readonly HashSet<string> HealthFields = new(
        ["health", "hitpoints", "hit_points"], StringComparer.Ordinal);
    private static readonly HashSet<string> DamageFields = new(
        ["damage", "k_damage"], StringComparer.Ordinal);

    internal static bool IsReceiptRelevant(FieldSet set) =>
        set.Field == "is_exhaust" || set.Field == "k_threat"
        || set.Field.StartsWith("c_", StringComparison.Ordinal)
        || set.From is not null && set.To is not null;

    internal static EventMotionKind Motion(FieldSet set)
    {
        string field = set.Field.ToLowerInvariant();
        long? change = set.From is null || set.To is null ? null : set.To - set.From;
        if (change is null or 0)
        {
            return EventMotionKind.State;
        }

        if (HealthFields.Contains(field))
        {
            return HealthMotion(change.Value);
        }

        if (DamageFields.Contains(field))
        {
            return DamageMotion(change.Value);
        }

        if (field == "k_threat")
        {
            return EventMotionKind.Threat;
        }

        if (field.StartsWith("c_", StringComparison.Ordinal)
            || field == EncounterDeck.AccelerationToken)
        {
            return EventMotionKind.Counter;
        }

        return EventMotionKind.State;
    }

    private static EventMotionKind HealthMotion(long change) =>
        change > 0 ? EventMotionKind.Heal : EventMotionKind.Damage;

    private static EventMotionKind DamageMotion(long change) =>
        change < 0 ? EventMotionKind.Heal : EventMotionKind.Damage;

    internal static string Summary(FieldSet set, WorldDescriptor world)
    {
        string subject = Card(set.Card, world, set);
        if (set.Field == "is_exhaust")
        {
            return set.To == 1
                ? $"{subject} became exhausted."
                : $"{subject} became ready.";
        }
        if (CounterName(set.Field) is { } counters)
        {
            return CounterSummary(subject, counters, set);
        }
        string field = Words(set.Field).ToLowerInvariant();
        if (set.Field == "k_threat" && set.From is { } before && set.To is { } after)
        {
            string change = after < before ? $"{before - after} removed" : $"{after - before} added";
            return $"{subject} threat: {before} → {after} ({change}).";
        }
        return FieldSummary(subject, field, set);
    }

    internal static string CueSummary(FieldSet set, WorldDescriptor world)
    {
        if (set.From is null || set.To is null || set.Field == "is_exhaust")
            return Summary(set, world);
        string field = set.Field.ToLowerInvariant();
        string label = HealthFields.Contains(field) ? "HP"
            : DamageFields.Contains(field) ? "damage"
            : field == "k_threat" ? "threat"
            : CounterName(field) ?? Words(field).ToLowerInvariant();
        return $"{Card(set.Card, world, set)} · {label} {Value(set.From)} → {Value(set.To)}";
    }

    private static string? CounterName(string field) =>
        field.StartsWith("c_", StringComparison.Ordinal)
            ? Words(field[2..]).ToLowerInvariant() + " counters"
            : field == EncounterDeck.AccelerationToken ? "acceleration tokens" : null;

    private static string FieldSummary(string subject, string field, FieldSet set)
    {
        if (set.From is null)
        {
            return $"{subject} gained {field} {Value(set.To)}.";
        }

        if (set.To is null)
        {
            return $"{subject} lost {field} {Value(set.From)}.";
        }

        return $"{subject} changed {field} from {Value(set.From)} to {Value(set.To)}.";
    }

    private static string CounterSummary(string subject, string counters, FieldSet set)
    {
        if (set.From is null) return $"{subject} gained {Value(set.To)} {counters}.";
        if (set.To is null) return $"{subject} lost {Value(set.From)} {counters}.";
        string change = set.To < set.From
            ? $"{set.From - set.To} removed"
            : $"{set.To - set.From} added";
        return $"{subject} {counters}: {Value(set.From)} → {Value(set.To)} ({change}).";
    }

    private static string Value(long? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? "an absent value";

}
