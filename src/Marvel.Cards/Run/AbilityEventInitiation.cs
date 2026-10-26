using Marvel.Rules.Play;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Opens an observed paid-event initiation before running its effect body.</summary>
internal static class AbilityEventInitiation
{
    internal static bool Open(AbilityResolutionExecution execution, AbilityResolutionState cast)
    {
        // rr:initiating-abilities.step.6: "The card commences being played".
        // The dedicated condition name is an engine choice; completion responses
        // remain on the containing card-play occurrence after event resolution.
        var occurrence = new Occurrence(0, [EventPlayTiming.Initiated],
            Subject: cast.Source.ObjectId, Player: cast.Player);
        if (execution.Waiting(cast.World, occurrence, WindowKind.Interrupt).Count == 0)
            return false;
        cast.World.Agenda.NowEventPlayInitiated(cast.World.Agenda.Current?.Round ?? 0,
            cast.Source.ObjectId, cast.Player);
        return true;
    }
}

