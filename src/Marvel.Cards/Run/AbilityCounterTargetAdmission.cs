using Marvel.Cards.Dsl;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityTargetAdmission;

namespace Marvel.Cards.Run;

/// <summary>A counter placement affects its target only when it places counters.</summary>
internal static class AbilityCounterTargetAdmission
{
    internal static TargetLegality Of(AbilityCounterEffect.PlaceCounters effect, AbilityAdmissionScope scope)
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

    internal static TargetLegality Of(AbilityCounterEffect.MoveCounters effect, AbilityAdmissionScope scope) =>
        // rr:target.3: moving a counter onto the card it would leave changes nothing.
        CanMove(effect, scope) ? TargetLegality.Valid : TargetLegality.Invalid;

    /// <summary>Whether the placement has a card and places at least one counter.</summary>
    internal static bool CanPlace(AbilityCounterEffect.PlaceCounters effect, AbilityAdmissionScope scope) =>
        Find(effect.Card, scope) is not null && Amount(effect.Count, scope) > 0;

    internal static bool CanMove(AbilityCounterEffect.MoveCounters effect, AbilityAdmissionScope scope) =>
        Find(effect.From, scope) is { } from && Find(effect.To, scope) is { } to
        && from.ObjectId != to.ObjectId && from.Tokens.GetValueOrDefault("c_" + effect.Counter) > 0;
}
