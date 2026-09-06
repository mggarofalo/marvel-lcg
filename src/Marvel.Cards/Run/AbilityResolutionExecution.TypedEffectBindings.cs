using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

internal sealed partial class AbilityResolutionExecution
{
    // rr:ability.8.1: only the attached player card's controller can trigger
    // an attachment ability that "uses the word “you” or “your”". The engine
    // represents that text with explicit bindings; literal names and display
    // descriptions are not bindings and cannot change permission.
    private static bool ContainsYouOrYour(AbilityEffect? effect) =>
        AbilityPlayerBindingAnalysis.Contains(effect);

    private static bool ContainsYouOrYour(AbilityPlayerSelection players) =>
        AbilityPlayerBindingAnalysis.Contains(players);
}
