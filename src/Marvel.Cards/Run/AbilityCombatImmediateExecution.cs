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
            case AbilityFixedInstruction.UseAttackForThwart:
                ThwartStatSubstitution.UseAttack(world, context.Admission.Query.Occurrence);
                return new(true, true);
            case AbilityFixedInstruction.AlsoAttackEachOtherHero:
                Attack.AlsoResolveAgainstEachOtherHero(world);
                return new(true, false);
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
