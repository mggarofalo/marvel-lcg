using static Marvel.Cards.Run.AbilityAdmission;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;

namespace Marvel.Cards.Run;

/// <summary>Binds one physical status to the rules-owned replacement.</summary>
internal static class AbilityStatusReplacement
{
    internal static bool CanReplace(AbilityEffect.ReplaceStatus effect, AbilityAdmissionScope scope) =>
        AbilityInitiationPrimitives.Find(effect.Card, scope) is { } status
        && StatusReplacement.CanReplace(scope.World, status, effect.Status);

    internal static void Execute(AbilityEffect.ReplaceStatus effect, AbilityCardStateContext context)
    {
        if (AbilityCardStateExecution.Find(effect.Card, context) is { } status)
            StatusReplacement.Replace(context.World, status, effect.Status, context.Trigger, context.Events);
    }
}
