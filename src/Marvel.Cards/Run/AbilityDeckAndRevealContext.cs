using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal sealed record AbilityDeckAndRevealContext(
    AbilityExpressionContext Expressions, string Trigger, List<GameEvent> Events,
    ICardPlayAbilities CardPlayAbilities, ICardReadinessAbilities Readiness,
    ImmutableArray<Card> Discarded)
{
    internal World World => Expressions.World;
    internal Card Source => Expressions.Source;
    internal int Player => Expressions.Player;
}
