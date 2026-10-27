using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Explains an offered player selection for public villain and engaged-enemy damage.</summary>
internal static class AbilityPlayerDamageDescription
{
    internal static string? Selection(AbilityEffect.ChooseCard choice) => Amounts(choice) is { } amounts
        ? amounts.Villain.Maximum is null && amounts.Enemies.Maximum is null
            ? "choose a player for damage to the villain and their engaged enemies"
            : "choose a player for damage with a later bounded recipient choice" : null;

    internal static string? Commitment(
        AbilityStructuralContext context, AbilityEffect.ChooseCard choice, Card candidate) =>
        Amounts(choice) is { } amounts
            ? amounts.Villain.Maximum is null && amounts.Enemies.Maximum is null
                ? $"Damage villain and enemies engaged with {AbilityPlayerChoiceDescription.Recipient(context, candidate)}"
                : $"Select {AbilityPlayerChoiceDescription.Recipient(context, candidate)}; choose damage recipients later"
            : null;

    internal static string? Description(
        AbilityStructuralContext context, AbilityEffect.ChooseCard choice, Card candidate)
    {
        if (Amounts(choice) is not { } amounts) return null;
        long villain = AbilityStructuralQueries.Amount(amounts.Villain.Amount, context.Expressions);
        long enemies = AbilityStructuralQueries.Amount(amounts.Enemies.Amount, context.Expressions);
        string player = AbilityPlayerChoiceDescription.Recipient(context, candidate);
        if (amounts.Villain.Maximum is null && amounts.Enemies.Maximum is null)
            return $"Deal {villain} damage to the villain and {enemies} to each enemy engaged with {player}. "
                + "Modifiers, prevention and later effects can change the damage.";
        string villainEffect = amounts.Villain.Maximum is null
            ? $"Deal {villain} damage to the villain. "
            : $"Later, choose whether to deal {villain} damage to the villain. ";
        string enemyEffect = amounts.Enemies.Maximum is { } maximum
            ? $"Then choose up to {maximum} different enemies engaged with {player}; deal {enemies} damage to each chosen enemy. "
            : $"Deal {enemies} damage to each enemy engaged with {player}. ";
        return villainEffect + enemyEffect
            + "Choosing none in a later selection deals no damage to that group. "
            + "Modifiers, prevention and later effects can change the damage.";
    }

    private static (AbilityEffect.Damage Villain, AbilityEffect.Damage Enemies)? Amounts(AbilityEffect.ChooseCard choice)
    {
        if (choice.From is not AbilityCardSelection.Query { Kind: AbilityCardQuery.Identities }
            || choice.Effect is not AbilityEffect.Sequence sequence) return null;
        if (sequence.Effects is [
            AbilityEffect.Damage { Cards: AbilityCardSelection.Query { Kind: AbilityCardQuery.Villain } } villain,
            AbilityEffect.Damage { Cards: AbilityCardSelection.Query
                { Kind: AbilityCardQuery.EnemiesEngagedWithChosenPlayer } } enemies]
            && AbilityPublicAmounts.IsCurrentStep(villain.Amount) && AbilityPublicAmounts.IsCurrentStep(enemies.Amount))
            return (villain, enemies);
        return null;
    }

}
