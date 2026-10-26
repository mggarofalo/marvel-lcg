using static Marvel.Cards.Run.AbilityAdmission;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

// Permission queries share the projected departure and modifier facts with
// characteristic evaluation. Constant abilities end when their source leaves.
internal static class AbilityProjectedCharacterPermissions
{
    internal static bool CanReady(
        Card card, AbilityAdmissionScope cast, AbilityPowerState state) =>
        AbilityProgramQueries.CanReady(cast.World, cast.Context.Program, card,
            state.Discarded, AbilityRepeatedCharacteristicTrace.TraceModified(
                card, ReadinessPermissions.ProhibitedField, cast,
                state.Discarded, state.Modifiers));

    internal static bool CanChangeForm(
        int seat, AbilityAdmissionScope cast, AbilityPowerState state) =>
        AbilityRepeatedCharacteristicTrace.TraceModified(
            cast.World.Seats[seat].IdentityCard,
            FormChangePermissions.ProhibitedField, cast,
            state.Discarded, state.Modifiers) <= 0;

    internal static bool PriorCanChangeForm(int seat, AbilityAdmissionScope cast)
    {
        if (FormChangePermissions.CanChange(
            cast.World, cast.World.Seats[seat].IdentityCard)) return true;
        Marvel.Rules.Play.RuleProjection<AbilityPowerState> projected =
            AbilityPowerStateFactory.Initial(cast);
        foreach (var prior in cast.Reachability.PriorSteps)
            projected = AbilityPowerTrace.PowerStateAfter(
                prior, cast, false, false, projected);
        if (projected is Marvel.Rules.Play.RuleProjection<AbilityPowerState>.Unsupported)
            return true;
        return AbilityPowerAlternatives.PowerPaths(projected)
            .Any(state => CanChangeForm(seat, cast, state));
    }
}
