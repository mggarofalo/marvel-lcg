using Marvel.Rules.Events;
using static Marvel.View.EventSubjectDescriptions;

namespace Marvel.View;

/// <summary>Describes the engine's completed instruction without deriving a grouping from adjacency.</summary>
internal static class CompletedActivityPresentation
{
    internal static string Summary(ActivityCompleted activity, WorldDescriptor world)
    {
        if (activity.PhaseStep == ActivityPhaseStep.ReadyCards)
            return $"Readied {Cards(activity.Participants[0], activity, world)} simultaneously.";
        string summary = string.Join("; ", activity.Participants.Select(participant =>
            $"{ParticipantName(participant.Player, world)} {Past(activity.Verb)} {Cards(participant, activity, world)}"));
        return summary + (activity.PhaseStep == ActivityPhaseStep.DrawToHandSize ? " simultaneously." : ".");
    }

    internal static string ParticipantName(int player, WorldDescriptor world) =>
        player < 0 ? "The encounter deck" : Player(player, world);

    private static string Past(string verb) => verb switch
    {
        "Draw" => "drew",
        "Discard" => "discarded",
        "Ready" => "readied",
        _ => throw new InvalidOperationException($"activity verb {verb} has no presentation"),
    };

    private static string Cards(ActivityParticipant participant, ActivityCompleted activity, WorldDescriptor world)
    {
        int hidden = participant.Count - participant.Cards.Count;
        string visible = CardList(participant.Cards, world, activity);
        if (hidden == 0) return visible;
        string concealed = $"{hidden} card{(hidden == 1 ? "" : "s")}";
        return participant.Cards.Count == 0 ? concealed : $"{visible} and {concealed}";
    }

    internal static EventMotionKind Motion(ActivityCompleted activity) => activity.Verb == "Draw"
        ? EventMotionKind.HandGain : activity.Verb == "Ready" ? EventMotionKind.State : EventMotionKind.Move;
}
