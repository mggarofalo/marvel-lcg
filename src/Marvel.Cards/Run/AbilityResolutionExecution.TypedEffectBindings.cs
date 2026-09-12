using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

internal static class AbilityResolutionEffectBindings
{
    // rr:ability.8.1: only the attached player card's controller can trigger
    // an attachment ability that "uses the word “you” or “your”". The engine
    // represents that text with explicit bindings; literal names and display
    // descriptions are not bindings and cannot change permission.
    internal static bool ContainsYouOrYour(this AbilityResolutionExecution execution, AbilityEffect? effect) =>
        AbilityPlayerBindingAnalysis.Contains(effect);

    internal static bool ContainsYouOrYour(this AbilityResolutionExecution execution, AbilityPlayerSelection players) =>
        AbilityPlayerBindingAnalysis.Contains(players);
}
