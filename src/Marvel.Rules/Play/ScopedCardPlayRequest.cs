using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>A single permanent play and its effect-supplied price adjustment.</summary>
public sealed record ScopedCardPlayRequest(
    ICardFacts Facts, ICardPlayAbilities Abilities, Seat Seat, Card Card, Decision Choice,
    ScopedCardCostReduction? Reduction = null);
