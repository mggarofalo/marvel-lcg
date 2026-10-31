using System.Collections.Immutable;
using System.Globalization;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Counts the boost icons an authored node is about to read.</summary>
/// <remarks>
/// <para>
/// Ruling 6962a015e8597900: "Boost icons are counted whenever a game step or card
/// ability determines the exact number of boost icons on an encounter card."
/// A node that reads boost icons on cards this resolution discarded, or on one
/// bound card, counts each such card once before the node resolves. Ruling
/// 310c4caa404d0f5a makes a "2 or more boost icons" test such a count.
/// </para>
/// <para>
/// Engine choice: the count is made immediately before the outermost node whose
/// text reads it, so a player choosing a target for that text already knows the
/// number. A count no interrupt observes completes inline; an observed count
/// schedules its window and resumes the same node afterwards. Each counted card
/// keeps its number for the rest of this resolution.
/// </para>
/// </remarks>
internal static class AbilityBoostCounting
{
    private const string KeyPrefix = "boostIconsCounted:";

    internal static string Key(Card card) => KeyPrefix + card.ObjectId.ToString(CultureInfo.InvariantCulture);

    /// <summary>The number this resolution counted on the card, or its live icons.</summary>
    internal static long Counted(World world, IReadOnlyDictionary<string, long> results, Card card) =>
        results.TryGetValue(Key(card), out long counted) ? counted : BoostIconCounting.Icons(world, card);

    /// <summary>Whether the card prints a star icon in its boost area.</summary>
    /// <remarks>rr:boost-boost-icon.1: "A star icon is not itself considered a boost icon."</remarks>
    internal static bool HasStarIcon(World world, Card card) =>
        world.Facts.PrintedStats(card.FaceId).TryGetValue("Boost", out var boost) && boost.SpecialStar;

    /// <summary>Count what the node reads; true when the node waits for a count window.</summary>
    internal static bool TrySuspend(AbilityResolutionExecution execution, AbilityEffect node, AbilityResolutionState cast)
    {
        if (!ReadsBoostIcons(node)) return false;
        var cards = Pending(execution, node, cast);
        if (cards.Count == 0) return false;
        BoostIconCount[] counts = [.. cards.Select(card => new BoostIconCount(cast.World, card))];
        if (!counts.Any(count => BoostIconCounting.Interrupted(cast.World, count, cast.Player)))
        {
            foreach (var count in counts)
            {
                count.Freeze();
                cast.Results[Key(count.Original)] = count.Value;
            }
            return false;
        }
        var capture = execution.Capture(cast, execution.AbilityOrdinal(node, cast)) with
        { Reason = AbilityResumeReason.BoostCount, BoostCounts = [.. counts] };
        var continuation = AbilityContinuations.Step(capture, AbilityContinuationStage.Resume,
            cast.World.Agenda.Current?.Round ?? 0, plan: true);
        BoostIconCounting.Schedule(cast.World, counts, cast.Player, continuation);
        cast.Suspend();
        return true;
    }

    /// <summary>The completed counts a resumed node reads.</summary>
    internal static ImmutableDictionary<string, long> Record(
        ImmutableDictionary<string, long> results, ImmutableArray<BoostIconCount> counts)
    {
        foreach (var count in counts)
        {
            count.Freeze();
            results = results.SetItem(Key(count.Original), count.Value);
        }
        return results;
    }

    private static List<Card> Pending(AbilityResolutionExecution execution, AbilityEffect node, AbilityResolutionState cast)
    {
        var cards = new List<Card>();
        foreach (var number in Numbers(node))
            Collect(execution, number, cast, cards);
        return cards;
    }

    private static void Collect(AbilityResolutionExecution execution, AbilityNumber number,
        AbilityResolutionState cast, List<Card> cards)
    {
        IEnumerable<Card> read = number switch
        {
            AbilityNumber.ResolutionValue { Kind: AbilityResolutionNumber.BoostIconsDiscarded } => cast.Discarded,
            AbilityNumber.ResolutionValue { Kind: AbilityResolutionNumber.TopEncounterDiscardBoostPlusOne } =>
                cast.Discarded.Count > 0 ? [cast.Discarded[^1]] : [],
            AbilityNumber.CardValue { Property: AbilityCardNumberProperty.BoostIcons } value =>
                execution.Find(value.Card, cast) is { } card ? [card] : [],
            _ => [],
        };
        foreach (var card in read)
        {
            if (card.Owner == World.Scenario && !cast.Results.ContainsKey(Key(card))
                && !cards.Any(other => ReferenceEquals(other, card)))
                cards.Add(card);
        }
    }

    private static bool ReadsBoostIcons(AbilityEffect node) => Numbers(node).Any(IsBoostRead);

    private static bool IsBoostRead(AbilityNumber number) => number is
        AbilityNumber.ResolutionValue { Kind: AbilityResolutionNumber.BoostIconsDiscarded
            or AbilityResolutionNumber.TopEncounterDiscardBoostPlusOne }
        or AbilityNumber.CardValue { Property: AbilityCardNumberProperty.BoostIcons };

    // Every number in the node's subtree, including those inside its conditions.
    private static IEnumerable<AbilityNumber> Numbers(AbilityEffect effect) =>
        AbilityEffectStructure.EffectNumbers(effect)
            .Concat(AbilityEffectStructure.EffectConditions(effect).SelectMany(Numbers))
            .SelectMany(Flatten)
            .Concat(AbilityEffectStructure.AllEffectChildren(effect).SelectMany(Numbers));

    private static IEnumerable<AbilityNumber> Numbers(AbilityCondition condition) => condition switch
    {
        AbilityCondition.All all => all.Operands.SelectMany(Numbers),
        AbilityCondition.Any any => any.Operands.SelectMany(Numbers),
        AbilityCondition.Negated negated => Numbers(negated.Operand),
        AbilityCondition.AtLeast comparison => [comparison.Value, comparison.Count],
        _ => [],
    };

    private static IEnumerable<AbilityNumber> Flatten(AbilityNumber number) => number switch
    {
        AbilityNumber.Sum sum => sum.Operands.SelectMany(Flatten),
        AbilityNumber.Product product => product.Operands.SelectMany(Flatten),
        AbilityNumber.FloorDivide division => division.Operands.SelectMany(Flatten),
        AbilityNumber.Minimum minimum => minimum.Operands.SelectMany(Flatten),
        AbilityNumber.Conditional conditional => Numbers(conditional.Test).SelectMany(Flatten)
            .Concat(Flatten(conditional.Then)).Concat(Flatten(conditional.Else)),
        _ => [number],
    };
}
