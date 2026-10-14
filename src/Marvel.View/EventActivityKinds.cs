using Marvel.Rules.Events;

namespace Marvel.View;

internal static class EventActivityKinds
{
    internal static EventActivityKind From(GameEvent happened) => happened switch
    {
        PlayAreaJoined or PlayAreaDetached => EventActivityKind.Topology,
        BoostResolved => EventActivityKind.Boost,
        CardAttached => EventActivityKind.Installation,
        CardsFlipped { FaceUp: true } => EventActivityKind.Reveal,
        CardsMoved moved when moved.From.Zone != "HandsArea" && moved.To.Zone != "HandsArea"
            && moved.From.Zone != "StatusArea" && moved.To.Zone != "StatusArea"
            && moved.Verb is not ("Defeat" or "Eliminate") => EventActivityKind.Travel,
        _ => EventActivityKind.Consequence,
    };
}
