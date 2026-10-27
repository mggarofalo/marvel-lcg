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
    internal static TargetRequest? Ask(World world, Card source, int player, AbilityCost? cost)
    {
        if (cost is AbilityCost.ChooseScheme)
            return new TargetRequest([.. AbilitySchemeChoice.Candidates(world, source, player).Select(card => card.ObjectId)], 1, 1);
        if (cost is AbilityCost.InvocationTop)
            return new TargetRequest(AbilityInvocationPayment.Top(world, player) is { } top ? [top.ObjectId] : [], 1, 1);
        if (cost is AbilityCost.Sequence sequence)
        {
            return sequence.Costs.Select(step => Ask(world, source, player, step))
                .SingleOrDefault(request => request is not null);
        }
        if (cost is AbilityCost.ChooseFromDiscard choice)
            return new TargetRequest([.. DiscardCandidates(world, player, choice).Select(card => card.ObjectId)], 1, 1);
        if (cost is AbilityCost.DiscardFromHand discard)
        {
            var hand = HandCandidates(world, source, player);
            var range = Range(discard.Range, hand.Count);
            return new TargetRequest([.. hand.Select(card => card.ObjectId)], range.Min, range.Max);
        }
        return cost is AbilityCost.ExhaustChosen exhaust
            ? ExhaustRequest(world, player, exhaust) : null;
    }

    private static TargetRequest ExhaustRequest(World world, int player, AbilityCost.ExhaustChosen exhaust)
    {
        var cards = Choices(world, player, exhaust).Where(card => card.Ready
            && (!exhaust.CaptureBasicUsePower || world.Agenda.Occurrence?.BasicUse is { } use
                && use.IsCurrent(world) && BasicUseAllyReceipt.Power(world, card, use) > 0)).ToList();
        var range = Range(exhaust.Range, cards.Count);
        return new TargetRequest([.. cards.Select(card => card.ObjectId)], range.Min, range.Max)
        { Details = exhaust.CaptureBasicUsePower && world.Agenda.Occurrence?.BasicUse is { } current
            ? cards.ToDictionary(card => card.ObjectId, card =>
                $"Exhaust {world.Facts.Title(card.FaceId)}; add its current {BasicUseAllyReceipt.Power(world, card, current)} {(current.Field == "attack" ? "ATK" : "THW")} to this basic {current.Field} only; no consequential damage")
            : null };
    }

    internal static IReadOnlyList<Card> DiscardCandidates(World world, int player,
        AbilityCost.ChooseFromDiscard choice) =>
        [.. world.Areas.Where(area => area.Type == DeckType.DiscardPile && area.PlayArea == PlayArea.Of(player))
            .SelectMany(area => area.Cards)
            .Where(card => card.Owner == player && Traits.Has(world, card, choice.Trait, world.Facts))];

    internal static bool BindsChosen(AbilityCost? cost) =>
        cost is AbilityCost.ChooseScheme || DiscardChoice(cost) is not null;

    internal static AbilityCost.ChooseFromDiscard? DiscardChoice(AbilityCost? cost) => cost switch
    {
        AbilityCost.ChooseFromDiscard choice => choice,
        AbilityCost.Sequence sequence => sequence.Costs.Select(DiscardChoice).SingleOrDefault(choice => choice is not null),
        _ => null,
    };

    internal static IReadOnlyList<Card> HandCandidates(World world, Card source, int player) =>
        [.. world.Seats[player].Hand.Cards.Where(card => card != source)];

    internal static IReadOnlyList<Card> Choices(World world, int player, AbilityCost.ExhaustChosen cost) =>
        [.. Choices(world, player, cost.From).Where(card => cost.Trait is null
            || Traits.Has(world, card, cost.Trait, world.Facts))];

    internal static IReadOnlyList<Card> Choices(World world, int player, AbilityCardQuery relation) => relation switch
    {
        AbilityCardQuery.UpgradesOnYourHero => [.. world.Cards.Where(card =>
            card.Area.Type == DeckType.UpgradesArea
            && card.Area.Host == world.Seats[player].IdentityCard.ObjectId
            && CardControl.ControllerOf(world, card) == player
            && EffectiveCards.Kind(card, world.Facts) == CardKind.Upgrade
            && Forms.In(world, world.Seats[player], world.Facts, Forms.Hero))],
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
    internal static string? CounterKeyForRemoval(Card card, string type, long count) =>
        CounterKeyForRemoval(card.InstanceState, type, count, card.FaceId);

    internal static string? CounterKeyForRemoval(
        CardInstanceState state, string type, long count, string faceId)
    {
        if (!string.Equals(type, "allPurpose", StringComparison.Ordinal))
        {
            string typed = "c_" + type;
            return state.Tokens.GetValueOrDefault(typed) >= count ? typed : null;
        }

        string[] pools = [.. state.Tokens
            .Where(pair => pair.Value > 0
                && pair.Key.StartsWith("c_", StringComparison.Ordinal))
            .Select(pair => pair.Key)
            .Order(StringComparer.Ordinal)];
        return pools.Length switch
        {
            0 => null,
            1 when state.Tokens[pools[0]] >= count => pools[0],
            1 => null,
            _ => throw new RulesNotImplementedException(
                $"'{faceId}' must choose which all-purpose counter to remove"),
        };
    }

}
