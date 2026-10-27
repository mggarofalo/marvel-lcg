using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Dispatches authored Kang topology instructions to their scenario owner.</summary>
internal static class AbilityKangImmediateExecution
{
    internal static AbilityImmediateResult? TryRun(AbilityEffect.Fixed effect, AbilityImmediateContext context)
    {
        var split = context.Admission.World.Agenda.KangSplit;
        switch (effect.Instruction)
        {
            case AbilityFixedInstruction.CaptureKangStageOneDefeat:
                split.Capture(context.Admission.World, context.Admission.Source);
                return new(true, true);
            case AbilityFixedInstruction.SplitKangStageTwo:
                split.SelectStages(context.Admission.World, context.Admission.Source, context.Events);
                return new(true, true);
            case AbilityFixedInstruction.CreateKangGameArea:
                split.CreateLocalArea(context.Admission.World, context.Admission.Source,
                    context.Admission.Query.Player, context.Events);
                return new(true, true);
            default:
                return null;
        }
    }
}
