using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Recognizes a threat removal that resolves on the live board after opaque defeat work.</summary>
/// <remarks>
/// rr:ability.4: "resolve the ability one sentence at a time." The removal chooses
/// nothing: its scheme is the retained chosen card, the power's targets or the main
/// scheme, and its amount is fixed or the excess damage that attack damage recorded
/// when it was dealt (ruling dd353e34). Any other selection or amount, including a
/// receipt no attack damage records, raises before the board changes.
/// </remarks>
internal static class AbilityLiveThreatRemoval
{
    internal static bool Resolves(AbilityEffect node) => node is AbilityEffect.RemoveThreat
    {
        OverridesCannotFrom: null,
        Schemes: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen }
            or AbilityCardSelection.Query { Kind: AbilityCardQuery.MainScheme or AbilityCardQuery.PowerTargets },
    } removal && removal.Amount switch
    {
        AbilityNumber.Constant => true,
        AbilityNumber.Result { Name: AbilityDamageCompletion.ExcessDealtResult } => true,
        _ => false,
    };
}
