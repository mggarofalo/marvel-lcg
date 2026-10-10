using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Describes an offered player's phase-long character modifiers.</summary>
internal static class AbilityPlayerModifierDescription
{
    internal static string? Selection(AbilityEffect.ChooseCard choice) => Modifier(choice) is { } modifier
        ? $"choose a player whose characters get {modifier} until the end of the player phase"
        : null;

    internal static string? Commitment(AbilityStructuralContext context,
        AbilityEffect.ChooseCard choice, Card candidate) => Modifier(choice) is { } modifier
        ? $"Give {AbilityPlayerChoiceDescription.Recipient(context, candidate)}'s characters {modifier}"
        : null;

    internal static string? Description(AbilityStructuralContext context,
        AbilityEffect.ChooseCard choice, Card candidate) => Commitment(context, choice, candidate) is { } commitment
        ? $"{commitment} until the end of the player phase. Includes characters they control later in this phase."
        : null;

    private static string? Modifier(AbilityEffect.ChooseCard choice)
    {
        // Describe checked instructions only; do not evaluate a variable or guess a duration.
        if (choice is not
            { From: AbilityCardSelection.Query { Kind: AbilityCardQuery.Identities },
              Effect: AbilityEffect.GrantControlledCharacters
              { Player: AbilityPlayer.ChosenPlayer, Amount: AbilityNumber.Constant { Value: > 0 } amount,
                Until: "EndOfPlayerPhase" } grant }
            || grant.Fields.Length == 0
            || grant.Fields.Any(field => field is not ("attack" or "thwart"))) return null;
        return string.Join(" and ", grant.Fields.Select(field =>
            $"+{amount.Value} {(field == "attack" ? "ATK" : "THW")}"));
    }
}
