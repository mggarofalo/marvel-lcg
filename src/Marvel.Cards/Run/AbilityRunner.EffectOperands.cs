using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    private static AbilityEffect.Conditional ConditionalOf(AbilityEffect node, Cast cast) =>
        (AbilityEffect.Conditional)node;

    private static T EffectOf<T>(AbilityEffect node, Cast cast) where T : AbilityEffect =>
        (T)node;

    private static AbilityNumber DamageAmountOf(AbilityEffect node, Cast cast) =>
        EffectOf<AbilityEffect>(node, cast) switch
        {
            AbilityEffect.Damage damage => damage.Amount,
            AbilityEffect.AttackDamage damage => damage.Amount,
            AbilityEffect.IndirectDamage damage => damage.Amount,
            _ => throw new InvalidOperationException("Expected a compiled damage instruction"),
        };

    private static AbilityCardSelection DamageSelectionOf(AbilityEffect node, Cast cast) =>
        EffectOf<AbilityEffect>(node, cast) switch
        {
            AbilityEffect.Damage damage => damage.Cards,
            AbilityEffect.AttackDamage damage => damage.Cards,
            AbilityEffect.IndirectDamage damage => damage.Among,
            _ => throw new InvalidOperationException("Expected a compiled damage instruction"),
        };

    private static AbilityCardSelection GrantSelectionOf(AbilityEffect node, Cast cast) =>
        EffectOf<AbilityEffect>(node, cast) switch
        {
            AbilityEffect.GrantField grant => grant.Cards,
            AbilityEffect.GrantTrait grant => grant.Cards,
            _ => throw new InvalidOperationException("Expected a compiled grant instruction"),
        };

    private static AbilityCardSelection ThreatSelectionOf(AbilityEffect node, Cast cast) =>
        EffectOf<AbilityEffect>(node, cast) switch
        {
            AbilityEffect.PlaceThreat threat => threat.Schemes,
            AbilityEffect.RemoveThreat threat => threat.Schemes,
            _ => throw new InvalidOperationException("Expected a compiled threat instruction"),
        };
}
