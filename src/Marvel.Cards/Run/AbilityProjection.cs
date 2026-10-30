using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityChoiceAnalysis;
using static Marvel.Cards.Run.AbilityPowerProjection;
using static Marvel.Cards.Run.AbilityPowerTrace;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityProjection;
using static Marvel.Cards.Run.AbilitySelectorTrace;
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
        AbilityCardSelection.StatusCardsOn => true,
        AbilityCardSelection.FirstAvailable or AbilityCardSelection.FaceDown or AbilityCardSelection.Last or AbilityCardSelection.WithMatchingPlayerArea => true,
        AbilityCardSelection.InObjectIdOrder ordered => SelectorMembershipCanChange(ordered.Cards),
        AbilityCardSelection.InPlayerArea { Area: DeckType.EngagedEnemiesArea } => true,
        AbilityCardSelection.WithTrait or AbilityCardSelection.WithStatus or AbilityCardSelection.EnemiesWithTrait
            or AbilityCardSelection.Ranked or AbilityCardSelection.WithoutAnotherCopyAttached => true,
        AbilityCardSelection.Query query => query.Kind is AbilityCardQuery.AttackableEnemies
            or AbilityCardQuery.VillainAndMinionsEngagedWithYou or AbilityCardQuery.MinionsEngagedWithYou
            or AbilityCardQuery.EnemiesEngagedWithChosenPlayer or AbilityCardQuery.UpgradesYouControl
            or AbilityCardQuery.SupportsYouControl or AbilityCardQuery.UpgradesAndSupportsYouControl
            or AbilityCardQuery.AlliesAndSupportsYouControl
            or AbilityCardQuery.CardsYouControl or AbilityCardQuery.OtherVillainSideSchemes or AbilityCardQuery.ActiveVillainSideScheme or AbilityCardQuery.VillainOfAttachedScheme
            || AbilityEncounterSchemeQueries.IsRanked(query.Kind),
        _ => false,
    };

    internal static bool PotentialVillainSelector(AbilityCardSelection selector, AbilityAdmissionScope cast)
    {
        if (!HasLivingVillain(cast.World)) return false;
        return selector switch
        {
            AbilityCardSelection.Query query => query.Kind is AbilityCardQuery.VillainAndMinionsEngagedWithYou or AbilityCardQuery.Villain
                or AbilityCardQuery.Villains or AbilityCardQuery.Enemies or AbilityCardQuery.AttackableEnemies or AbilityCardQuery.Characters
                || AbilityEncounterSchemeQueries.IsRanked(query.Kind),
            AbilityCardSelection.Titled titled => PotentialStages(cast.World)
                .Any(stage => string.Equals(titled.Title, cast.World.Facts.Title(stage.FaceId), StringComparison.Ordinal)),
            AbilityCardSelection.EnemiesWithTrait => true,
            AbilityCardSelection.WithTrait filtered => PotentialVillainSelector(filtered.Cards, cast),
            AbilityCardSelection.WithStatus filtered => PotentialVillainSelector(filtered.Cards, cast),

            AbilityCardSelection.FirstAvailable fallback => fallback.Options.Any(option => PotentialVillainSelector(option, cast)),
            AbilityCardSelection.Last filtered => PotentialVillainSelector(filtered.Cards, cast),

            AbilityCardSelection.FaceDown filtered => PotentialVillainSelector(filtered.Cards, cast),

            AbilityCardSelection.InObjectIdOrder filtered => PotentialVillainSelector(filtered.Cards, cast),

            AbilityCardSelection.WithMatchingPlayerArea filtered => PotentialVillainSelector(filtered.Cards, cast),
            AbilityCardSelection.WithoutAnotherCopyAttached filtered => PotentialVillainSelector(filtered.Cards, cast),
            AbilityCardSelection.Ranked ranked => PotentialVillainSelector(ranked.Cards, cast),
            _ => false,
        };
    }

    private static bool HasLivingVillain(World world) => world.EncounterTopology is { } topology
        ? topology.Groups.Any(group => group.CurrentVillain is not null)
        : world.TheCardIn(DeckType.VillainArea) is not null;

    private static IEnumerable<Card> PotentialStages(World world) => world.EncounterTopology is { } topology
        ? topology.Groups.SelectMany(group => group.Stages)
        : world.AreaOf(DeckType.VillainDeck).Cards.Prepend(world.TheCardIn(DeckType.VillainArea)!);

    internal static bool TryTraceCount(
        AbilityCardSelection selector, Card next, AbilityAdmissionScope cast,
        VillainTraceChanges changes, ulong formsMayChange, out long count)
    {
        // Villain-transition evidence records status membership, not its count.
        // Steady can change from one status card to two without that flag changing.
        if (SelectorReadsStatus(selector))
        {
            count = 0;
            return false;
        }
        if (selector is AbilityCardSelection.Bound
            { Binding: AbilityCardBinding.YourHero or AbilityCardBinding.YourAlterEgo } formSelector)
            return TryTraceFormCount(formSelector, cast, changes.Discarded, formsMayChange, out count);
        if (selector is AbilityCardSelection.Query { Kind: AbilityCardQuery.Heroes })
            return TryTraceHeroCount(cast, changes.Discarded, formsMayChange, out count);
        if (CountSelectorFormsMayChange(selector, cast, formsMayChange))
        {
            count = 0;
            return false;
        }
        count = TraceGeneralCount(selector, next, cast, changes);
        return true;
    }

    private static long TraceGeneralCount(
        AbilityCardSelection selector, Card next, AbilityAdmissionScope cast,
        VillainTraceChanges changes)
    {
        var traits = changes.Traits.ToDictionary(pair => pair.Key,
            pair => new HashSet<string>(pair.Value, StringComparer.Ordinal));
        var modifiers = new Dictionary<(int Card, string Field), long>(changes.Modifiers);
        var engagement = new Dictionary<int, int>(changes.Engagement);
        long count = 0;
        foreach (var card in cast.World.Cards)
        {
            bool projectedInPlay = card.ObjectId == next.ObjectId
                || DeckTypes.IsInPlay(card.Area.Type) && !changes.Discarded.Contains(card.ObjectId)
                || !DeckTypes.IsInPlay(card.Area.Type)
                    && EffectiveCards.Kind(card, cast.World.Facts) == CardKind.Minion
                    && !changes.Discarded.Contains(card.ObjectId);
            if (projectedInPlay && TraceSelectorMatches(selector, card, new(next.ObjectId,
                cast, changes.Discarded, traits, modifiers, engagement))) count++;
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
        AbilityNumber number, Card next, AbilityAdmissionScope cast,
        VillainTraceChanges changes, ulong formsMayChange, out long amount)
    {
        if (number is AbilityNumber.Constant constant)
        {
            amount = constant.Value;
            return true;
        }
        if (number is AbilityNumber.Count count)
        {
            return TryTraceCount(count.Cards, next, cast, changes, formsMayChange, out amount);
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
            && Find(target, cast) is { } removed && changes.Discarded.Contains(removed.ObjectId)
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
                number, operands, next, cast, changes, formsMayChange, out amount);
        amount = 0;
        return false;
    }

    private static System.Collections.Immutable.ImmutableArray<AbilityNumber> CompositeOperands(
        AbilityNumber number) => number switch
        {
            AbilityNumber.FloorDivide division => division.Operands,
            AbilityNumber.Minimum minimum => minimum.Operands,
            AbilityNumber.Sum sum => sum.Operands,
            AbilityNumber.Product product => product.Operands,
            _ => default,
        };

    private static bool TryTraceCompositeAmount(
        AbilityNumber number, System.Collections.Immutable.ImmutableArray<AbilityNumber> operands,
        Card next, AbilityAdmissionScope cast, VillainTraceChanges changes,
        ulong formsMayChange, out long amount)
    {
        var values = new List<long>();
        foreach (var operand in operands)
        {
            if (!TryTraceCountAmount(operand, next, cast, changes,
                formsMayChange, out long traced))
            {
                amount = 0;
                return false;
            }
            values.Add(traced);
        }
        amount = number switch
        {
            AbilityNumber.FloorDivide => IntegerQuotients.Floor(values[0], values[1]),
            AbilityNumber.Minimum => values.Min(),
            AbilityNumber.Sum => values.Sum(),
            _ => values.Aggregate(1L, (product, value) => product * value),
        };
        return true;
    }

    internal static bool IsProjectedVillainSelector(AbilityCardSelection selector) =>
        selector is AbilityCardSelection.Query { Kind: AbilityCardQuery.Villain };

    internal static Card? TraceEnteredCard(AbilityCardSelection selector, HashSet<int> discarded, AbilityAdmissionScope cast) =>
        selector is AbilityCardSelection.Titled titled
            ? cast.World.Cards.FirstOrDefault(card => !DeckTypes.IsInPlay(card.Area.Type)
                && !discarded.Contains(card.ObjectId)
                && EffectiveCards.Kind(card, cast.World.Facts) == CardKind.Minion
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
