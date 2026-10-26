using Marvel.Rules.Play;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Opens an observed paid-event initiation before running its effect body.</summary>
internal static class AbilityEventInitiation
{
    internal static EventPlayInitiation? Open(AbilityResolutionExecution execution, AbilityResolutionState cast)
    {
        // rr:initiating-abilities.step.6: "The card commences being played".
        // The dedicated condition name is an engine choice; completion responses
        // remain on the containing card-play occurrence after event resolution.
        var occurrence = new Occurrence(0, [EventPlayTiming.Initiated],
            Subject: cast.Source.ObjectId, Player: cast.Player)
        { EventRemovesThreat = AbilityEventThreatFacts.RemovesThreat(execution, cast) };
        if (execution.Waiting(cast.World, occurrence, WindowKind.Interrupt).Count == 0)
            return null;
        return cast.World.Agenda.NowEventPlayInitiated(cast.World.Agenda.Current?.Round ?? 0,
            cast.Source.ObjectId, cast.Player, occurrence.EventRemovesThreat);
    }

    internal static void CompleteCancellation(AbilityResolutionExecution execution, AbilityResolutionState cast)
    {
        // rr:event.2: "Only the effects are canceled." Costs remain paid;
        // normal discard and card-play responses still follow cancellation.
        var address = cast.ResolutionAbility!.Value;
        var ability = execution.Pending(cast.Source, address);
        execution.Use(cast.World, cast.Source, ability, cast.Occurrence);
        cast.Occurrence.Resolution.BeginCard(cast.Source.ObjectId, [address]);
        cast.Occurrence.Resolution.Cancel(address);
        execution.DiscardEvent(cast.Source, cast);
        cast.CompleteResolution();
    }
}
