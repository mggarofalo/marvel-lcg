using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Describes an identity standing for the recipient of a public player effect.</summary>
internal static class AbilityPlayerChoiceDescription
{
    internal static int? DrawCount(AbilityEffect.ChooseCard choice) => choice is
        { From: AbilityCardSelection.Query { Kind: AbilityCardQuery.Identities },
          Effect: AbilityEffect.Draw { Players: AbilityPlayerSelection.OnePlayer
              { Player: AbilityPlayer.ChosenPlayer } } draw } ? draw.Count : null;

    internal static string? Selection(AbilityEffect.ChooseCard choice) =>
        DrawCount(choice) is { } count ? $"choose a player to draw {Cards(count)}"
        : Discount(choice) is { } amount
            ? $"choose a player whose next card this phase costs {amount} fewer resources"
            : AbilityPlayerModifierDescription.Selection(choice)
                ?? AbilityPlayerDamageDescription.Selection(choice);

    internal static string? Commitment(
        AbilityStructuralContext context, AbilityEffect.ChooseCard choice, Card candidate)
    {
        if (AbilityPlayerDamageDescription.Commitment(context, choice, candidate) is { } damage)
            return damage;
        if (AbilityPlayerModifierDescription.Commitment(context, choice, candidate) is { } modifier)
            return modifier;
        if (Selection(choice) is null) return null;
        string player = Recipient(context, candidate);
        return DrawCount(choice) is { } count ? $"{player} draws {Cards(count)}"
            : $"{player}: next card −{Discount(choice)}";
    }

    internal static string? Description(
        AbilityStructuralContext context, AbilityEffect.ChooseCard choice, Card candidate) =>
        AbilityPlayerModifierDescription.Description(context, choice, candidate)
        ?? AbilityPlayerDamageDescription.Description(context, choice, candidate)
        ?? (Commitment(context, choice, candidate) is { } commitment
            ? DrawCount(choice) is not null
                ? $"{commitment}. The drawn card's identity is not known before the draw."
                : $"{commitment} resource cost this phase. Applies once, when that player plays their next card."
            : null);

    internal static string Recipient(AbilityStructuralContext context, Card candidate)
    {
        var expressions = context.Admission().WithSelection(candidate).Expressions;
        var evaluation = new AbilityExpressionEvaluation(expressions,
            new AbilitySelectorEvaluation(expressions.Bindings));
        int recipient = evaluation.Seat(AbilityPlayer.ChosenPlayer);
        return expressions.World.Seats[recipient].Name;
    }

    private static long? Discount(AbilityEffect.ChooseCard choice) => choice is
        { From: AbilityCardSelection.Query { Kind: AbilityCardQuery.Identities },
          Effect: AbilityEffect.ReduceNextCardCost { Player: AbilityPlayer.ChosenPlayer,
              Amount: AbilityNumber.Constant amount } } ? amount.Value : null;

    private static string Cards(int count) => $"{count} card{(count == 1 ? "" : "s")}";
}
