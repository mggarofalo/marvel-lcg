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
        bool written = program.On(scheme.FaceId).Any(ability =>
            ability.Trigger.Timing == AbilityType.WhenCompleted
            && ability.Trigger.Event == MainSchemeCompletionOperation.Completed);
        if (!written && world.Facts.HasWhenCompleted(scheme.FaceId))
            throw new RulesNotImplementedException(
                $"card '{scheme.FaceId}' has When Completed text with no executable completion ability");
        return written;
    }
}
