using static Marvel.Cards.Run.AbilityRepeatedCharacteristicTrace;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityChoiceAnalysis;
using static Marvel.Cards.Run.AbilityPowerProjection;
using static Marvel.Cards.Run.AbilityPowerTrace;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityProjection;
using static Marvel.Cards.Run.AbilityRepeatedEffectAnalysis;
using static Marvel.Cards.Run.AbilityResolutionAdmission;
using static Marvel.Cards.Run.AbilityInitiation;
using static Marvel.Cards.Run.AbilityEffectStructure;
using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using PowerReachability = Marvel.Rules.Play.RuleProjection<Marvel.Cards.Run.AbilityPowerState>;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

using static Marvel.Cards.Run.AbilityPowerStateProjection;
namespace Marvel.Cards.Run;

internal static class AbilityPowerStateProjection
{
    internal static bool ContainsFieldGrant(
        AbilityEffect node, Card source, Card target, string field, AbilityAdmissionScope cast)
    {
        if (node is AbilityEffect.GrantField { Until: null } grant
            && string.Equals(grant.Field, field, StringComparison.Ordinal)
            && ConstantSelectorAffects(
                grant.Cards, source, target, cast))
        {
            return true;
        }
        return StructuralChildren(node).Any(child =>
            ContainsFieldGrant(child, source, target, field, cast));
    }

    internal static bool ConstantSelectorAffects(
        AbilityCardSelection selector, Card source, Card target, AbilityAdmissionScope cast) =>
        selector is AbilityCardSelection.Bound { Binding: AbilityCardBinding.This }
            ? source.ObjectId == target.ObjectId
            : Every(selector, cast).Any(card => card.ObjectId == target.ObjectId);

    internal static bool TraceCardsInPlayMayDiffer(
        HashSet<int> discarded, AbilityAdmissionScope cast) => cast.World.Cards.Any(card =>
            DeckTypes.IsInPlay(card.Area.Type)
                ? discarded.Contains(card.ObjectId)
                : EffectiveCards.Kind(card, cast.World.Facts) == CardKind.Minion
                    && !discarded.Contains(card.ObjectId));


    internal static bool TraceTitlePresenceMayDiffer(
        string title, HashSet<int> discarded, AbilityAdmissionScope cast) =>
        cast.World.Cards.Any(card => string.Equals(
                cast.World.Facts.Title(card.FaceId), title,
                StringComparison.Ordinal)
            && (DeckTypes.IsInPlay(card.Area.Type)
                ? discarded.Contains(card.ObjectId)
                : EffectiveCards.Kind(card, cast.World.Facts) == CardKind.Minion
                    && !discarded.Contains(card.ObjectId)));


    internal static long PowerDamage(AbilityPowerState state, Card card) =>
        state.CardDamage.TryGetValue(card.ObjectId, out long damage)
            ? damage
            : card.Damage;

    internal static long PowerThreat(AbilityPowerState state, Card scheme) =>
        TraceThreat(state.SchemeThreat, scheme);

    internal static long TraceThreat(
        Dictionary<int, long> schemeThreat, Card scheme) =>
        schemeThreat.TryGetValue(scheme.ObjectId, out long threat)
            ? threat
            : scheme.Tokens.GetValueOrDefault("k_threat");

    internal static bool PowerCrisis(AbilityPowerState state, AbilityAdmissionScope cast) =>
        cast.World.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .Where(card => !state.Discarded.Contains(card.ObjectId))
            .Any(card => TraceModified(
                card, "crisis", cast, state.Discarded, state.Modifiers) > 0);

    internal static AbilityPowerState SetPowerThreat(
        AbilityPowerState state, Card scheme, long threat)
    {
        var values = new Dictionary<int, long>(state.SchemeThreat);
        long live = scheme.Tokens.GetValueOrDefault("k_threat");
        if (threat == live)
        {
            values.Remove(scheme.ObjectId);
        }
        else
        {
            values[scheme.ObjectId] = threat;
        }
        return state with { SchemeThreat = values };
    }

    internal static bool PowerTough(
        AbilityPowerState state, Card card, AbilityAdmissionScope cast) =>
        state.CardTough.TryGetValue(card.ObjectId, out bool tough)
            ? tough
            : Statuses.Has(cast.World, card, Statuses.Tough);

    internal static PowerReadiness PowerReady(
        Card card, AbilityPowerState state) =>
        state.CardReadiness.TryGetValue(card.ObjectId, out var readiness)
            ? readiness
            : card.Ready
                ? PowerReadiness.Ready
                : PowerReadiness.Exhausted;

    internal static AbilityPowerState SetPowerReady(
        AbilityPowerState state, Card card, PowerReadiness readiness)
    {
        var cards = new Dictionary<int, PowerReadiness>(state.CardReadiness);
        var live = card.Ready
            ? PowerReadiness.Ready
            : PowerReadiness.Exhausted;
        if (readiness == live)
        {
            cards.Remove(card.ObjectId);
        }
        else
        {
            cards[card.ObjectId] = readiness;
        }
        return state with { CardReadiness = cards };
    }

    internal static long PowerCardsAvailable(
        AbilityPowerState state, int player, AbilityAdmissionScope cast) =>
        state.PlayerCardsAvailable.TryGetValue(player, out long available)
            ? available
            : cast.World.Seats[player].Deck.Cards.Count
                + cast.World.AreaOf(
                    DeckType.DiscardPile, PlayArea.Of(player)).Cards.Count;

    internal static AbilityPowerState SetPowerCardsAvailable(
        AbilityPowerState state, int player, long available, AbilityAdmissionScope cast)
    {
        var cards = new Dictionary<int, long>(state.PlayerCardsAvailable);
        long live = cast.World.Seats[player].Deck.Cards.Count
            + cast.World.AreaOf(
                DeckType.DiscardPile, PlayArea.Of(player)).Cards.Count;
        if (available == live)
        {
            cards.Remove(player);
        }
        else
        {
            cards[player] = available;
        }
        return state with { PlayerCardsAvailable = cards };
    }

    internal static AbilityPowerState SetPowerDamage(
        AbilityPowerState state, Card card, long damage, Card first, AbilityAdmissionScope cast)
    {
        if (damage == PowerDamage(state, card))
        {
            return state;
        }
        var inventory = new Dictionary<int, long>(state.CardDamage);
        if (damage == card.Damage)
        {
            inventory.Remove(card.ObjectId);
        }
        else
        {
            inventory[card.ObjectId] = damage;
        }
        ulong forms = state.FormsMayChange;
        int firstPlayer = state.FirstPlayer;
        var tracedFirst = cast.World.Seats[firstPlayer].IdentityCard;
        if (card == tracedFirst
            && damage >= PowerHealth(state, tracedFirst, cast))
        {
            forms |= FirstPlayerRebinding;
            var changed = state with { CardDamage = inventory };
            for (int offset = 1; offset < cast.World.Seats.Count; offset++)
            {
                int candidate = (firstPlayer + offset) % cast.World.Seats.Count;
                var identity = cast.World.Seats[candidate].IdentityCard;
                if (PowerDamage(changed, identity) < PowerHealth(changed, identity, cast))
                {
                    firstPlayer = candidate;
                    break;
                }
            }
        }
        return state with
        {
            FormsMayChange = forms,
            FirstPlayer = firstPlayer,
            FirstPlayerDamage = card == first ? damage : state.FirstPlayerDamage,
            CardDamage = inventory,
        };
    }

    internal static AbilityPowerState SetPowerTough(
        AbilityPowerState state, Card card, bool tough, Card first, AbilityAdmissionScope cast)
    {
        if (tough == PowerTough(state, card, cast))
        {
            return state;
        }
        var statuses = new Dictionary<int, bool>(state.CardTough);
        bool live = Statuses.Has(cast.World, card, Statuses.Tough);
        if (tough == live)
        {
            statuses.Remove(card.ObjectId);
        }
        else
        {
            statuses[card.ObjectId] = tough;
        }
        return state with
        {
            FirstPlayerTough = card == first ? tough : state.FirstPlayerTough,
            CardTough = statuses,
        };
    }

    internal static long SaturatingAdd(long left, long right) =>
        right > 0 && left > long.MaxValue - right ? long.MaxValue : left + right;

    internal static long SaturatingSubtract(long left, long right)
    {
        if (right > 0 && left < long.MinValue + right)
        {
            return long.MinValue;
        }
        if (right < 0 && left > long.MaxValue + right)
        {
            return long.MaxValue;
        }
        return left - right;
    }

    internal static ulong AllPlayerSeats(AbilityAdmissionScope cast) =>
        cast.World.Seats.Count >= 63
            ? FirstPlayerRebinding - 1
            : (1UL << cast.World.Seats.Count) - 1;

    internal static IEnumerable<AbilityEffect> EachPlayers(AbilityEffect node)
    {
        if (node.OperationName() == "eachPlayer")
        {
            yield return node;
            yield break;
        }
        IEnumerable<AbilityEffect> children = node.OperationName() switch
        {
            "seq" or "and" => OrderedEffects(node),
            "if" => ConditionalBranches((AbilityEffect.Conditional)node).Where(value => value is not null)
                .Select(value => value),
            "then" =>
            [
                EffectBody(node),
                EffectFollowing(node),
            ],
            "otherwise" =>
            [
                EffectBody(node),
                EffectFollowing(node),
            ],
            "forEach" => [EffectBody(node)],
            _ => [],
        };
        foreach (var found in children.SelectMany(EachPlayers))
        {
            yield return found;
        }
    }

}
