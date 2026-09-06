using Marvel.Cards.Dsl;
using Marvel.Rules.Prompts;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Engine-owned card choices for a compiled arrow cost.</summary>
internal static class AbilityCostSelection
{
    // rr:initiating-abilities keeps choosing and paying separate. Resource
    // selections travel in Decision.Resources; this describes the cards a
    // discard or exhaust cost asks the payer to choose.
    internal static TargetRequest? Ask(World world, int player, AbilityCost? cost)
    {
        if (cost is AbilityCost.Sequence sequence)
        {
            return sequence.Costs.Select(step => Ask(world, player, step))
                .SingleOrDefault(request => request is not null);
        }
        if (cost is AbilityCost.DiscardFromHand discard)
        {
            var hand = world.Seats[player].Hand.Cards;
            var range = Range(discard.Range, hand.Count);
            return new TargetRequest([.. hand.Select(card => card.ObjectId)], range.Min, range.Max);
        }
        if (cost is AbilityCost.ExhaustChosen exhaust)
        {
            var cards = Choices(world, player, exhaust.From).Where(card => card.Ready).ToList();
            var range = Range(exhaust.Range, cards.Count);
            return new TargetRequest([.. cards.Select(card => card.ObjectId)], range.Min, range.Max);
        }
        return null;
    }

    internal static IReadOnlyList<Card> Choices(World world, int player, AbilityCardQuery relation) => relation switch
    {
        AbilityCardQuery.HeroesAndAllies =>
        [
            .. world.PlayerOrder
                .Where(seat => Forms.In(world, world.Seats[seat], world.Facts, Forms.Hero))
                .Select(seat => world.Seats[seat].IdentityCard),
            .. world.Areas.Where(area => area.Type == DeckType.AlliesArea).SelectMany(area => area.Cards),
        ],
        AbilityCardQuery.CharactersYouControl =>
        [
            world.Seats[player].IdentityCard,
            .. world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player)).Cards,
        ],
        AbilityCardQuery.AlliesYouControl => [.. world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player)).Cards],
        _ => throw new InvalidOperationException($"'{relation}' is not a compiled cost-selection relation"),
    };

    internal static (int Min, int Max) Range(AbilityCostRange range, int available) => range switch
    {
        AbilityCostRange.Exact exact => (exact.Count, exact.Count),
        AbilityCostRange.UpTo upTo => (1, Math.Min(upTo.Count, available)),
        AbilityCostRange.Any => (1, available),
        _ => throw new InvalidOperationException("Unknown compiled cost range"),
    };
    /// <summary>Resolves the physical counter removed by a cost.</summary>
    /// <remarks>
    /// If more than one typed pool is present, the rule permits the player to
    /// choose either one. The current action protocol has no counter-choice
    /// affordance, so resolution raises before changing state rather than
    /// choosing an outcome on the player's behalf.
    /// </remarks>
    internal static string? CounterKeyForRemoval(Card card, string type, long count)
    {
        if (!string.Equals(type, "allPurpose", StringComparison.Ordinal))
        {
            string typed = "c_" + type;
            return card.Tokens.GetValueOrDefault(typed) >= count ? typed : null;
        }

        string[] pools = [.. card.Tokens
            .Where(pair => pair.Value > 0
                && pair.Key.StartsWith("c_", StringComparison.Ordinal))
            .Select(pair => pair.Key)
            .Order(StringComparer.Ordinal)];
        return pools.Length switch
        {
            0 => null,
            1 when card.Tokens[pools[0]] >= count => pools[0],
            1 => null,
            _ => throw new RulesNotImplementedException(
                $"'{card.FaceId}' must choose which all-purpose counter to remove"),
        };
    }

}
