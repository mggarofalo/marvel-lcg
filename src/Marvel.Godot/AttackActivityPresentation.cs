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
                { Operation = "Attack_Active", AttackIdentity = attack.Identity },
        };
    }

    internal static EventPresentation? DuringTransition(ActiveAttackDescriptor? previous,
        ActiveAttackDescriptor? current, IReadOnlyList<EventPresentation> playback)
    {
        return From(previous);
    }

    internal static EventPresentation? Advance(EventPresentation? active, EventPresentation? beat)
    {
        if (beat?.Activity is not { } activity) return active;
        if (activity.AttackBoundary == true || activity.Operation == "Attack_Presentation") return beat;
        return activity.AttackBoundary == false && activity.AttackIdentity == active?.Activity?.AttackIdentity
            ? null : active;
    }
}
