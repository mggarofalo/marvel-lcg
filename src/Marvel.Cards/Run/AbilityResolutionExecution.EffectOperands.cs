using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

internal sealed partial class AbilityResolutionExecution
{
    private static T EffectOf<T>(AbilityEffect node, AbilityResolutionState cast) where T : AbilityEffect =>
        (T)node;

}
