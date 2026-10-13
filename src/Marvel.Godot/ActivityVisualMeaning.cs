using System.Globalization;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>The tabletop's presentation vocabulary for supplied occurrences and quantities.</summary>
internal static class ActivityVisualMeaning
{
    internal static string Title(EventPresentation entry) => entry.Motion switch
    {
        EventMotionKind.Attack => "ATTACK RESULT",
        EventMotionKind.Damage => "DAMAGE RESULT",
        EventMotionKind.Heal => "HEALING RESULT",
        EventMotionKind.Threat => "THREAT RESULT",
        EventMotionKind.Status => entry.Activity?.StatusChange == ActivityStatusChange.Removed ? "STATUS REMOVED" : "STATUS APPLIED",
        EventMotionKind.Counter => "COUNTER RESULT",
        EventMotionKind.Defeat => "DEFEATED",
        EventMotionKind.Flip => "REVEAL / FORM",
        EventMotionKind.HandGain => "CARDS GAINED",
        _ => entry.Activity?.Quantities.Any(q => q.Field == "boost_icons") == true ? "BOOST RESOLVED" : "RESOLVED",
    };

    internal static string Participants(EventPresentation entry)
    {
        var subjects = entry.Activity?.Subjects ?? [];
        ActivitySubject? source = subjects.FirstOrDefault(s => s.Role is ActivitySubjectRole.Source or ActivitySubjectRole.Boost);
        ActivitySubject? target = subjects.FirstOrDefault(s => s.Role == ActivitySubjectRole.Target);
        string names = source is not null && target is not null ? $"{source.Name} → {target.Name}"
            : string.Join(" · ", subjects.Where(s => s.Role != ActivitySubjectRole.Defender).Select(s => s.Name).Distinct());
        ActivitySubject? defender = subjects.FirstOrDefault(s => s.Role == ActivitySubjectRole.Defender);
        return defender is null ? names : names + $" · DEF {defender.Name}";
    }

    internal static string Result(EventPresentation entry)
    {
        if (entry.Activity is not { } activity) return entry.CueSummary ?? entry.Summary;
        if (activity.StatusChange != ActivityStatusChange.None)
            return (activity.StatusChange == ActivityStatusChange.Applied ? "+ " : "− ")
                + string.Join(" · ", activity.Subjects.Where(subject => subject.Role == ActivitySubjectRole.Subject)
                    .Select(subject => subject.Name));
        if (activity.Quantities.Count > 0) return string.Join(" · ", activity.Quantities.Select(Quantity));
        if (activity.To.Length > 0) return $"→ {activity.To}";
        return entry.CueSummary ?? entry.Summary;
    }

    internal static string Role(EventPresentation entry, ActivitySubject subject) => subject.Role switch
    {
        ActivitySubjectRole.Source => entry.Motion == EventMotionKind.Attack ? "ATTACKER" : "SOURCE",
        ActivitySubjectRole.Target => entry.Motion == EventMotionKind.Attack ? "ATTACK TARGET" : "AFFECTED",
        ActivitySubjectRole.Defender => "DEFENDER",
        ActivitySubjectRole.Boost => "BOOST",
        _ => Title(entry),
    };

    internal static string LocalResult(EventPresentation entry, ActivitySubject subject)
    {
        if (entry.Activity?.Quantities.Any(quantity => quantity.Field == "boost_icons") == true)
        {
            var quantities = entry.Activity.Quantities.Where(quantity => subject.Role == ActivitySubjectRole.Boost
                ? quantity.Field == "boost_icons" : quantity.Field != "boost_icons");
            return string.Join(" · ", quantities.Select(Quantity));
        }
        return Result(entry);
    }

    internal static string Quantity(ActivityQuantity quantity)
    {
        string label = quantity.Field switch
        {
            "health" or "hitpoints" or "hit_points" => "HP",
            "k_damage" or "damage" => "damage",
            "k_threat" => "threat",
            "boost_icons" => "boost",
            "attack_damage" => "damage dealt",
            "is_exhaust" => "ready state",
            _ when quantity.Field.StartsWith("c_", StringComparison.Ordinal) => quantity.Field[2..] + " counters",
            _ => quantity.Field,
        };
        if (quantity.Field == "is_exhaust") return quantity.After == 1 ? "EXHAUSTED" : "↑ READY";
        string Value(long? value) => value?.ToString(CultureInfo.InvariantCulture) ?? "absent";
        return quantity.Before is null ? $"{label} {Value(quantity.After)}"
            : $"{label} {Value(quantity.Before)} → {Value(quantity.After)}";
    }

    internal static IReadOnlyList<EventPresentation> Settled(IReadOnlyList<EventPresentation> entries)
    {
        // This is a presentation selection, not an aggregation of gameplay quantities.
        var meaningful = entries.Where(entry => entry.Motion is EventMotionKind.Attack or EventMotionKind.Damage
            or EventMotionKind.Heal or EventMotionKind.Threat or EventMotionKind.Status or EventMotionKind.Defeat
            || entry.Activity?.Quantities.Any(q => q.Field == "boost_icons") == true).ToArray();
        // Distinct objects retain their latest receipt; local badges retain other visible results.
        var chosen = (meaningful.Length > 0 ? meaningful : entries)
            .GroupBy(entry => string.Join(",", entry.Activity?.Subjects.Select(subject => subject.Card) ?? entry.Anchors))
            .Select(group => group.Last()).TakeLast(2).ToList();
        EventPresentation? attack = entries.LastOrDefault(entry => entry.Motion == EventMotionKind.Attack);
        if (attack is not null && !chosen.Contains(attack)) chosen[0] = attack;
        return chosen;
    }
}
