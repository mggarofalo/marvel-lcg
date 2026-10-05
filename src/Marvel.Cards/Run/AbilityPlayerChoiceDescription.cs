using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Describes an identity standing for the recipient of a chosen-player draw.</summary>
internal static class AbilityPlayerChoiceDescription
{
    internal static int? DrawCount(AbilityEffect.ChooseCard choice) => choice is
        { From: AbilityCardSelection.Query { Kind: AbilityCardQuery.Identities },
          Effect: AbilityEffect.Draw { Players: AbilityPlayerSelection.OnePlayer
              { Player: AbilityPlayer.ChosenPlayer } } draw } ? draw.Count : null;

    internal static string? Selection(AbilityEffect.ChooseCard choice) =>
        DrawCount(choice) is { } count ? $"choose a player to draw {Cards(count)}" : null;

    internal static string? Commitment(
        AbilityStructuralContext context, AbilityEffect.ChooseCard choice, Card candidate)
    {
        if (DrawCount(choice) is not { } count) return null;
        var expressions = context.Admission().WithSelection(candidate).Expressions;
        var evaluation = new AbilityExpressionEvaluation(expressions,
            new AbilitySelectorEvaluation(expressions.Bindings));
        int recipient = evaluation.Seat(AbilityPlayer.ChosenPlayer);
        return $"{expressions.World.Seats[recipient].Name} draws {Cards(count)}";
    }

    internal static string? Description(
        AbilityStructuralContext context, AbilityEffect.ChooseCard choice, Card candidate) =>
        Commitment(context, choice, candidate) is { } commitment
            ? $"{commitment}. The drawn card's identity is not known before the draw." : null;

    private static string Cards(int count) => $"{count} card{(count == 1 ? "" : "s")}";
}
