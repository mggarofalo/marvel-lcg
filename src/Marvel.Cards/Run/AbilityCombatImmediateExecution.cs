using Marvel.Cards.Dsl;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

internal static class AbilityCombatImmediateExecution
{
    internal static AbilityImmediateResult Run(AbilityEffect.Fixed effect, AbilityImmediateContext context)
    {
        var world = context.Admission.World;
        switch (effect.Instruction)
        {
            case AbilityFixedInstruction.ReadyUndamagedDefender:
                UndamagedDefenseReady.Install(world, context.Admission.Query.Occurrence,
                    context.Admission.Source, context.Admission.Query.Player);
                return new(true, true);
            case AbilityFixedInstruction.RequireAttackedPlayerAllyDefender:
                AttackAllyDefenseRequirement.Install(world, context.Admission.Source);
                return new(true, true);
            case AbilityFixedInstruction.UseAttackForThwart:
                ThwartStatSubstitution.UseAttack(world, context.Admission.Query.Occurrence);
                return new(true, true);
            case AbilityFixedInstruction.AlsoAttackEachOtherHero:
                Attack.AlsoResolveAgainstEachOtherHero(world);
                return new(true, false);
            case AbilityFixedInstruction.ReflectAttackDamage:
                PlayerAttackReflection.Install(world, context.Admission.Source, context.Trigger);
                return new(true, true);
            case AbilityFixedInstruction.MakeAttackIndirect:
                Attack.MakeIndirect(world);
                return new(true, false);
            case AbilityFixedInstruction.PreventAttackDamage:
                Attack.PreventDamage(world);
                return new(true, true);
            default: return new(false, false);
        }
    }
}
