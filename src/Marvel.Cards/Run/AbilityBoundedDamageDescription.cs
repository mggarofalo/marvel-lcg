using Marvel.Cards.Dsl;

namespace Marvel.Cards.Run;

/// <summary>Explains a later recipient choice without committing damage to its candidates.</summary>
internal static class AbilityBoundedDamageDescription
{
    internal static string Label(int maximum) => $"Choose up to {maximum} damage recipients";

    // Bounded ordinary damage admits only positive constant amounts.
    internal static string Instruction(AbilityNumber amount, int maximum) =>
        $"After this choice, choose up to {maximum} different damage recipients. "
        + $"Deal {((AbilityNumber.Constant)amount).Value} printed damage to each chosen recipient; choosing none deals no damage. "
        + "Modifiers, prevention and later effects can change the damage.";
}
