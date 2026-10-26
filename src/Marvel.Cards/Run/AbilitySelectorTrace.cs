using static Marvel.Cards.Run.AbilityRepeatedCharacteristicTrace;
using static Marvel.Cards.Run.AbilityAdmission;
using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityRepeatedSelectorTrace;
using static Marvel.Cards.Run.AbilityRepeatedStatusTrace;

namespace Marvel.Cards.Run;

internal static class AbilitySelectorTrace
{
    internal static bool SelectorReadsStatus(AbilityCardSelection selector) => selector switch
    {
        AbilityCardSelection.WithStatus => true,
        AbilityCardSelection.WithTrait filtered => SelectorReadsStatus(filtered.Cards),
        AbilityCardSelection.InObjectIdOrder ordered => SelectorReadsStatus(ordered.Cards),
        AbilityCardSelection.Last last => SelectorReadsStatus(last.Cards),
        AbilityCardSelection.FaceDown filtered => SelectorReadsStatus(filtered.Cards),
        AbilityCardSelection.FirstAvailable fallback => fallback.Options.Any(option =>
            SelectorReadsStatus(option)),
        AbilityCardSelection.Ranked ranked => SelectorReadsStatus(ranked.Cards),
        AbilityCardSelection.WithMatchingPlayerArea filtered => SelectorReadsStatus(filtered.Cards),
        AbilityCardSelection.WithoutAnotherCopyAttached filtered => SelectorReadsStatus(filtered.Cards),
        AbilityCardSelection.Discardable filtered => SelectorReadsStatus(filtered.Cards),
        _ => false,
    };

    internal static bool CountSelectorFormsMayChange(AbilityCardSelection selector,
        AbilityAdmissionScope cast, ulong formsMayChange) => selector switch
    {
        AbilityCardSelection.Bound { Binding: AbilityCardBinding.YourHero or AbilityCardBinding.YourAlterEgo } =>
            SeatMayChange(formsMayChange, Resolver(cast)),
        AbilityCardSelection.Query { Kind: AbilityCardQuery.Heroes } =>
            Enumerable.Range(0, cast.World.Seats.Count).Any(seat => SeatMayChange(formsMayChange, seat)),
        AbilityCardSelection.WithTrait filtered => CountSelectorFormsMayChange(filtered.Cards, cast, formsMayChange),
        AbilityCardSelection.WithStatus filtered => CountSelectorFormsMayChange(filtered.Cards, cast, formsMayChange),
        AbilityCardSelection.FirstAvailable fallback => fallback.Options.Any(option => CountSelectorFormsMayChange(option, cast, formsMayChange)),
        AbilityCardSelection.Last filtered => CountSelectorFormsMayChange(filtered.Cards, cast, formsMayChange),
        AbilityCardSelection.FaceDown filtered => CountSelectorFormsMayChange(filtered.Cards, cast, formsMayChange),
        AbilityCardSelection.InObjectIdOrder filtered => CountSelectorFormsMayChange(filtered.Cards, cast, formsMayChange),
        AbilityCardSelection.WithMatchingPlayerArea filtered => CountSelectorFormsMayChange(filtered.Cards, cast, formsMayChange),
        AbilityCardSelection.Ranked ranked => CountSelectorFormsMayChange(ranked.Cards, cast, formsMayChange),
        AbilityCardSelection.WithoutAnotherCopyAttached filtered => CountSelectorFormsMayChange(filtered.Cards, cast, formsMayChange),
        _ => false,
    };

    internal static List<Card> TraceCandidateCards(AbilityCardSelection selector, AbilityAdmissionScope cast) => selector switch
    {
        AbilityCardSelection.Query { Kind: AbilityCardQuery.OtherVillainSideSchemes } =>
            [.. AbilityOtherVillainSchemeQuery.Candidates(cast.World)],
        AbilityCardSelection.Query { Kind: AbilityCardQuery.VillainOfAttachedScheme } =>
            [.. AbilityAttachedSchemeVillainQuery.Candidates(cast.World)],
        AbilityCardSelection.Query { Kind: AbilityCardQuery.ActiveVillainSideScheme } =>
            [.. AbilityActiveSchemeQuery.Candidates(cast.World)],
        AbilityCardSelection.Query query when AbilityEncounterSchemeQueries.IsRanked(query.Kind) =>
            [.. (cast.World.EncounterTopology?.Groups
                ?? throw new Marvel.Rules.Play.RulesNotImplementedException("ranked encounter schemes require encounter topology"))
                .SelectMany(group => group.Stages)],
        AbilityCardSelection.Ranked ranked => TraceCandidateCards(ranked.Cards, cast),
        AbilityCardSelection.WithTrait filtered => TraceCandidateCards(filtered.Cards, cast),
        AbilityCardSelection.WithStatus filtered => TraceCandidateCards(filtered.Cards, cast),
        AbilityCardSelection.FirstAvailable fallback => [.. fallback.Options.SelectMany(option => TraceCandidateCards(option, cast)).Distinct()],
        AbilityCardSelection.Last filtered => TraceCandidateCards(filtered.Cards, cast),
        AbilityCardSelection.FaceDown filtered => TraceCandidateCards(filtered.Cards, cast),
        AbilityCardSelection.InObjectIdOrder ordered => [.. TraceCandidateCards(ordered.Cards, cast).OrderBy(card => card.ObjectId)],
        AbilityCardSelection.WithMatchingPlayerArea filtered => TraceCandidateCards(filtered.Cards, cast),
        AbilityCardSelection.WithoutAnotherCopyAttached filtered => TraceCandidateCards(filtered.Cards, cast),
        AbilityCardSelection.Discardable filtered => TraceCandidateCards(filtered.Cards, cast),
        AbilityCardSelection.InPlayerArea { Area: DeckType.EngagedEnemiesArea } =>
            [.. cast.World.Areas.SelectMany(area => area.Cards)
                .Where(card => EffectiveCards.Kind(card, cast.World.Facts) == CardKind.Minion)],
        AbilityCardSelection.Query { Kind: AbilityCardQuery.VillainAndMinionsEngagedWithYou } =>
            [.. cast.World.Areas.SelectMany(area => area.Cards)
                .Where(card => CardKinds.IsEnemy(EffectiveCards.Kind(card, cast.World.Facts)))
                // Engine choice: this simultaneous cohort names the villain before its minions.
                .OrderByDescending(card => CardKinds.IsVillain(EffectiveCards.Kind(card, cast.World.Facts)))],
        AbilityCardSelection.EnemiesWithTrait or AbilityCardSelection.Query
        {
            Kind: AbilityCardQuery.Villains or AbilityCardQuery.Enemies or AbilityCardQuery.AttackableEnemies
                or AbilityCardQuery.MinionsEngagedWithYou or AbilityCardQuery.EnemiesEngagedWithChosenPlayer
        } => [.. cast.World.Areas.SelectMany(area => area.Cards)
            .Where(card => CardKinds.IsEnemy(EffectiveCards.Kind(card, cast.World.Facts)))],
        AbilityCardSelection.Query { Kind: AbilityCardQuery.CardsYouControl } =>
            [.. cast.World.Cards.Where(card => DeckTypes.IsInPlay(card.Area.Type)
                && CardControl.IsPlayerCard(cast.World.Facts, card))],
        AbilityCardSelection.Query
        {
            Kind: AbilityCardQuery.UpgradesYouControl or AbilityCardQuery.SupportsYouControl
                or AbilityCardQuery.UpgradesAndSupportsYouControl
        } => [.. cast.World.Areas.Where(area => area.Type is DeckType.UpgradesArea or DeckType.SupportsArea)
            .SelectMany(area => area.Cards)],
        _ => [.. Every(selector, cast)],
    };

    internal static bool TraceSelectorMatches(
        AbilityCardSelection selector, Card candidate, AbilitySelectorTraceContext trace) => selector switch
    {
        AbilityCardSelection.Query query => TraceQueryMatches(query.Kind, candidate, trace),
        AbilityCardSelection.Titled titled => string.Equals(titled.Title,
            EffectiveCards.Title(candidate, trace.Cast.World.Facts), StringComparison.Ordinal),
        AbilityCardSelection.EnemiesWithTrait filtered => HasTrait(candidate, filtered.Trait, trace),
        AbilityCardSelection.WithTrait or AbilityCardSelection.WithStatus =>
            TraceFilterMatches(selector, candidate, trace),
        AbilityCardSelection.FaceDown filtered => !candidate.FaceUp
            && TraceSelectorMatches(filtered.Cards, candidate, trace),
        AbilityCardSelection.InObjectIdOrder ordered => TraceSelectorMatches(ordered.Cards, candidate, trace),
        AbilityCardSelection.FirstAvailable fallback => fallback.Options.Select(option => MatchingCandidates(option, trace).ToList()).FirstOrDefault(cards => cards.Count > 0)?.Contains(candidate) == true,
        AbilityCardSelection.Last last => MatchingCandidates(last.Cards, trace).LastOrDefault() == candidate,
        AbilityCardSelection.WithoutAnotherCopyAttached filtered => TraceSelectorMatches(filtered.Cards, candidate, trace)
            && !AnotherCopyAttachedInTrace(candidate, trace.Cast, trace.Discarded),
        AbilityCardSelection.Discardable filtered => TraceSelectorMatches(filtered.Cards, candidate, trace)
            && Rules.Play.Discard.CanDiscardFromPlay(candidate, trace.Cast.World.Facts)
            && Removable(candidate, trace),
        AbilityCardSelection.Ranked ranked => RankedIncludes(ranked, candidate, trace),
        AbilityCardSelection.InPlayerArea { Area: DeckType.EngagedEnemiesArea } area =>
            EffectiveCards.Kind(candidate, trace.Cast.World.Facts) == CardKind.Minion
            && !trace.Discarded.Contains(candidate.ObjectId)
            && TraceEngagedWith(candidate, Seat(area.Player, trace.Cast), trace.Engagement),
        AbilityCardSelection.InPlayerArea or AbilityCardSelection.WithMatchingPlayerArea
            or AbilityCardSelection.DefeatedWithProfile or AbilityCardSelection.InAreas
            => Every(selector, trace.Cast).Contains(candidate),
        AbilityCardSelection.Bound bound => BoundMatches(bound, candidate, trace),
        _ => throw new InvalidOperationException("Unknown compiled selector in projected membership"),
    };

    private static bool TraceFilterMatches(AbilityCardSelection selector,
        Card candidate, AbilitySelectorTraceContext trace) => selector switch
    {
        AbilityCardSelection.WithTrait filtered =>
            TraceSelectorMatches(filtered.Cards, candidate, trace)
            && HasTrait(candidate, filtered.Trait, trace) != filtered.Exclude,
        AbilityCardSelection.WithStatus filtered =>
            TraceSelectorMatches(filtered.Cards, candidate, trace)
            && (DeckTypes.IsInPlay(candidate.Area.Type)
                || candidate.ObjectId == trace.CurrentVillain
                || trace.EncounterStages?.Values.Contains(candidate.ObjectId) == true)
            && !trace.Discarded.Contains(candidate.ObjectId)
            && TraceAfflicted(candidate, filtered.Status, trace),
        _ => throw new InvalidOperationException("Unknown compiled card filter in projected membership"),
    };

    private static bool TraceAfflicted(Card candidate, string status,
        AbilitySelectorTraceContext trace)
    {
        int count = trace.StatusCounts?.GetValueOrDefault(
            (candidate.ObjectId, status), Statuses.Count(trace.Cast.World, candidate, status))
            ?? Statuses.Count(trace.Cast.World, candidate, status);
        if (status is Statuses.Stunned or Statuses.Confused)
        {
            if (TraceModified(candidate, "stalwart", trace.Cast, trace.Discarded,
                    trace.Modifiers) > 0) return false;
            return count >= (TraceModified(candidate, "steady", trace.Cast,
                trace.Discarded, trace.Modifiers) > 0 ? 2 : 1);
        }
        return count > 0;
    }

    private static bool BoundMatches(
        AbilityCardSelection.Bound bound, Card candidate, AbilitySelectorTraceContext trace)
    {
        var found = Every(bound, trace.Cast);
        if (trace.EncounterStages is null) return found.Contains(candidate);
        return found.Any(original =>
            AbilityPowerEncounterProjection.Current(original, trace.Cast, trace.EncounterStages) == candidate
            && string.Equals(trace.Cast.World.Facts.Title(original.FaceId),
                trace.Cast.World.Facts.Title(candidate.FaceId), StringComparison.Ordinal));
    }

    private static bool HasTrait(Card candidate, string trait, AbilitySelectorTraceContext trace) =>
        TraceHasTrait(candidate, trait, trace.Cast, trace.Discarded, trace.Traits);

    private static bool Removable(Card candidate, AbilitySelectorTraceContext trace) =>
        TraceModified(candidate, "permanent", trace.Cast, trace.Discarded) <= 0
        || Rules.Play.Discard.SameSet(trace.Cast.World.Facts, trace.Cast.Source, candidate);

    private static bool RankedIncludes(
        AbilityCardSelection.Ranked ranked, Card candidate, AbilitySelectorTraceContext trace)
    {
        if (!TraceSelectorMatches(ranked.Cards, candidate, trace)) return false;
        var candidates = MatchingCandidates(ranked.Cards, trace).ToList();
        return TraceRankedCandidatesInclude(candidates, candidate, ranked.By, ranked.Maximum,
            trace);
    }

    internal static IEnumerable<Card> MatchingCandidates(AbilityCardSelection selector, AbilitySelectorTraceContext trace)
    {
        // Engagement projections retain membership, not move chronology.
        // An explicit object order supplies a complete order; an area's last
        // card after re-engagement requires chronology that this trace lacks.
        if (trace.Engagement.Count > 0 && !UsesObjectIdOrder(selector) && UsesEngagedAreaOrder(selector))
            throw new Rules.Play.RulesNotImplementedException(
                "Last-card area order after projected engagement changes is not implemented");
        int boardVillain = Rules.Play.EncounterRouting.Villain(trace.Cast.World)?.ObjectId ?? -1;
        IEnumerable<Card> source = TraceCandidateCards(selector, trace.Cast);
        if (trace.EncounterStages is not null)
            source = source.Concat(trace.Cast.World.EncounterTopology!.Groups.SelectMany(group => group.Stages));
        var candidates = source
            .Select(card => ProjectedCard(card, boardVillain, trace))
            .Where(card => card is not null).Cast<Card>().DistinctBy(card => card.ObjectId)
            .Where(card => !trace.Discarded.Contains(card.ObjectId)
                && TraceSelectorMatches(selector, card, trace));
        return UsesObjectIdOrder(selector)
            ? candidates.OrderBy(card => card.ObjectId)
            : AbilityEncounterCandidateOrder.Apply(candidates, trace.Cast.World,
                AbilityEncounterSchemeQueries.UsesGroupOrder(selector) ? null : trace.EncounterOrder);
    }

    private static Card? ProjectedCard(Card card, int boardVillain, AbilitySelectorTraceContext trace)
    {
        if (trace.EncounterStages is { } stages)
            return AbilityPowerEncounterProjection.Current(card, trace.Cast, stages);
        if (card.ObjectId != boardVillain) return card;
        return trace.CurrentVillain >= 0 ? trace.Cast.World.Cards[trace.CurrentVillain] : null;
    }

    private static bool UsesEngagedAreaOrder(AbilityCardSelection selector) => selector switch
    {
        AbilityCardSelection.InPlayerArea { Area: DeckType.EngagedEnemiesArea } => true,
        AbilityCardSelection.Query { Kind: AbilityCardQuery.VillainAndMinionsEngagedWithYou or AbilityCardQuery.MinionsEngagedWithYou
            or AbilityCardQuery.EnemiesEngagedWithChosenPlayer } => true,
        AbilityCardSelection.WithTrait filtered => UsesEngagedAreaOrder(filtered.Cards),
        AbilityCardSelection.WithStatus filtered => UsesEngagedAreaOrder(filtered.Cards),
        AbilityCardSelection.FaceDown filtered => UsesEngagedAreaOrder(filtered.Cards),
        AbilityCardSelection.WithMatchingPlayerArea filtered => UsesEngagedAreaOrder(filtered.Cards),
        AbilityCardSelection.WithoutAnotherCopyAttached filtered => UsesEngagedAreaOrder(filtered.Cards),
        AbilityCardSelection.Discardable filtered => UsesEngagedAreaOrder(filtered.Cards),
        AbilityCardSelection.Ranked ranked => UsesEngagedAreaOrder(ranked.Cards),
        _ => false,
    };

    internal static bool UsesObjectIdOrder(AbilityCardSelection selector) => selector switch
    {
        AbilityCardSelection.InObjectIdOrder => true,
        AbilityCardSelection.WithTrait filtered => UsesObjectIdOrder(filtered.Cards),
        AbilityCardSelection.WithStatus filtered => UsesObjectIdOrder(filtered.Cards),
        AbilityCardSelection.FaceDown filtered => UsesObjectIdOrder(filtered.Cards),
        AbilityCardSelection.WithMatchingPlayerArea filtered => UsesObjectIdOrder(filtered.Cards),
        AbilityCardSelection.WithoutAnotherCopyAttached filtered => UsesObjectIdOrder(filtered.Cards),
        AbilityCardSelection.Discardable filtered => UsesObjectIdOrder(filtered.Cards),
        AbilityCardSelection.Ranked ranked => UsesObjectIdOrder(ranked.Cards),
        _ => false,
    };
}
