using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityResolutionAdmission;
using static Marvel.Cards.Run.AbilityTargetAdmission;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityPowerHealthTrace;
using static Marvel.Cards.Run.AbilityPowerTrace;
using static Marvel.Cards.Run.AbilityPowerSelectionTrace;
using static Marvel.Cards.Run.AbilityProjectedStateQueries;

namespace Marvel.Cards.Run;

internal static class AbilityHitPointAssignment
{
    internal static AbilityStructuralOutcome Execute(
        AbilityEffect.SetHitPoints assignment, AbilityDamageAndThreatContext context) =>
        AbilityDamageAndThreatQueries.Find(assignment.Card, context) is { } dial
            && HitPointAssignment.Set(context.World, dial, assignment.Remaining, context.Trigger, context.Events)
                ? AbilityStructuralOutcome.Full : AbilityStructuralOutcome.None;

    internal static TargetLegality TargetLegalityOf(AbilityEffect node, AbilityAdmissionScope cast) =>
        Outcome(node, cast) == AdmissionResolution.None ? TargetLegality.Invalid : TargetLegality.Valid;

    internal static void ValidateShape(AbilityEffect effect, bool repeated = false)
    {
        if (repeated && effect is AbilityEffect.SetHitPoints)
            throw new RulesNotImplementedException("hit-point assignment inside repeated frames is not implemented");
        bool childRepeated = repeated || effect is AbilityEffect.EachPlayer or AbilityEffect.ForEach or AbilityEffect.EachTime;
        foreach (var child in AbilityEffectStructure.AllEffectChildren(effect)) ValidateShape(child, childRepeated);
    }

    internal static AdmissionResolution Outcome(AbilityEffect node, AbilityAdmissionScope cast)
    {
        var assignment = (AbilityEffect.SetHitPoints)node;
        var card = Find(assignment.Card, cast);
        return card is null ? AdmissionResolution.None : Outcome(
            cast.World, card, assignment.Remaining,
            DamagePlacement.Health(cast.World, cast.World.Facts, card), card.Damage);
    }

    internal static AdmissionResolution Outcome(
        World world, Card card, long remaining, long maximum, long damage) =>
        HitPointAssignment.RequiredDamage(world, card, remaining, maximum) == damage
            ? AdmissionResolution.None : AdmissionResolution.Full;

    internal static AbilityPowerState ProjectPower(
        AbilityEffect node, AbilityAdmissionScope cast, AbilityPowerState state)
    {
        var assignment = (AbilityEffect.SetHitPoints)node;
        var card = PowerFind(assignment.Card, cast, state);
        if (card is null) return state;
        long damage = HitPointAssignment.RequiredDamage(
            cast.World, card, assignment.Remaining, PowerHealth(state, card, cast));
        return SetPowerDamage(state, card, damage,
            cast.World.Seats[cast.World.FirstPlayer].IdentityCard, cast);
    }

    internal static AbilityProjectionResolution ProjectedOutcome(
        AbilityEffect node, AreaProjectionState state, AbilityAdmissionContext context)
    {
        var assignment = (AbilityEffect.SetHitPoints)node;
        var card = ProjectedFind(assignment.Card, state, context);
        return card is not null && Outcome(
            context.World, card, assignment.Remaining, state.HealthOf(context, card), state.DamageOf(card))
                == AdmissionResolution.Full ? AbilityProjectionResolution.Full : AbilityProjectionResolution.None;
    }

    internal static List<AreaProjectionState>? Trace(
        AbilityAreaProjection projection, AbilityEffect effect, List<AreaProjectionState> states)
    {
        if (effect is not AbilityEffect.SetHitPoints assignment) return null;
        foreach (var state in states)
        {
            var card = ProjectedFind(assignment.Card, state, projection.context);
            if (card is null) continue;
            state.Damage[card.ObjectId] = HitPointAssignment.RequiredDamage(
                projection.context.World, card, assignment.Remaining,
                state.HealthOf(projection.context, card));
        }
        return states;
    }
}
