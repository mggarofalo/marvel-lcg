using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Captured quantities for read-only queries at one projected payment step.</summary>
internal sealed record AbilityQueryProjection(
    ImmutableDictionary<int, CardInstanceState> Cards)
{
    internal CardInstanceState Quantities(Card card) => Cards.GetValueOrDefault(card.ObjectId)
        ?? card.InstanceState;

    internal AbilityQueryProjection WithDamage(Card card, long damage) => new(Cards.SetItem(
        card.ObjectId, CardInstanceState.FromFacts(damage, Quantities(card).Ready, Quantities(card).Tokens)));

    internal AbilityQueryProjection WithReady(Card card, bool ready) => new(Cards.SetItem(
        card.ObjectId, CardInstanceState.FromFacts(Quantities(card).Damage, ready, Quantities(card).Tokens)));

    internal AbilityQueryProjection WithCounters(Card card, string key, long count)
    {
        var quantities = Quantities(card);
        var tokens = quantities.Tokens.ToImmutableDictionary(StringComparer.Ordinal).SetItem(key, count);
        return new(Cards.SetItem(card.ObjectId,
            CardInstanceState.FromFacts(quantities.Damage, quantities.Ready, tokens)));
    }
}
