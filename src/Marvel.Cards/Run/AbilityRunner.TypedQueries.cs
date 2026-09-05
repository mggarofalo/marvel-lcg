using Marvel.Cards.Dsl;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    private static IReadOnlyList<Card> QueryCards(AbilityCardQuery query, Cast cast) =>
        AbilityCardQueries.Cards(query, cast.QueryContext());
}
