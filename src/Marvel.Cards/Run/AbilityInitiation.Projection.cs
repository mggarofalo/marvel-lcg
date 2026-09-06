using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal static partial class AbilityInitiation
{
    private static IEnumerable<AbilityEffect> StructuralChildren(AbilityEffect effect) => effect switch
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

    private static bool DiscardTopHasCards(AbilityEffect.DiscardTop discard, AbilityAdmissionScope cast) =>
        discard.Players is { } players
            ? Seats(players, cast).Any(player => cast.World.Seats[player].Deck.Cards.Count > 0)
            : Area(discard.From, cast).Cards.Count > 0;

    private static bool SelectorMembershipCanChange(AbilityCardSelection selector) => selector switch
    {
        AbilityCardSelection.WithTrait or AbilityCardSelection.EnemiesWithTrait
            or AbilityCardSelection.Ranked or AbilityCardSelection.WithoutAnotherCopyAttached => true,
        AbilityCardSelection.Query query => query.Kind is AbilityCardQuery.AttackableEnemies
            or AbilityCardQuery.MinionsEngagedWithYou or AbilityCardQuery.DronesEngagedWithYou
            or AbilityCardQuery.EnemiesEngagedWithChosenPlayer or AbilityCardQuery.UpgradesYouControl
            or AbilityCardQuery.SupportsYouControl or AbilityCardQuery.UpgradesAndSupportsYouControl,
        _ => false,
    };

    private static bool PotentialVillainSelector(AbilityCardSelection selector, AbilityAdmissionScope cast)
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

    private static List<Card> TraceCandidateCards(AbilityCardSelection selector, AbilityAdmissionScope cast) => selector switch
    {
        AbilityCardSelection.Ranked ranked => TraceCandidateCards(ranked.Cards, cast),
        AbilityCardSelection.WithTrait filtered => TraceCandidateCards(filtered.Cards, cast),
        AbilityCardSelection.WithoutAnotherCopyAttached filtered => TraceCandidateCards(filtered.Cards, cast),
        AbilityCardSelection.EnemiesWithTrait or AbilityCardSelection.Query
            { Kind: AbilityCardQuery.Enemies or AbilityCardQuery.AttackableEnemies
                or AbilityCardQuery.MinionsEngagedWithYou or AbilityCardQuery.DronesEngagedWithYou
                or AbilityCardQuery.EnemiesEngagedWithChosenPlayer } =>
            [.. cast.World.Areas.SelectMany(area => area.Cards)
                .Where(card => CardKinds.IsEnemy(FacedownDrones.Kind(card, cast.World.Facts)))],
        AbilityCardSelection.Query { Kind: AbilityCardQuery.UpgradesYouControl
            or AbilityCardQuery.SupportsYouControl or AbilityCardQuery.UpgradesAndSupportsYouControl } =>
            [.. cast.World.Areas.Where(area => area.Type is DeckType.UpgradesArea or DeckType.SupportsArea)
                .SelectMany(area => area.Cards)],
        _ => [.. Every(selector, cast)],
    };

    private static bool TraceSelectorMatches(
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

    private static bool TraceRankedSelectorIncludesCard(
        AbilityCardSelection.Ranked ranked, Card candidate, int currentVillain, AbilityAdmissionScope cast,
        HashSet<int> discarded, Dictionary<int, HashSet<string>> traits,
        Dictionary<(int Card, string Field), long> modifiers,
        Dictionary<int, int> engagement)
    {
        if (!TraceSelectorMatches(ranked.Cards, candidate, currentVillain,
                cast, discarded, traits, modifiers, engagement)
            || TraceModified(candidate, "permanent", cast, discarded) > 0
                && !Rules.Play.Discard.SameSet(cast.World.Facts, cast.Source, candidate)) return false;

        int boardVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        var candidates = TraceCandidateCards(ranked.Cards, cast)
            .Select(card => card.ObjectId == boardVillain
                ? currentVillain >= 0 ? cast.World.Cards[currentVillain] : null : card)
            .Where(card => card is not null).Cast<Card>().DistinctBy(card => card.ObjectId)
            .Where(card => !discarded.Contains(card.ObjectId)
                && TraceSelectorMatches(ranked.Cards, card, currentVillain,
                    cast, discarded, traits, modifiers, engagement)
                && (TraceModified(card, "permanent", cast, discarded) <= 0
                    || Rules.Play.Discard.SameSet(cast.World.Facts, cast.Source, card)))
            .ToList();
        return TraceRankedCandidatesInclude(candidates, candidate, ranked.By, ranked.Maximum,
            cast, discarded, modifiers);
    }

    private static bool TryTraceCount(
        AbilityCardSelection selector, Card next, AbilityAdmissionScope cast, HashSet<int> discarded,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange, out long count)
    {
        if (selector is AbilityCardSelection.Bound
            { Binding: AbilityCardBinding.YourHero or AbilityCardBinding.YourAlterEgo } formSelector)
        {
            int seat = Resolver(cast);
            var identity = cast.World.Seats[seat].IdentityCard;
            string form = formSelector.Binding == AbilityCardBinding.YourHero ? Forms.Hero : Forms.AlterEgo;
            bool liveForm = Forms.In(cast.World, cast.World.Seats[seat], cast.World.Facts, form);
            bool tracedForm = SeatMayChange(formsMayChange, seat) ? !liveForm : liveForm;
            count = tracedForm && !discarded.Contains(identity.ObjectId) ? 1 : 0;
            return true;
        }
        if (selector is AbilityCardSelection.Query { Kind: AbilityCardQuery.Heroes })
        {
            count = cast.World.Seats.Select((seat, player) => (seat, player))
                .Count(pair => !discarded.Contains(pair.seat.IdentityCard.ObjectId)
                    && (SeatMayChange(formsMayChange, pair.player)
                        != Forms.In(cast.World, pair.seat, cast.World.Facts, Forms.Hero)));
            return true;
        }
        if (CountSelectorFormsMayChange(selector, cast, formsMayChange))
        {
            count = 0;
            return false;
        }
        var traits = traitChanges.ToDictionary(pair => pair.Key,
            pair => new HashSet<string>(pair.Value, StringComparer.Ordinal));
        var modifiers = new Dictionary<(int Card, string Field), long>(modifierChanges);
        var engagement = new Dictionary<int, int>(engagementChanges);
        count = 0;
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
        return true;
    }

    private static bool TryTraceCountAmount(
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
            AbilityNumber.CardValue { Property: AbilityCardNumberProperty.Threat
                or AbilityCardNumberProperty.Damage or AbilityCardNumberProperty.RemainingHealth } value => value.Card,
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
        var operands = number switch
        {
            AbilityNumber.Minimum minimum => minimum.Operands,
            AbilityNumber.Sum sum => sum.Operands,
            AbilityNumber.Product product => product.Operands,
            _ => default,
        };
        if (!operands.IsDefault)
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
        amount = 0;
        return false;
    }

    private static bool CountSelectorFormsMayChange(AbilityCardSelection selector, AbilityAdmissionScope cast, ulong formsMayChange) =>
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

    private static bool IsProjectedVillainSelector(AbilityCardSelection selector) =>
        selector is AbilityCardSelection.Query { Kind: AbilityCardQuery.Villain };

    private static Card? TraceEnteredCard(AbilityCardSelection selector, HashSet<int> discarded, AbilityAdmissionScope cast) =>
        selector is AbilityCardSelection.Titled titled
            ? cast.World.Cards.FirstOrDefault(card => !DeckTypes.IsInPlay(card.Area.Type)
                && !discarded.Contains(card.ObjectId)
                && FacedownDrones.Kind(card, cast.World.Facts) == CardKind.Minion
                && string.Equals(cast.World.Facts.Title(card.FaceId), titled.Title, StringComparison.Ordinal))
            : null;

    private static bool? TraceVillainExists(AbilityCardSelection selector, Card next, AbilityAdmissionScope cast, HashSet<int> discarded) =>
        selector switch
        {
            AbilityCardSelection.Query { Kind: AbilityCardQuery.Villain or AbilityCardQuery.Enemies
                or AbilityCardQuery.Characters } => true,
            AbilityCardSelection.Titled titled => string.Equals(cast.World.Facts.Title(next.FaceId), titled.Title,
                    StringComparison.Ordinal)
                || cast.World.Areas.Where(area => DeckTypes.IsInPlay(area.Type)).SelectMany(area => area.Cards)
                    .Any(card => !discarded.Contains(card.ObjectId)
                        && string.Equals(cast.World.Facts.Title(card.FaceId), titled.Title, StringComparison.Ordinal)),
            _ => null,
        };

    private static AbilityCardSelection? TraceTestCard(AbilityCondition condition) => condition switch
    {
        AbilityCondition.CardText { Property: AbilityCardTextProperty.Status or AbilityCardTextProperty.Trait
            or AbilityCardTextProperty.Title } text => text.Card,
        AbilityCondition.IsKind kind => kind.Card,
        _ => null,
    };

    private static bool TryTraceEnteredCardTest(
        AbilityCondition condition, AbilityAdmissionScope cast, HashSet<int> discarded,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges, out bool result)
    {
        result = false;
        if (TraceTestCard(condition) is not { } selector
            || TraceEnteredCard(selector, discarded, cast) is not { } card) return false;
        result = condition switch
        {
            AbilityCondition.CardText { Property: AbilityCardTextProperty.Status } text =>
                statusChanges.Contains((card.ObjectId, text.Text)),
            AbilityCondition.CardText { Property: AbilityCardTextProperty.Trait } text =>
                Rules.State.Traits.Has(cast.World, card, text.Text, cast.World.Facts)
                || traitChanges.TryGetValue(card.ObjectId, out var traits) && traits.Contains(text.Text),
            AbilityCondition.CardText { Property: AbilityCardTextProperty.Title } text =>
                string.Equals(cast.World.Facts.Title(card.FaceId), text.Text, StringComparison.Ordinal),
            AbilityCondition.IsKind kind => cast.World.Facts.Kind(card.FaceId) == kind.Kind,
            _ => throw new InvalidOperationException("Unknown compiled test of an entered card"),
        };
        return true;
    }

    private static bool ValueReadsVillain(AbilityCardSelection selector, AbilityAdmissionScope cast) =>
        PotentialVillainSelector(selector, cast) || selector switch
        {
            AbilityCardSelection.WithTrait filtered => ValueReadsVillain(filtered.Cards, cast),
            AbilityCardSelection.WithoutAnotherCopyAttached filtered => ValueReadsVillain(filtered.Cards, cast),
            AbilityCardSelection.Discardable filtered => ValueReadsVillain(filtered.Cards, cast),
            AbilityCardSelection.Ranked ranked => ValueReadsVillain(ranked.Cards, cast),
            _ => false,
        };

    private static bool ValueReadsVillain(AbilityNumber number, AbilityAdmissionScope cast) => number switch
    {
        AbilityNumber.CardValue value => ValueReadsVillain(value.Card, cast),
        AbilityNumber.Counters counters => ValueReadsVillain(counters.Card, cast),
        AbilityNumber.Modified modified => ValueReadsVillain(modified.Card, cast),
        AbilityNumber.Count count => ValueReadsVillain(count.Cards, cast),
        AbilityNumber.Sum sum => sum.Operands.Any(value => ValueReadsVillain(value, cast)),
        AbilityNumber.Minimum minimum => minimum.Operands.Any(value => ValueReadsVillain(value, cast)),
        AbilityNumber.Product product => product.Operands.Any(value => ValueReadsVillain(value, cast)),
        AbilityNumber.Conditional conditional => ValueReadsVillain(conditional.Test, cast)
            || ValueReadsVillain(conditional.Then, cast) || ValueReadsVillain(conditional.Else, cast),
        _ => false,
    };

    private static bool ValueReadsVillain(AbilityCondition condition, AbilityAdmissionScope cast) => condition switch
    {
        AbilityCondition.All all => all.Operands.Any(test => ValueReadsVillain(test, cast)),
        AbilityCondition.Any any => any.Operands.Any(test => ValueReadsVillain(test, cast)),
        AbilityCondition.Negated negated => ValueReadsVillain(negated.Operand, cast),
        AbilityCondition.AtLeast comparison => ValueReadsVillain(comparison.Value, cast) || ValueReadsVillain(comparison.Count, cast),
        AbilityCondition.Exists exists => ValueReadsVillain(exists.Cards, cast),
        AbilityCondition.LegalPractice practice => ValueReadsVillain(practice.Schemes, cast),
        AbilityCondition.AutomaticThwart thwart => ValueReadsVillain(thwart.Scheme, cast),
        AbilityCondition.CardText text => ValueReadsVillain(text.Card, cast),
        AbilityCondition.IsKind kind => ValueReadsVillain(kind.Card, cast),
        AbilityCondition.WasDefeated defeated => ValueReadsVillain(defeated.Card, cast),
        AbilityCondition.IsYourIdentity identity => ValueReadsVillain(identity.Card, cast),
        _ => false,
    };

    private static bool TryTraceConstantTest(
        AbilityCondition test, Card current, Card next, AbilityAdmissionScope cast,
        HashSet<int> discarded, IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange, int traceFirstPlayer, out bool result)
    {
        bool Trace(AbilityCondition child, out bool value) => TryTraceConstantTest(child, current, next, cast,
            discarded, threatChanges, damageChanges, modifierChanges, traitChanges, statusChanges,
            engagementChanges, formsMayChange, traceFirstPlayer, out value);
        var operands = test switch
        {
            AbilityCondition.All all => all.Operands,
            AbilityCondition.Any any => any.Operands,
            _ => default,
        };
        if (!operands.IsDefault)
        {
            bool unknown = false;
            foreach (var child in operands)
            {
                if (!Trace(child, out bool value))
                {
                    unknown = true;
                    continue;
                }
                if (test is AbilityCondition.All && !value || test is AbilityCondition.Any && value)
                {
                    result = value;
                    return true;
                }
            }
            result = test is AbilityCondition.All;
            return !unknown;
        }
        if (test is AbilityCondition.Negated negated)
        {
            bool known = Trace(negated.Operand, out bool inner);
            result = !inner;
            return known;
        }
        if (test is AbilityCondition.InForm form)
        {
            int seat = form.Player == AbilityPlayer.FirstPlayer ? traceFirstPlayer : Seat(form.Player, cast);
            bool live = Forms.In(cast.World, cast.World.Seats[seat], cast.World.Facts, form.Form);
            result = SeatMayChange(formsMayChange, seat) ? !live : live;
            return true;
        }
        if (test is AbilityCondition.TitleInPlay title)
        {
            result = string.Equals(cast.World.Facts.Title(next.FaceId), title.Title, StringComparison.Ordinal)
                || cast.World.Cards.Any(card => string.Equals(cast.World.Facts.Title(card.FaceId), title.Title,
                    StringComparison.Ordinal) && (DeckTypes.IsInPlay(card.Area.Type)
                        ? !discarded.Contains(card.ObjectId)
                        : FacedownDrones.Kind(card, cast.World.Facts) == CardKind.Minion && !discarded.Contains(card.ObjectId)));
            return true;
        }
        if (test is AbilityCondition.Exists exists)
        {
            if (TraceVillainExists(exists.Cards, next, cast, discarded) is { } found)
            {
                result = found;
                return true;
            }
            if (TryTraceCount(exists.Cards, next, cast, discarded, traitChanges, modifierChanges,
                engagementChanges, formsMayChange, out long count))
            {
                result = count > 0;
                return true;
            }
        }
        if (test is AbilityCondition.AtLeast comparison
            && TryTraceCountAmount(comparison.Value, next, cast, discarded, traitChanges, modifierChanges,
                engagementChanges, formsMayChange, out long tracedValue)
            && TryTraceCountAmount(comparison.Count, next, cast, discarded, traitChanges, modifierChanges,
                engagementChanges, formsMayChange, out long tracedCount))
        {
            result = tracedValue >= tracedCount;
            return true;
        }
        if (test is AbilityCondition.CardText { Property: AbilityCardTextProperty.Title } text
            && IsProjectedVillainSelector(text.Card))
        {
            result = string.Equals(cast.World.Facts.Title(next.FaceId), text.Text, StringComparison.Ordinal);
            return true;
        }
        if (test is AbilityCondition.IsKind kind && IsProjectedVillainSelector(kind.Card))
        {
            result = cast.World.Facts.Kind(next.FaceId) == kind.Kind;
            return true;
        }
        if (TryTraceEnteredCardTest(test, cast, discarded, traitChanges, statusChanges, out result)) return true;
        if (TraceTestCard(test) is { } selector && Find(selector, cast) is { } absent
            && discarded.Contains(absent.ObjectId))
        {
            result = false;
            return true;
        }
        if (!TestCanChangeOnVillainAdvance(test, current, next, cast, discarded, threatChanges, damageChanges,
            modifierChanges, traitChanges, statusChanges, engagementChanges, formsMayChange, traceFirstPlayer))
        {
            result = Test(test, cast);
            return true;
        }
        result = false;
        return false;
    }

    private static bool AmountCanDifferInVillainTrace(
        AbilityNumber number, Card current, Card next, AbilityAdmissionScope cast,
        HashSet<int> discarded, IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange, int traceFirstPlayer, int dependencyDepth = 16)
    {
        bool Changes(AbilityNumber child) => AmountCanDifferInVillainTrace(child, current, next, cast,
            discarded, threatChanges, damageChanges, modifierChanges, traitChanges, statusChanges,
            engagementChanges, formsMayChange, traceFirstPlayer, dependencyDepth);
        bool ConditionalModifier(Card card, string field) => ConditionalModifierCanDiffer(card, field,
            current, next, cast, discarded, threatChanges, damageChanges, modifierChanges,
            traitChanges, statusChanges, engagementChanges, formsMayChange, traceFirstPlayer, dependencyDepth);
        return number switch
        {
            AbilityNumber.CardValue { Property: AbilityCardNumberProperty.Threat } value =>
                PotentialVillainSelector(value.Card, cast) || TraceEnteredCard(value.Card, discarded, cast) is not null
                || Find(value.Card, cast) is { } card
                    && (discarded.Contains(card.ObjectId) || threatChanges.ContainsKey(card.ObjectId)),
            AbilityNumber.Counters => throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' reads all-purpose counters in a changing constant modifier; projecting those counters is not implemented"),
            AbilityNumber.CardValue { Property: AbilityCardNumberProperty.Damage } value =>
                PotentialVillainSelector(value.Card, cast)
                || (Find(value.Card, cast) ?? TraceEnteredCard(value.Card, discarded, cast)) is { } card
                    && (discarded.Contains(card.ObjectId) || damageChanges.ContainsKey(card.ObjectId)),
            AbilityNumber.CardValue { Property: AbilityCardNumberProperty.RemainingHealth } value =>
                PotentialVillainSelector(value.Card, cast) || TraceEnteredCard(value.Card, discarded, cast) is not null
                || Find(value.Card, cast) is { } card
                    && (discarded.Contains(card.ObjectId) || damageChanges.ContainsKey(card.ObjectId)
                        || modifierChanges.ContainsKey((card.ObjectId, "health")) || ConditionalModifier(card, "health")),
            AbilityNumber.Count count => !TryTraceCount(count.Cards, next, cast, discarded, traitChanges,
                    modifierChanges, engagementChanges, formsMayChange, out long tracedCount)
                || tracedCount != Every(count.Cards, cast).Count,
            AbilityNumber.Modified modified => PotentialVillainSelector(modified.Card, cast)
                || TraceEnteredCard(modified.Card, discarded, cast) is not null
                || Find(modified.Card, cast) is { } card
                    && (discarded.Contains(card.ObjectId) || modifierChanges.ContainsKey((card.ObjectId, modified.Field))
                        || damageChanges.ContainsKey(card.ObjectId) || traitChanges.ContainsKey(card.ObjectId)
                        || statusChanges.Any(change => change.Card == card.ObjectId) || ConditionalModifier(card, modified.Field)),
            AbilityNumber.Minimum minimum => minimum.Operands.Any(Changes),
            AbilityNumber.Sum sum => sum.Operands.Any(Changes),
            AbilityNumber.Product product => product.Operands.Any(Changes),
            AbilityNumber.Conditional conditional => TestCanChangeOnVillainAdvance(conditional.Test, current, next, cast,
                    discarded, threatChanges, damageChanges, modifierChanges, traitChanges, statusChanges,
                    engagementChanges, formsMayChange, traceFirstPlayer, dependencyDepth)
                || Changes(conditional.Then) || Changes(conditional.Else),
            _ => false,
        };
    }

    private static bool TestCanChangeOnVillainAdvance(
        AbilityCondition test, Card current, Card next, AbilityAdmissionScope cast,
        HashSet<int> discarded, IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange, int traceFirstPlayer, int dependencyDepth = 16)
    {
        bool Changes(AbilityCondition child) => TestCanChangeOnVillainAdvance(child, current, next, cast,
            discarded, threatChanges, damageChanges, modifierChanges, traitChanges, statusChanges,
            engagementChanges, formsMayChange, traceFirstPlayer, dependencyDepth);
        bool ChangesAmount(AbilityNumber number) => AmountCanDifferInVillainTrace(number, current, next, cast,
            discarded, threatChanges, damageChanges, modifierChanges, traitChanges, statusChanges,
            engagementChanges, formsMayChange, traceFirstPlayer, dependencyDepth);
        if (TryTraceEnteredCardTest(test, cast, discarded, traitChanges, statusChanges, out bool traced)
            && traced != Test(test, cast)) return true;
        return test switch
        {
            AbilityCondition.All all => all.Operands.Any(Changes),
            AbilityCondition.Any any => any.Operands.Any(Changes),
            AbilityCondition.Negated negated => Changes(negated.Operand),
            AbilityCondition.InForm { Player: AbilityPlayer.FirstPlayer } =>
                FirstPlayerMayRebind(formsMayChange) || traceFirstPlayer != cast.World.FirstPlayer
                || SeatMayChange(formsMayChange, traceFirstPlayer),
            AbilityCondition.InForm form => SeatMayChange(formsMayChange, Seat(form.Player, cast)),
            AbilityCondition.TitleInPlay title =>
                !string.Equals(cast.World.Facts.Title(current.FaceId), cast.World.Facts.Title(next.FaceId), StringComparison.Ordinal)
                    && (string.Equals(title.Title, cast.World.Facts.Title(current.FaceId), StringComparison.Ordinal)
                        || string.Equals(title.Title, cast.World.Facts.Title(next.FaceId), StringComparison.Ordinal))
                || TraceTitlePresenceMayDiffer(title.Title, discarded, cast),
            AbilityCondition.Exists exists => TraceCardsInPlayMayDiffer(discarded, cast)
                || PotentialVillainSelector(exists.Cards, cast),
            AbilityCondition.CardText { Property: AbilityCardTextProperty.Status } text =>
                PotentialVillainSelector(text.Card, cast)
                || Find(text.Card, cast) is { } card
                    && (discarded.Contains(card.ObjectId) || statusChanges.Contains((card.ObjectId, text.Text))),
            AbilityCondition.CardText { Property: AbilityCardTextProperty.Trait } text =>
                PotentialVillainSelector(text.Card, cast)
                || Find(text.Card, cast) is { } card
                    && (discarded.Contains(card.ObjectId)
                        || traitChanges.TryGetValue(card.ObjectId, out var traits) && traits.Contains(text.Text)),
            AbilityCondition.CardText { Property: AbilityCardTextProperty.Title } text =>
                PotentialVillainSelector(text.Card, cast)
                || Find(text.Card, cast) is { } card && discarded.Contains(card.ObjectId),
            AbilityCondition.IsKind kind => PotentialVillainSelector(kind.Card, cast)
                || Find(kind.Card, cast) is { } card && discarded.Contains(card.ObjectId),
            AbilityCondition.AtLeast comparison => ValueReadsVillain(comparison.Value, cast)
                || ValueReadsVillain(comparison.Count, cast)
                || ChangesAmount(comparison.Value) || ChangesAmount(comparison.Count),
            _ => false,
        };
    }
}
