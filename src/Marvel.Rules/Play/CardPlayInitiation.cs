using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Accepted card-play choices and the cost snapshot retained through payment.</summary>
internal sealed record CardPlayInitiation(
    ICardFacts Facts, ICardPlayAbilities Abilities, Seat Seat, Card Card,
    AdjustedCardCost Cost, IReadOnlyList<int> Targets, IReadOnlyList<int> Paying);
