using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Recognizes independent boost cancellation within one labeled attack.</summary>
internal static class AbilityIndependentAttack
{
    // Ruling 0fc2a7cc9197e127 and rr:target.3: cancellation can affect the
    // boost card even when Guard excludes the villain. The closed body keeps
    // both sentences under one label and never substitutes a chosen enemy.
    internal static bool HasIndependentCancellation(AbilityEffect.Power power) => power is
    {
        Kind: AbilityPowerKind.Attack, AutomaticTarget: false, TargetCount: null,
        Target: AbilityCardSelection.Query { Kind: AbilityCardQuery.Villain },
        Effect: AbilityEffect.Dependent
        {
            OnFull: true,
            Effect: AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.CancelBoostIcons },
            Continuation: AbilityEffect.AttackDamage
            {
                Cards: AbilityCardSelection.Bound { Binding: AbilityCardBinding.Chosen },
                Amount: AbilityNumber.Result { Name: "cancelledBoostIcons" },
                Overkill: false, SimultaneousPlacement: false,
            },
        },
    };

    internal static bool CanAffectBoost(AbilityEffect.Power power, AbilityAdmissionContext context) =>
        HasIndependentCancellation(power)
        && context.Expressions.Occurrence.BoostIcons is { Remaining: > 0 } icons
        && icons.IsCurrent(context.World);

    internal static bool CanAttack(World world, int player, Card source, Card enemy) =>
        AbilityAttackTargets.For(world, player, source)
            .Any(candidate => candidate.ObjectId == enemy.ObjectId)
        && world.DamageAbilities.CanTakeDamage(world, enemy, source);
}
