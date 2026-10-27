using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Recognizes a retained enemy attack followed by fixed damage to that enemy.</summary>
internal static class AbilityPowerActivationSupport
{
    internal static bool SupportsBody(AbilityEffect effect) => effect is AbilityEffect.Sequence
    {
        Effects: [AbilityEffect.ActivateEnemies
        {
            Attack: true, Enemies: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
            Against: null, EngagedHero: false, Dynamic: false, Modifier: null,
        }, AbilityEffect.AttackDamage
        {
            Cards: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
            Amount: AbilityNumber.Constant,
        }],
    };
}
