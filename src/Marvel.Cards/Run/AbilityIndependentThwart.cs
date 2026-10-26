using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Recognizes a scheme choice guarded by the same scheme availability query.</summary>
internal static class AbilityIndependentThwart
{
    // Each branch is structurally preflighted by the caller. Current target
    // eligibility belongs to the taken branch: its guard supplies a scheme,
    // while the other branch affects the villain independently (rr:target.3).
    internal static bool IsSchemeAlternative(AbilityEffect.Conditional conditional) =>
        conditional.Test is AbilityCondition.Exists
            { Cards: AbilityCardSelection.Query { Kind: AbilityCardQuery.ThwartableSchemes } }
        && conditional.Then is AbilityEffect.ChooseCard
        {
            From: AbilityCardSelection.Query { Kind: AbilityCardQuery.ThwartableSchemes },
            Effect: AbilityEffect.Power
            {
                Kind: AbilityPowerKind.Thwart,
                Target: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
                AutomaticTarget: false, TargetCount: null,
                Effect: AbilityEffect.Sequence { Effects.Length: 2 } body,
            },
        }
        && body.Effects[0] is AbilityEffect.RemoveThreat
            { Schemes: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen } }
        && body.Effects[1] is AbilityEffect.GiveStatus status
        && conditional.Else is AbilityEffect.Power
            { Kind: AbilityPowerKind.Thwart, Target: null, AutomaticTarget: false,
              TargetCount: null, Effect: AbilityEffect.GiveStatus independent }
        && status == independent;
}
