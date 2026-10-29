using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// The authored next-turn form prohibition always targets the resolving identity.
internal static class AbilityLastingDuration
{
    internal static Duration For(World world, Card target, string until) =>
        until == TimingPoints.EndOfNextPlayerTurn
            ? Duration.UntilNextTurnEnds(world.Seats.Single(seat => seat.IdentityCard == target).Index)
            : Duration.UntilEndOf(until);
}
