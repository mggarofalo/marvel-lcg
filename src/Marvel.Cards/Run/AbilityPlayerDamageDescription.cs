using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Explains an offered player selection for public villain and engaged-enemy damage.</summary>
internal static class AbilityPlayerDamageDescription
{
    internal static string? Selection(AbilityEffect.ChooseCard choice) => Amounts(choice) is not null
        ? "choose a player for damage to the villain and their engaged enemies" : null;

    internal static string? Commitment(
        AbilityStructuralContext context, AbilityEffect.ChooseCard choice, Card candidate) =>
        Amounts(choice) is not null
            ? $"Damage villain and enemies engaged with {AbilityPlayerChoiceDescription.Recipient(context, candidate)}"
            : null;

    internal static string? Description(
        AbilityStructuralContext context, AbilityEffect.ChooseCard choice, Card candidate)
    {
        if (Amounts(choice) is not { } amounts) return null;
        long villain = AbilityStructuralQueries.Amount(amounts.Villain, context.Expressions);
        long enemies = AbilityStructuralQueries.Amount(amounts.Enemies, context.Expressions);
        string player = AbilityPlayerChoiceDescription.Recipient(context, candidate);
        return $"Deal {villain} damage to the villain and {enemies} to each enemy engaged with {player}. "
            + "Modifiers, prevention and later effects can change the damage.";
    }

    private static (AbilityNumber Villain, AbilityNumber Enemies)? Amounts(AbilityEffect.ChooseCard choice)
    {
        if (choice.From is not AbilityCardSelection.Query { Kind: AbilityCardQuery.Identities }
            || choice.Effect is not AbilityEffect.Sequence sequence) return null;
        if (sequence.Effects is [
            AbilityEffect.Damage { Cards: AbilityCardSelection.Query { Kind: AbilityCardQuery.Villain } } villain,
            AbilityEffect.Damage { Cards: AbilityCardSelection.Query
                { Kind: AbilityCardQuery.EnemiesEngagedWithChosenPlayer } } enemies]
            && AbilityPublicAmounts.IsCurrentStep(villain.Amount) && AbilityPublicAmounts.IsCurrentStep(enemies.Amount))
            return (villain.Amount, enemies.Amount);
        return null;
    }

}
