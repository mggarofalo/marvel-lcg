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
using Marvel.Rules.Play;
using Marvel.Rules.State;

using static Marvel.Cards.Run.AbilityConditionProjection;
namespace Marvel.Cards.Run;

internal static class AbilityProjection
{
    internal static IEnumerable<AbilityEffect> StructuralChildren(AbilityEffect effect) => effect switch
    {
        AbilityEffect.Sequence sequence => sequence.Effects,
        AbilityEffect.Simultaneous simultaneous => simultaneous.Effects,
        AbilityEffect.Conditional conditional => ConditionalBranches(conditional),
        AbilityEffect.Dependent dependent => [dependent.Effect, dependent.Continuation],
        AbilityEffect.Power { Kind: AbilityPowerKind.Defense } power => [power.Effect],
        AbilityEffect.ForEach repeated => [repeated.Effect],
        AbilityEffect.EachTime repeated => [repeated.Effect, repeated.Then],
        _ => [],
    };

    internal static bool DiscardTopHasCards(AbilityEffect.DiscardTop discard, AbilityAdmissionScope cast) =>
        discard.Players is { } players
            ? Seats(players, cast).Any(player => cast.World.Seats[player].Deck.Cards.Count > 0)
            : Area(discard.From, cast).Cards.Count > 0;

    internal static bool SelectorMembershipCanChange(AbilityCardSelection selector) => selector switch
    {
        AbilityCardSelection.WithTrait or AbilityCardSelection.EnemiesWithTrait
            or AbilityCardSelection.Ranked or AbilityCardSelection.WithoutAnotherCopyAttached => true,
        AbilityCardSelection.Query query => query.Kind is AbilityCardQuery.AttackableEnemies
            or AbilityCardQuery.MinionsEngagedWithYou or AbilityCardQuery.DronesEngagedWithYou
            or AbilityCardQuery.EnemiesEngagedWithChosenPlayer or AbilityCardQuery.UpgradesYouControl
            or AbilityCardQuery.SupportsYouControl or AbilityCardQuery.UpgradesAndSupportsYouControl,
        _ => false,
    };

    internal static bool PotentialVillainSelector(AbilityCardSelection selector, AbilityAdmissionScope cast)
    {
        if (cast.World.TheCardIn(DeckType.VillainArea) is null) return false;
        return selector switch
        {
            AbilityCardSelection.Query query => query.Kind is AbilityCardQuery.Villain
                or AbilityCardQuery.Enemies or AbilityCardQuery.AttackableEnemies or AbilityCardQuery.Characters,
            AbilityCardSelection.Titled titled => cast.World.AreaOf(DeckType.VillainDeck).Cards
                .Prepend(cast.World.TheCardIn(DeckType.VillainArea)!)
                .Any(stage => string.Equals(titled.Title, cast.World.Facts.Title(stage.FaceId), StringComparison.Ordinal)),
            AbilityCardSelection.EnemiesWithTrait => true,
            AbilityCardSelection.WithTrait filtered => PotentialVillainSelector(filtered.Cards, cast),
            AbilityCardSelection.WithoutAnotherCopyAttached filtered => PotentialVillainSelector(filtered.Cards, cast),
            AbilityCardSelection.Ranked ranked => PotentialVillainSelector(ranked.Cards, cast),
            _ => false,
        };
    }

    internal static List<Card> TraceCandidateCards(AbilityCardSelection selector, AbilityAdmissionScope cast) => selector switch
    {
        AbilityCardSelection.Ranked ranked => TraceCandidateCards(ranked.Cards, cast),
        AbilityCardSelection.WithTrait filtered => TraceCandidateCards(filtered.Cards, cast),
        AbilityCardSelection.WithoutAnotherCopyAttached filtered => TraceCandidateCards(filtered.Cards, cast),
        AbilityCardSelection.EnemiesWithTrait or AbilityCardSelection.Query
        {
            Kind: AbilityCardQuery.Enemies or AbilityCardQuery.AttackableEnemies
                or AbilityCardQuery.MinionsEngagedWithYou or AbilityCardQuery.DronesEngagedWithYou
                or AbilityCardQuery.EnemiesEngagedWithChosenPlayer
        } =>
            [.. cast.World.Areas.SelectMany(area => area.Cards)
                .Where(card => CardKinds.IsEnemy(FacedownDrones.Kind(card, cast.World.Facts)))],
        AbilityCardSelection.Query
        {
            Kind: AbilityCardQuery.UpgradesYouControl
            or AbilityCardQuery.SupportsYouControl or AbilityCardQuery.UpgradesAndSupportsYouControl
        } =>
            [.. cast.World.Areas.Where(area => area.Type is DeckType.UpgradesArea or DeckType.SupportsArea)
                .SelectMany(area => area.Cards)],
        _ => [.. Every(selector, cast)],
    };

    internal static bool TraceSelectorMatches(
        AbilityCardSelection selector, Card candidate, int currentVillain, AbilityAdmissionScope cast,
        HashSet<int> discarded, Dictionary<int, HashSet<string>> traits,
        Dictionary<(int Card, string Field), long> modifiers,
        Dictionary<int, int> engagement) => selector switch
        {
            AbilityCardSelection.Query query => TraceQueryMatches(query.Kind, candidate, currentVillain,
                cast, discarded, traits, modifiers, engagement),
            AbilityCardSelection.Titled titled => string.Equals(titled.Title,
                cast.World.Facts.Title(candidate.FaceId), StringComparison.Ordinal),
            AbilityCardSelection.EnemiesWithTrait filtered =>
                TraceHasTrait(candidate, filtered.Trait, cast, discarded, traits),
            AbilityCardSelection.WithTrait filtered => TraceSelectorMatches(filtered.Cards, candidate,
                    currentVillain, cast, discarded, traits, modifiers, engagement)
                && TraceHasTrait(candidate, filtered.Trait, cast, discarded, traits),
            AbilityCardSelection.WithoutAnotherCopyAttached filtered => TraceSelectorMatches(filtered.Cards, candidate,
                    currentVillain, cast, discarded, traits, modifiers, engagement)
                && !AnotherCopyAttachedInTrace(candidate, cast, discarded),
            AbilityCardSelection.Discardable filtered => TraceSelectorMatches(filtered.Cards, candidate,
                    currentVillain, cast, discarded, traits, modifiers, engagement)
                && (TraceModified(candidate, "permanent", cast, discarded) <= 0
                    || Rules.Play.Discard.SameSet(cast.World.Facts, cast.Source, candidate)),
            AbilityCardSelection.Ranked ranked => TraceRankedSelectorIncludesCard(ranked, candidate,
                currentVillain, cast, discarded, traits, modifiers, engagement),
            _ => false,
        };

    internal static bool TraceRankedSelectorIncludesCard(
        AbilityCardSelection.Ranked ranked, Card candidate, int currentVillain, AbilityAdmissionScope cast,
        HashSet<int> discarded, Dictionary<int, HashSet<string>> traits,
        Dictionary<(int Card, string Field), long> modifiers,
        Dictionary<int, int> engagement)
    {
        if (!TraceSelectorMatches(ranked.Cards, candidate, currentVillain,
                cast, discarded, traits, modifiers, engagement)
            || TraceModified(candidate, "permanent", cast, discarded) > 0
                && !Rules.Play.Discard.SameSet(cast.World.Facts, cast.Source, candidate)) return false;

        var candidates = RankedTraceCandidates(
            ranked.Cards, currentVillain, cast, discarded, traits, modifiers, engagement);
        return TraceRankedCandidatesInclude(candidates, candidate, ranked.By, ranked.Maximum,
            cast, discarded, modifiers);
    }

    private static List<Card> RankedTraceCandidates(
        AbilityCardSelection selector, int currentVillain, AbilityAdmissionScope cast,
        HashSet<int> discarded, Dictionary<int, HashSet<string>> traits,
        Dictionary<(int Card, string Field), long> modifiers,
        Dictionary<int, int> engagement)
    {
        int boardVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        return TraceCandidateCards(selector, cast)
            .Select(card => card.ObjectId == boardVillain
                ? currentVillain >= 0 ? cast.World.Cards[currentVillain] : null : card)
            .Where(card => card is not null).Cast<Card>().DistinctBy(card => card.ObjectId)
            .Where(card => !discarded.Contains(card.ObjectId)
                && TraceSelectorMatches(selector, card, currentVillain,
                    cast, discarded, traits, modifiers, engagement)
                && (TraceModified(card, "permanent", cast, discarded) <= 0
                    || Rules.Play.Discard.SameSet(cast.World.Facts, cast.Source, card)))
            .ToList();
    }

    internal static bool TryTraceCount(
        AbilityCardSelection selector, Card next, AbilityAdmissionScope cast, HashSet<int> discarded,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange, out long count)
    {
        if (selector is AbilityCardSelection.Bound
            { Binding: AbilityCardBinding.YourHero or AbilityCardBinding.YourAlterEgo } formSelector)
            return TryTraceFormCount(formSelector, cast, discarded, formsMayChange, out count);
        if (selector is AbilityCardSelection.Query { Kind: AbilityCardQuery.Heroes })
            return TryTraceHeroCount(cast, discarded, formsMayChange, out count);
        if (CountSelectorFormsMayChange(selector, cast, formsMayChange))
        {
            count = 0;
            return false;
        }
        count = TraceGeneralCount(
            selector, next, cast, discarded, traitChanges,
            modifierChanges, engagementChanges);
        return true;
    }

    private static long TraceGeneralCount(
        AbilityCardSelection selector, Card next, AbilityAdmissionScope cast,
        HashSet<int> discarded,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, int> engagementChanges)
    {
        var traits = traitChanges.ToDictionary(pair => pair.Key,
            pair => new HashSet<string>(pair.Value, StringComparer.Ordinal));
        var modifiers = new Dictionary<(int Card, string Field), long>(modifierChanges);
        var engagement = new Dictionary<int, int>(engagementChanges);
        long count = 0;
        foreach (var card in cast.World.Cards)
        {
            bool projectedInPlay = card.ObjectId == next.ObjectId
                || DeckTypes.IsInPlay(card.Area.Type) && !discarded.Contains(card.ObjectId)
                || !DeckTypes.IsInPlay(card.Area.Type)
                    && FacedownDrones.Kind(card, cast.World.Facts) == CardKind.Minion
                    && !discarded.Contains(card.ObjectId);
            if (projectedInPlay && TraceSelectorMatches(selector, card, next.ObjectId,
                cast, discarded, traits, modifiers, engagement)) count++;
        }
        return count;
    }

    private static bool TryTraceFormCount(
        AbilityCardSelection.Bound selector, AbilityAdmissionScope cast,
        HashSet<int> discarded, ulong formsMayChange, out long count)
    {
        int seat = Resolver(cast);
        var identity = cast.World.Seats[seat].IdentityCard;
        string form = selector.Binding == AbilityCardBinding.YourHero ? Forms.Hero : Forms.AlterEgo;
        bool live = Forms.In(cast.World, cast.World.Seats[seat], cast.World.Facts, form);
        bool traced = SeatMayChange(formsMayChange, seat) ? !live : live;
        count = traced && !discarded.Contains(identity.ObjectId) ? 1 : 0;
        return true;
    }

    private static bool TryTraceHeroCount(
        AbilityAdmissionScope cast, HashSet<int> discarded,
        ulong formsMayChange, out long count)
    {
        count = cast.World.Seats.Select((seat, player) => (seat, player))
            .Count(pair => !discarded.Contains(pair.seat.IdentityCard.ObjectId)
                && (SeatMayChange(formsMayChange, pair.player)
                    != Forms.In(cast.World, pair.seat, cast.World.Facts, Forms.Hero)));
        return true;
    }

    internal static bool TryTraceCountAmount(
        AbilityNumber number, Card next, AbilityAdmissionScope cast, HashSet<int> discarded,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange, out long amount)
    {
        if (number is AbilityNumber.Constant constant)
        {
            amount = constant.Value;
            return true;
        }
        if (number is AbilityNumber.Count count)
        {
            return TryTraceCount(count.Cards, next, cast, discarded, traitChanges,
                modifierChanges, engagementChanges, formsMayChange, out amount);
        }
        AbilityCardSelection? target = number switch
        {
            AbilityNumber.Counters counters => counters.Card,
            AbilityNumber.Modified modified => modified.Card,
            AbilityNumber.CardValue
            {
                Property: AbilityCardNumberProperty.Threat
                or AbilityCardNumberProperty.Damage or AbilityCardNumberProperty.RemainingHealth
            } value => value.Card,
            _ => null,
        };
        if (target is not null && !PotentialVillainSelector(target, cast)
            && Find(target, cast) is { } removed && discarded.Contains(removed.ObjectId)
            && cast.World.Seats.Any(seat => seat.IdentityCard.ObjectId == removed.ObjectId))
        {
            // Amount reads zero after the identity's selector stops finding it.
            // The projected removal has not changed the physical World.
            amount = 0;
            return true;
        }
        var operands = CompositeOperands(number);
        if (!operands.IsDefault)
            return TryTraceCompositeAmount(
                number, operands, next, cast, discarded, traitChanges,
                modifierChanges, engagementChanges, formsMayChange, out amount);
        amount = 0;
        return false;
    }

    private static System.Collections.Immutable.ImmutableArray<AbilityNumber> CompositeOperands(
        AbilityNumber number) => number switch
        {
            AbilityNumber.Minimum minimum => minimum.Operands,
            AbilityNumber.Sum sum => sum.Operands,
            AbilityNumber.Product product => product.Operands,
            _ => default,
        };

    private static bool TryTraceCompositeAmount(
        AbilityNumber number, System.Collections.Immutable.ImmutableArray<AbilityNumber> operands,
        Card next, AbilityAdmissionScope cast, HashSet<int> discarded,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, int> engagementChanges, ulong formsMayChange,
        out long amount)
    {
        var values = new List<long>();
        foreach (var operand in operands)
        {
            if (!TryTraceCountAmount(operand, next, cast, discarded, traitChanges,
                modifierChanges, engagementChanges, formsMayChange, out long traced))
            {
                amount = 0;
                return false;
            }
            values.Add(traced);
        }
        amount = number switch
        {
            AbilityNumber.Minimum => values.Min(),
            AbilityNumber.Sum => values.Sum(),
            _ => values.Aggregate(1L, (product, value) => product * value),
        };
        return true;
    }

    internal static bool CountSelectorFormsMayChange(AbilityCardSelection selector, AbilityAdmissionScope cast, ulong formsMayChange) =>
        selector switch
        {
            AbilityCardSelection.Bound { Binding: AbilityCardBinding.YourHero or AbilityCardBinding.YourAlterEgo } =>
                SeatMayChange(formsMayChange, Resolver(cast)),
            AbilityCardSelection.Query { Kind: AbilityCardQuery.Heroes } =>
                Enumerable.Range(0, cast.World.Seats.Count).Any(seat => SeatMayChange(formsMayChange, seat)),
            AbilityCardSelection.WithTrait filtered => CountSelectorFormsMayChange(filtered.Cards, cast, formsMayChange),
            AbilityCardSelection.Ranked ranked => CountSelectorFormsMayChange(ranked.Cards, cast, formsMayChange),
            AbilityCardSelection.WithoutAnotherCopyAttached filtered => CountSelectorFormsMayChange(filtered.Cards, cast, formsMayChange),
            _ => false,
        };

    internal static bool IsProjectedVillainSelector(AbilityCardSelection selector) =>
        selector is AbilityCardSelection.Query { Kind: AbilityCardQuery.Villain };

    internal static Card? TraceEnteredCard(AbilityCardSelection selector, HashSet<int> discarded, AbilityAdmissionScope cast) =>
        selector is AbilityCardSelection.Titled titled
            ? cast.World.Cards.FirstOrDefault(card => !DeckTypes.IsInPlay(card.Area.Type)
                && !discarded.Contains(card.ObjectId)
                && FacedownDrones.Kind(card, cast.World.Facts) == CardKind.Minion
                && string.Equals(cast.World.Facts.Title(card.FaceId), titled.Title, StringComparison.Ordinal))
            : null;

    internal static bool? TraceVillainExists(AbilityCardSelection selector, Card next, AbilityAdmissionScope cast, HashSet<int> discarded) =>
        selector switch
        {
            AbilityCardSelection.Query
            {
                Kind: AbilityCardQuery.Villain or AbilityCardQuery.Enemies
                or AbilityCardQuery.Characters
            } => true,
            AbilityCardSelection.Titled titled => string.Equals(cast.World.Facts.Title(next.FaceId), titled.Title,
                    StringComparison.Ordinal)
                || cast.World.Areas.Where(area => DeckTypes.IsInPlay(area.Type)).SelectMany(area => area.Cards)
                    .Any(card => !discarded.Contains(card.ObjectId)
                        && string.Equals(cast.World.Facts.Title(card.FaceId), titled.Title, StringComparison.Ordinal)),
            _ => null,
        };

}
