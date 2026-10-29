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
        return Find(effect.Card, scope) is not null && AbilityFutureDiscardAdmission.MayHavePositiveAmount(effect.Count, scope)
            ? TargetLegality.Valid : TargetLegality.Invalid;
    }
}
