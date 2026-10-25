using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

// A projection between completed effects has no enemy attack to which a
// source-only attack constant can apply. Active attack contexts remain unknown.
internal static class AbilityInactiveAttackConstant
{
    internal static bool IsInactive(CompiledCardAbility ability, World world) =>
        world.Attack is null
        && ability.When is null
        && ability.Effect is AbilityEffect.Conditional { Else: null } conditional
        && RequiresSourceAttack(conditional.Test);

    private static bool RequiresSourceAttack(AbilityCondition condition) => condition switch
    {
        AbilityCondition.IsEnemyAttacker
        {
            Card: AbilityCardSelection.Bound { Binding: AbilityCardBinding.This },
        } => true,
        AbilityCondition.All all => !all.Operands.IsEmpty && RequiresSourceAttack(all.Operands[0]),
        _ => false,
    };
}
