using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Matches a response to a ready against the cards that instruction readied.</summary>
/// <remarks>
/// A ready instruction can ready several cards at once; a response to the readied
/// condition may answer any card that instruction readied. Engine choice: as for
/// spent resource cards, "you" names the controller of that readied card rather
/// than the instructing player.
/// </remarks>
internal static class ReadiedTriggerMatching
{
    internal static bool Applies(Occurrence occurrence) =>
        occurrence.Is(CardReadying.Readied) && occurrence.Readying is not null;

    internal static bool Target(World world, AbilityTrigger trigger, Card card, Occurrence occurrence, int? restricted)
    {
        int controller = restricted ?? AbilityCardQueries.ControllerOf(world, card);
        return occurrence.Readying!.ReadiedFacts(world).Any(readied =>
            AbilityTriggerRoleMatching.Role(world, trigger.Target, card, readied, restricted)
            && (trigger.Player != AbilityPlayers.You || readied.Controller == controller));
    }
}
