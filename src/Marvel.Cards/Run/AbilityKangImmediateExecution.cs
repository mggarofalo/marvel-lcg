using Marvel.Cards.Dsl;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

/// <summary>Dispatches authored Kang topology instructions to their scenario owner.</summary>
internal static class AbilityKangImmediateExecution
{
    internal static AbilityImmediateResult? TryRun(AbilityEffect.Fixed effect, AbilityImmediateContext context)
    {
        var split = context.Admission.World.Agenda.KangSplit;
        switch (effect.Instruction)
        {
            case AbilityFixedInstruction.CaptureKangLocalCompletion:
                KangLocalCompletion.Capture(context.Admission.World, context.Admission.Source,
                    context.Admission.Query.Occurrence, context.Events);
                return new(true, true);
            case AbilityFixedInstruction.CaptureKangStageTwoDefeat:
                context.Admission.World.Agenda.KangTransitions.CaptureDefeat(context.Admission.World,
                    context.Admission.Source, context.Admission.Query.Defeat, context.Events);
                return new(true, true);
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
            case AbilityFixedInstruction.RevealKangStageThree:
            case AbilityFixedInstruction.PutKangNemesesIntoPlay:
                KangCentral.Schedule(context.Admission.World, context.Admission.Source,
                    effect.Instruction == AbilityFixedInstruction.RevealKangStageThree
                        ? KangCentralPart.RevealVillain : KangCentralPart.Nemeses);
                return new(true, true) { ProcedureScheduled = true };
            default:
                return null;
        }
    }
}
