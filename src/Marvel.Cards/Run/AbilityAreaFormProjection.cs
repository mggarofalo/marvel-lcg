using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

// Exact identity faces belong to each projected board, not to the live card.
internal static class AbilityAreaFormProjection
{
    internal static List<AreaProjectionState>? TryTraceChangeForm(
        this AbilityAreaProjection projection, AbilityEffect effect,
        List<AreaProjectionState> states, long repetitions, long baseMultiplier)
    {
        if (effect is not AbilityEffect.ChangeForm change) return null;
        for (long repeat = 0; repeat < repetitions; repeat++)
        {
            foreach (var state in states)
            {
                int player = projection.context.Evaluator().Seat(change.Player);
                var identity = projection.context.World.Seats[player].IdentityCard;
                if (Destination(change, state, projection.context) is { } face)
                    state.SetFace(identity, face);
            }
        }
        return states;
    }

    internal static string? Destination(
        AbilityEffect.ChangeForm change, AreaProjectionState state,
        AbilityAdmissionContext context)
    {
        if (change.Player is AbilityPlayer.ChosenPlayer or AbilityPlayer.FirstPlayer)
            throw new RulesNotImplementedException(
                "projecting a form change for a potentially rebound player is not implemented");
        int player = context.Evaluator().Seat(change.Player);
        var identity = context.World.Seats[player].IdentityCard;
        if (state.Departed.Contains(identity.ObjectId)
            || state.ModifiedOf(context, identity, FormChangePermissions.ProhibitedField) > 0)
            return null;
        return AbilityFormDestinationResolver.Resolve(identity, context.World.Facts,
            state.FaceOf(identity), FormsOf(state, context, player), change.Destination);
    }

    internal static IReadOnlySet<string> FormsOf(
        AreaProjectionState state, AbilityAdmissionContext context, int player)
    {
        var seat = context.World.Seats[player];
        // Additional-form membership has its own lifecycle and is not yet projected.
        if (state.Departed.Concat(state.Entered).Any(id =>
                context.World.Cards[id].Owner == player
                && context.World.Facts.FormKeyword(context.World.Cards[id].FaceId) is not null))
            throw new RulesNotImplementedException("projecting additional-form card movement is not implemented");
        return Forms.OfIdentityFace(context.World, seat, context.World.Facts,
            state.FaceOf(seat.IdentityCard));
    }
}
