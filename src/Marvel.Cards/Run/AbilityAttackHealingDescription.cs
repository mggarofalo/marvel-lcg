using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Describes healing instructed by the settled triggering attack receipt.</summary>
internal static class AbilityAttackHealingDescription
{
    internal static string? Summary(AbilityEffect effect, Attacked? attack) => effect switch
    {
        AbilityEffect.Heal
        {
            Card: AbilityCardSelection.Bound { Binding: AbilityCardBinding.You },
            Amount: AbilityNumber.ResolutionValue { Kind: AbilityResolutionNumber.AttackExcessDamage },
        } when attack is not null =>
            $"Heal up to {attack.ExcessDamage} damage from your identity. "
            + "The amount healed is limited by damage present; intervening effects can change the result.",
        AbilityEffect.Sequence { Effects.Length: 1 } sequence => Summary(sequence.Effects[0], attack),
        _ => null,
    };
}
