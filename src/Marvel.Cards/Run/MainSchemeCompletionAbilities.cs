using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>Requires executable completion text before the scheme can complete.</summary>
internal static class MainSchemeCompletionAbilities
{
    internal static bool Observes(AbilityProgram program, World world, Card scheme)
    {
        if (KangLocalCompletion.IsLocal(scheme))
        {
            bool response = program.On(scheme.FaceId).Any(ability =>
                ability.Trigger.Timing == AbilityType.ForcedResponse
                && ability.Trigger.Event == MainSchemeCompletionOperation.Completed
                && ability.Effect is AbilityEffect.Fixed
                    { Instruction: AbilityFixedInstruction.CaptureKangLocalCompletion });
            if (!response) throw new RulesNotImplementedException(
                $"Kang local main scheme {scheme.FaceId} completion requires Kang's Dominion and phase-end reunion");
            return true;
        }
        bool written = program.On(scheme.FaceId).Any(ability =>
            ability.Trigger.Timing == AbilityType.WhenCompleted
            && ability.Trigger.Event == MainSchemeCompletionOperation.Completed);
        if (!written && world.Facts.HasWhenCompleted(scheme.FaceId))
            throw new RulesNotImplementedException(
                $"card '{scheme.FaceId}' has When Completed text with no executable completion ability");
        return written;
    }
}
