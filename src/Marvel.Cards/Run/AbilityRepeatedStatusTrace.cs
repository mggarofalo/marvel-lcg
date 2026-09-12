using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityChoiceAnalysis;
using static Marvel.Cards.Run.AbilityDelayedReachability;
using static Marvel.Cards.Run.AbilityPowerProjection;
using static Marvel.Cards.Run.AbilityPowerTrace;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityProjection;
using static Marvel.Cards.Run.AbilityRepeatedEffectAnalysis;
using static Marvel.Cards.Run.AbilityResolutionAdmission;
using static Marvel.Cards.Run.AbilityInitiation;
using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

using static Marvel.Cards.Run.AbilityRepeatedDamageTrace;
using static Marvel.Cards.Run.AbilityRepeatedDamageAnalysis;
using static Marvel.Cards.Run.AbilityRepeatedSelectorTrace;
using static Marvel.Cards.Run.AbilityRepeatedStatusTrace;
namespace Marvel.Cards.Run;

internal static class AbilityRepeatedStatusTrace
{
    internal static int TraceStatusLimit(
        Card card, string status, AbilityAdmissionScope cast, HashSet<int> discarded,
        Dictionary<(int Card, string Field), long> modifiers)
    {
        if (status is not (Statuses.Stunned or Statuses.Confused))
        {
            return 1;
        }
        if (TraceModified(card, "stalwart", cast, discarded, modifiers) > 0)
        {
            return 0;
        }
        return TraceModified(card, "steady", cast, discarded, modifiers) > 0
            ? 2
            : 1;
    }

    internal static void TraceStatusesLeave(
        int cardId, AbilityAdmissionScope cast,
        Dictionary<(int Card, string Status), int> statusCounts,
        HashSet<(int Card, string Status)> statusChanges)
    {
        var statuses = statusCounts.Keys
            .Where(key => key.Card == cardId)
            .Select(key => key.Status)
            .Concat(cast.World.Areas
                .Where(area => area.Type == DeckType.StatusArea
                    && area.Host == cardId)
                .SelectMany(area => area.Cards)
                .Select(card => card.FaceId))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        foreach (string status in statuses)
        {
            TraceSetStatusCount(
                cast.World.Cards[cardId], status, 0, cast,
                statusCounts, statusChanges);
        }
    }

    internal static void TraceSetStatusCount(
        Card card, string status, int count, AbilityAdmissionScope cast,
        Dictionary<(int Card, string Status), int> statusCounts,
        HashSet<(int Card, string Status)> statusChanges)
    {
        var key = (card.ObjectId, status);
        int live = Statuses.Count(cast.World, card, status);
        if (count == live)
        {
            statusCounts.Remove(key);
        }
        else
        {
            statusCounts[key] = count;
        }
        if ((count > 0) == (live > 0))
        {
            statusChanges.Remove(key);
        }
        else
        {
            statusChanges.Add(key);
        }
    }

    internal static bool TraceStatusMakesVulnerable(
        Card card, string status, int count, int limit, AbilityAdmissionScope cast,
        HashSet<int> discarded,
        Dictionary<(int Card, string Field), long> modifiers) =>
        status is Statuses.Stunned or Statuses.Confused
        && limit > 0
        && count >= limit
        && TraceModified(
            card, "vulnerable", cast, discarded, modifiers) > 0;

    internal static bool AnotherCopyAttachedInTrace(
        Card current, AbilityAdmissionScope cast, HashSet<int> discarded)
    {
        int boardVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        string sourceTitle = cast.World.Facts.Title(cast.Source.FaceId);
        bool carried = boardVillain >= 0 && string.Equals(
            cast.World.Facts.Title(cast.World.Cards[boardVillain].FaceId),
            cast.World.Facts.Title(current.FaceId), StringComparison.Ordinal);
        return cast.World.Areas
            .Where(area => area.Host == current.ObjectId
                || carried && area.Host == boardVillain)
            .SelectMany(area => area.Cards)
            .Any(attached => attached.ObjectId != cast.Source.ObjectId
                && !discarded.Contains(attached.ObjectId)
                && string.Equals(
                    cast.World.Facts.Title(attached.FaceId), sourceTitle,
                    StringComparison.Ordinal));
    }

    internal static bool TraceRankedCandidatesInclude(
        List<Card> candidates, Card candidate, AbilityCardRank rank, bool maximum,
        AbilityAdmissionScope cast, HashSet<int> discarded,
        Dictionary<(int Card, string Field), long> modifiers)
    {
        long Rank(Card card) => rank switch
        {
            AbilityCardRank.Cost => cast.World.Facts.PrintedValue(
                card.FaceId, "Cost", cast.World.Players),
            AbilityCardRank.Attack => TraceModified(
                card, "attack", cast, discarded, modifiers),
            AbilityCardRank.PrintedHealth => FacedownDrones.BaseValue(
                card, cast.World.Facts, "HP", cast.World.Players),
            _ => throw new InvalidOperationException("Unknown compiled rank in a projected selector"),
        };
        long extreme = maximum ? candidates.Max(Rank) : candidates.Min(Rank);
        return Rank(candidate) == extreme;
    }

    internal static long MutationTotal(
        AbilityEffect node, AbilityAdmissionScope cast, RepeatedChange assumed, bool binding,
        long own,
        Func<AbilityEffect, long> childAmount)
    {
        if (node.OperationName() == "if")
        {
            var test = ConditionalOf(node, cast).Test;
            if (RepeatedTestCanChange(test, assumed)
                || binding && BindingCanChange(test))
            {
                long possible = ConditionalBranches((AbilityEffect.Conditional)node)
                    .Where(value => value is not null)
                    .Select(value => childAmount(value))
                    .DefaultIfEmpty(0)
                    .Max();
                return SaturatingSum(own, [possible]);
            }

            bool passes = Test(test, cast);
            long active = ConditionalBranch(node, passes ? "then" : "else") is { } branch
                ? childAmount(branch)
                : 0;
            return SaturatingSum(own, [active]);
        }

        var amounts = MutationChildren(node).Select(childAmount).ToList();

        // The engine chooses one option. Ordered and simultaneous children all
        // resolve, so only those amounts combine.
        long descendants = node.OperationName() switch
        {
            "choose" => amounts.DefaultIfEmpty(0).Max(),
            "forEach" => SaturatingMultiply(
                amounts.SingleOrDefault(), ForEachCount(node, cast)),
            _ => SaturatingSum(0, amounts),
        };
        return SaturatingSum(own, [descendants]);
    }

    internal static long SaturatingSum(long own, IEnumerable<long> rest)
        => AbilityAmounts.SaturatingSum(own, rest);

    internal static long SaturatingMultiply(long amount, long multiplier)
    {
        if (amount <= 0 || multiplier <= 0)
        {
            return 0;
        }
        return amount > long.MaxValue / multiplier
            ? long.MaxValue
            : amount * multiplier;
    }

    internal static bool CanExhaust(
        long amountPerFrame, int frames, long remaining) =>
        amountPerFrame > 0 && frames > 0
        && amountPerFrame >= (remaining + frames - 1) / frames;

    internal static IEnumerable<AbilityEffect> MutationChildren(AbilityEffect node) =>
        node.OperationName() is "attack" or "thwart"
            ? [EffectBody(node)]
            : ContinuationChildren(node);

    internal static IEnumerable<AbilityEffect> ContinuationChildren(AbilityEffect node) =>
        node.OperationName() switch
        {
            "choose" => ((AbilityEffect.Choose)node).Options,
            "chooseCard" or "eachPlayer" or "forEach" =>
                [EffectBody(node)],
            "eachTime" =>
            [
                EffectBody(node),
                EffectFollowing(node),
            ],
            "afterActivation" => [EffectBody(node)],
            "payOrEffect" or "payOrExhaust" => [EffectFollowing(node)],
            "thwartSchemes" or "thwartDifferentSchemes" or "legalPractice" =>
                [((AbilityEffect.ThwartGroup)node).Thwart],
            _ => ResolutionChildren(node),
        };

    /// <summary>Whether this player-card effect can remove any threat.</summary>
    internal static bool CanRemoveThreat(AbilityEffect node, AbilityAdmissionScope cast)
    {
        var scheme = Find(ThreatSelectionOf(node, cast), cast);
        return scheme is not null
            && scheme.Tokens.GetValueOrDefault("k_threat") > 0
            && Amount(EffectOf<AbilityEffect.RemoveThreat>(node, cast).Amount, cast) > 0
            && CanRemoveThreatFrom(node, cast, scheme);
    }

    internal static bool CanRemoveThreatFrom(AbilityEffect node, AbilityAdmissionScope cast, Card scheme) =>
        AbilityProgramQueries.CanRemoveThreat(
            cast.World, cast.Context.Program, scheme,
            OverriddenThreatRemovalSource(node, cast))
        && (IgnoresCrisis(node, cast)
            || scheme.Area.Type != DeckType.MainSchemesArea
            || !IsPlayerCard(cast)
            || !MainScheme.Crisis(cast.World, cast.World.Facts));

    internal static int OverriddenThreatRemovalSource(AbilityEffect node, AbilityAdmissionScope cast) =>
        EffectOf<AbilityEffect.RemoveThreat>(node, cast).OverridesCannotFrom is { } source
            ? Find(source, cast)?.ObjectId ?? -1
            : -1;

    internal static bool IgnoresCrisis(AbilityEffect node, AbilityAdmissionScope cast) =>
        EffectOf<AbilityEffect.RemoveThreat>(node, cast).IgnoresCrisis;

    /// <summary>Whether at least one named player can draw a card.</summary>
    internal static bool CanDraw(AbilityEffect node, AbilityAdmissionScope cast) =>
        EffectOf<AbilityEffect.Draw>(node, cast) is var draw
        && draw.Count > 0
        && Seats(draw.Players, cast).Any(player =>
            CanDraw(cast.World, player));

    internal static bool CanDraw(World world, int player) =>
        world.Seats[player].Deck.Cards.Count > 0;

    /// <summary>Whether a search names at least one searchable game area.</summary>
    internal static bool HasSearchableArea(AbilityEffect node, AbilityAdmissionScope cast)
    {
        var search = EffectOf<AbilityEffect.Search>(node, cast);
        if (search.Areas.IsEmpty)
        {
            return false;
        }

        var searchedAreas = search.Areas
            .Select(value => Area(value, cast))
            .ToList();

        var searched = SearchAreaTypes(node, cast);
        if (cast.Reachability.CheckingInitiation
            && (cast.Reachability.PriorSteps.Any(step =>
                    MayChangeAnyArea(step, searched, cast))
                || cast.Reachability.PaymentCost is { } cost
                    && CostMayChangeAnyArea(cost, searched, cast)))
        {
            if (cast.Reachability.FilteringContinuationOption)
            {
                return false;
            }
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' searches an area after its matching "
                + "cards may change");
        }

        string wanted = search.Face;
        int matches = searchedAreas.SelectMany(area => area.Cards)
            .Count(card => string.Equals(
                card.FaceId, wanted, StringComparison.Ordinal));
        if (matches > 1)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' searched and found {matches} copies of "
                + $"'{wanted}'; rr:search.1 gives the player that choice and asking is "
                + "not implemented");
        }
        return true;
    }

}
