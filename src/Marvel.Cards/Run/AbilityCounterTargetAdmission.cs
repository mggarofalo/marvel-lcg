using Marvel.Cards.Dsl;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityTargetAdmission;

namespace Marvel.Cards.Run;

/// <summary>A counter placement affects its target only when it places counters.</summary>
internal static class AbilityCounterTargetAdmission
{
    internal static TargetLegality Of(AbilityEffect.PlaceCounters effect, AbilityAdmissionScope scope)
    {
        // rr:target.3: a target is valid if any part of the ability can affect it.
        // Composite target admission retains any other independently effective sentence.
        if (!AbilityFutureDiscardAdmission.MayHavePositiveAmount(effect.Count, scope)
            && !AbilityFutureResultAdmission.MayBePositive(effect.Count, scope))
            return TargetLegality.Invalid;
        var areas = AbilitySelectionAreaReads.For(effect.Card);
        var probe = scope.ForReachability(scope.Reachability with { FilteringContinuationOption = true });
        if (effect.Card is AbilityCardSelection.InAreas or AbilityCardSelection.Last
            or AbilityCardSelection.Query { Kind: AbilityCardQuery.InvocationTop }
            && areas.Count > 0 && !AbilityRuntimeQueries.SingularAreaQueryIsStable(areas, probe.Context))
            return TargetLegality.None;
        return Find(effect.Card, scope) is not null
            ? TargetLegality.Valid : TargetLegality.Invalid;
    }
}
