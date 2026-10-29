using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>Finds cards affected by the departure of continuous effects.</summary>
internal static class ContinuousEffectDepartureCandidates
{
    internal static Card[] LostUsesCandidates(this ContinuousEffects effects) => effects.world.Cards.Where(card =>
            DeckTypes.IsInPlay(card.Area.Type)
            && !EffectiveCards.HasProfile(card)
            && Characteristics.IsLost(effects.world, card, "uses")
            && effects.world.CounterPools.CounterPool(effects.world, card)?.Uses == true
            && card.Tokens
                .Where(pair => pair.Key.StartsWith("c_", StringComparison.Ordinal))
                .Sum(pair => pair.Value) == 0).ToArray();

    internal static Card[] RestoredUsesAfter(this ContinuousEffects effects, Card[] candidates)
    {
        var restored = candidates
            .Where(card => !Characteristics.IsLost(effects.world, card, "uses"))
            .ToArray();
        var restoredIds = restored.Select(card => card.ObjectId).ToHashSet();
        return restored.Where(card => !effects.HasHostedAncestor(card, restoredIds)).ToArray();
    }

    internal static Card[] LethalAfterDerivedChange(
        this ContinuousEffects effects, IReadOnlyDictionary<int, long> before) =>
        before.Where(pair => pair.Key >= 0 && pair.Key < effects.world.Cards.Count)
            .Select(pair => (Card: effects.world.Cards[pair.Key], Former: pair.Value))
            .Where(item => DeckTypes.IsInPlay(item.Card.Area.Type)
                && StateFields.Modified(effects.world, item.Card, "is_infinite_health",
                    effects.world.Facts, effects.world.Players) <= 0
                && item.Card.Damage < item.Former
                && Play.DamagePlacement.Health(effects.world, effects.world.Facts, item.Card) is long current
                && current < item.Former && item.Card.Damage >= current)
            .Select(item => item.Card).ToArray();

    internal static bool HasHostedAncestor(this ContinuousEffects effects, Card card, HashSet<int> candidates)
    {
        int host = card.Area.Host;
        var seen = new HashSet<int> { card.ObjectId };
        bool candidateAncestor = false;
        while (host >= 0)
        {
            if (!seen.Add(host))
            {
                throw new RulesNotImplementedException(
                    $"attachment {host} forms a hosting cycle");
            }
            if (candidates.Contains(host))
            {
                candidateAncestor = true;
            }
            host = host < effects.world.Cards.Count ? effects.world.Cards[host].Area.Host : -1;
        }
        return candidateAncestor;
    }
}
