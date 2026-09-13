using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal sealed record AbilityCardStateContext(AbilityExpressionContext Expressions, string Trigger,
    List<GameEvent> Events, ICardPlayAbilities CardPlayAbilities,
    ICardReadinessAbilities Readiness,
    AbilityCardStateResult Result)
{
    internal World World => Expressions.World;
    internal Card Source => Expressions.Source;
    internal int Player => Expressions.Player;
}
