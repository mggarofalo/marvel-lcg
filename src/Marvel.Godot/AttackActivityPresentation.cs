using Marvel.View;

namespace Marvel.Godot;

/// <summary>Maps authoritative attack context to a passive board cue through response staging.</summary>
internal static class AttackActivityPresentation
{
    internal static EventPresentation? From(ActiveAttackDescriptor? attack)
    {
        if (attack is null) return null;
        string caption = $"{attack.Source.Name} → {attack.Target.Name}";
        return new EventPresentation(caption, "Attack in progress",
            [attack.Source.Card, attack.Target.Card], EventMotionKind.Attack)
        {
            CueSummary = caption,
            Activity = new EventActivityDescriptor([attack.Source, attack.Target], [])
                { Operation = "Attack_Active" },
        };
    }

    internal static EventPresentation? DuringTransition(ActiveAttackDescriptor? previous,
        ActiveAttackDescriptor? current, IReadOnlyList<EventPresentation> playback)
    {
        EventPresentation? accepted = playback.FirstOrDefault(entry => entry.Activity?.Operation == "Attack_Presentation");
        if (previous is null) return From(current) ?? accepted;
        // A defender may be accepted and the attack finish in the same response.
        // Its completed occurrence supplies the final target while consequences are presented.
        if (HasSource(accepted, previous.Source.Card)) return accepted;
        return From(current?.Source.Card == previous.Source.Card ? current : previous);
    }

    private static bool HasSource(EventPresentation? presentation, int source) =>
        presentation?.Activity?.Subjects.Any(subject => subject.Role == ActivitySubjectRole.Source
            && subject.Card == source) == true;
}
