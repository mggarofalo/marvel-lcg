using Marvel.Rules.State;

namespace Marvel.Rules.Play;

internal sealed record ResourceCostOrigin(Card? Card, int Incarnation, int Area, int Player)
{
    internal static ResourceCostOrigin For(Card? card, int player) =>
        new(card, card?.Incarnation ?? -1, card?.Area.Id ?? -1, player);

    internal bool Matches(Card card, int player) =>
        ReferenceEquals(Card, card) && card.Incarnation == Incarnation
        && card.Area.Id == Area && player == Player;
}
