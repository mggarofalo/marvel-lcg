using Marvel.Cards.Dsl;
using static Marvel.Cards.Run.AbilityAdmission;

namespace Marvel.Cards.Run;

/// <summary>Recognizes an independent threat-removal sentence with no legal target.</summary>
internal static class AbilityIndependentCardChoice
{
    // ruling:8883bc6b0af1c9b8: the choose sentence "doesn't limit the entire
    // ability, only that sentence". This admission shape supports a direct
    // scheme choice; nested choices and dependent effects keep their own gates.
    internal static bool IsEmptyThreatChoice(AbilityEffect effect, AbilityAdmissionScope scope) =>
        effect is AbilityEffect.ChooseCard
        {
            From: AbilityCardSelection.Query { Kind: AbilityCardQuery.Schemes },
            Effect: AbilityEffect.RemoveThreat
            {
                Schemes: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
            },
        }
        && LegalCardChoices(effect, scope).Count == 0;
}
