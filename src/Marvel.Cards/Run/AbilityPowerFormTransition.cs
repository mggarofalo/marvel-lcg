using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;

namespace Marvel.Cards.Run;

// Projects only identity faces; other form-dependent readers keep their guards.
internal static class AbilityPowerFormTransition
{
    internal static string? Destination(
        AbilityEffect.ChangeForm change, int seat, AbilityAdmissionScope cast, AbilityPowerState state)
    {
        var player = cast.World.Seats[seat];
        var identity = player.IdentityCard;
        if (!state.IdentityFaces.TryGetValue(seat, out var current))
        {
            if (SeatMayChange(state.FormsMayChange, seat))
                throw new RulesNotImplementedException("projecting form from an uncertain legacy seat is not implemented");
            current = identity.FaceId;
        }
        if (!AbilityProjectedCharacterPermissions.CanChangeForm(seat, cast, state)) return null;
        return AbilityFormDestinationResolver.Resolve(identity, cast.World.Facts, current,
            Forms.OfIdentityFace(cast.World, player, cast.World.Facts, current), change.Destination);
    }

    internal static AbilityPowerState Apply(
        AbilityEffect.ChangeForm change, int seat, AbilityAdmissionScope cast, AbilityPowerState state)
    {
        var destination = Destination(change, seat, cast, state);
        if (destination is null) return state;
        ulong bit = PlayerSeat(seat);
        bool isLiveFace = destination == cast.World.Seats[seat].IdentityCard.FaceId;
        return state with
        {
            FormsMayChange = isLiveFace ? state.FormsMayChange & ~bit : state.FormsMayChange | bit,
            IdentityFaces = isLiveFace ? state.IdentityFaces.Remove(seat) : state.IdentityFaces.SetItem(seat, destination),
        };
    }
}
