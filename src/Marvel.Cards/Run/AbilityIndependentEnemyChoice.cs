using Marvel.Cards.Dsl;
using static Marvel.Cards.Run.AbilityAdmission;

namespace Marvel.Cards.Run;

/// <summary>Recognizes an independent enemy-status and nonattack-damage sentence.</summary>
internal static class AbilityIndependentEnemyChoice
{
    // rr:ability.4: "resolve the ability one sentence at a time";
    // ruling:8883bc6b0af1c9b8: choosing limits "only that sentence".
    // This bounded composition leaves whole-ability target/effect admission intact.
    internal static bool IsEmpty(AbilityEffect effect, AbilityAdmissionScope scope) =>
        effect is AbilityEffect.ChooseCard
        {
            From: AbilityCardSelection.Query { Kind: AbilityCardQuery.Enemies },
            Effect: AbilityEffect.Sequence { Effects: [
                AbilityEffect.GiveStatus
                {
                    Cards: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
                },
                AbilityEffect.Damage
                {
                    Cards: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
                    AttackVerb: false,
                    IgnoresTough: false,
                    Amount: AbilityNumber.Constant { Value: > 0 },
                },
            ] },
        }
        && LegalCardChoices(effect, scope).Count == 0;
}
