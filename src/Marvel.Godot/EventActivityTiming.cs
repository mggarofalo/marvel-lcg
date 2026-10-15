using Marvel.View;

namespace Marvel.Godot;

/// <summary>Product pacing for an authorized occurrence, independent of response length.</summary>
internal static class EventActivityTiming
{
    internal static double Onset(EventPresentation? entry) => entry?.Activity?.Operation switch
    {
        "Ready" => .28,
        "Draw" => .16,
        "Attack_Presentation" => .2,
        _ => entry?.Motion switch
        {
            EventMotionKind.Damage or EventMotionKind.Defeat => .22,
            EventMotionKind.Move when entry.Details.Count == 0 => .12,
            _ => .18,
        },
    };

    internal static double Duration(EventPresentation? entry)
    {
        if (entry is null) return 0;
        if (AttackDuration(entry.Activity) is { } attack) return attack;
        if (entry.Activity?.PhaseBoundary == true) return .5;
        if (entry.Activity?.PhaseStep == true) return .8;
        if (BriefTravel(entry)) return .38;
        return ConsequenceDuration(entry);
    }

    // A cause establishes direction; its consequence has its own readable beat.
    private static double? AttackDuration(EventActivityDescriptor? activity) => activity switch
    {
        { AttackBoundary: false } => .01,
        { AttackBoundary: true } or { Operation: "Attack_Presentation" } => .48,
        _ => null,
    };

    // Movement endpoints are supplied facts; short travel does not need a second reading pause.
    private static bool BriefTravel(EventPresentation entry) => entry.Motion == EventMotionKind.Move
        && entry.Details.Count == 0 && entry.ForcedAbilityNotification is null
        && entry.Activity is { From.Length: > 0, To.Length: > 0, Quantities.Count: 0 } activity
        && (activity.Kind == EventActivityKind.Travel || activity.Operation is "" or "Move");

    private static double ConsequenceDuration(EventPresentation entry)
    {
        if (entry.Motion is EventMotionKind.Flip or EventMotionKind.Defeat or EventMotionKind.Terminal)
            return .9;
        if (entry.Activity?.Quantities.Any(quantity => quantity.Field == "boost_icons") == true)
            return .85;
        if (entry.Motion is EventMotionKind.Damage or EventMotionKind.Heal or EventMotionKind.Threat
            or EventMotionKind.Status or EventMotionKind.Attack) return .75;
        if (entry.Details.Count > 1 || entry.Motion == EventMotionKind.HandGain) return .8;
        return .65;
    }
}
