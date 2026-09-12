using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityConditionProjection;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityPowerProjection;
using static Marvel.Cards.Run.AbilityPowerStateProjection;
using static Marvel.Cards.Run.AbilityProjection;

namespace Marvel.Cards.Run;

internal sealed class VillainConditionTrace(
    Card current, Card next, AbilityAdmissionScope cast,
    HashSet<int> discarded, IReadOnlyDictionary<int, long> threatChanges,
    IReadOnlyDictionary<int, long> damageChanges,
    IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
    IReadOnlyDictionary<int, HashSet<string>> traitChanges,
    IReadOnlySet<(int Card, string Status)> statusChanges,
    IReadOnlyDictionary<int, int> engagementChanges,
    ulong formsMayChange, int traceFirstPlayer)
{
    internal static VillainConditionTrace Create(
        Card current, Card next, AbilityAdmissionScope cast,
        HashSet<int> discarded, IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange, int traceFirstPlayer) =>
        new(current, next, cast, discarded, threatChanges, damageChanges,
            modifierChanges, traitChanges, statusChanges, engagementChanges,
            formsMayChange, traceFirstPlayer);

    internal bool TryTest(AbilityCondition test, out bool result)
    {
        if (LogicalTrace(test) is { } logical)
        {
            result = logical.Value;
            return logical.Known;
        }
        if (TryDirectTest(test, out result)) return true;
        if (TryTraceEnteredCardTest(
            test, cast, discarded, traitChanges, statusChanges, out result)) return true;
        if (TraceTestCard(test) is { } selector
            && Find(selector, cast) is { } absent
            && discarded.Contains(absent.ObjectId))
        {
            result = false;
            return true;
        }
        if (!TestCanChange(test, 16))
        {
            result = Test(test, cast);
            return true;
        }
        result = false;
        return false;
    }

    private TestTrace? LogicalTrace(AbilityCondition test) => test switch
    {
        AbilityCondition.All all => TraceOperands(all.Operands, all: true),
        AbilityCondition.Any any => TraceOperands(any.Operands, all: false),
        AbilityCondition.Negated negated => NegatedTrace(negated.Operand),
        _ => null,
    };

    private TestTrace TraceOperands(
        IEnumerable<AbilityCondition> operands, bool all)
    {
        bool unknown = false;
        foreach (var child in operands)
        {
            if (!TryTest(child, out bool value))
            {
                unknown = true;
                continue;
            }
            if (all && !value || !all && value) return new(true, value);
        }
        return new(!unknown, all);
    }

    private TestTrace NegatedTrace(AbilityCondition operand)
    {
        bool known = TryTest(operand, out bool value);
        return new(known, !value);
    }

    private bool TryDirectTest(AbilityCondition test, out bool result)
    {
        if (test is AbilityCondition.InForm form)
        {
            result = TracedForm(form);
            return true;
        }
        if (test is AbilityCondition.TitleInPlay title)
        {
            result = TracedTitleInPlay(title.Title);
            return true;
        }
        if (test is AbilityCondition.Exists exists
            && TryTraceExists(exists.Cards, out result)) return true;
        if (test is AbilityCondition.AtLeast comparison
            && TryTraceComparison(comparison, out result)) return true;
        if (TryProjectedVillainTest(test, out result)) return true;
        result = false;
        return false;
    }

    private bool TracedForm(AbilityCondition.InForm form)
    {
        int seat = form.Player == AbilityPlayer.FirstPlayer
            ? traceFirstPlayer : Seat(form.Player, cast);
        bool live = Forms.In(
            cast.World, cast.World.Seats[seat], cast.World.Facts, form.Form);
        return SeatMayChange(formsMayChange, seat) ? !live : live;
    }

    private bool TracedTitleInPlay(string title) =>
        string.Equals(cast.World.Facts.Title(next.FaceId), title,
            StringComparison.Ordinal)
        || cast.World.Cards.Any(card => CardHasLiveTitle(card, title));

    private bool CardHasLiveTitle(Card card, string title) =>
        string.Equals(cast.World.Facts.Title(card.FaceId), title,
            StringComparison.Ordinal)
        && (DeckTypes.IsInPlay(card.Area.Type)
            ? !discarded.Contains(card.ObjectId)
            : FacedownDrones.Kind(card, cast.World.Facts) == CardKind.Minion
                && !discarded.Contains(card.ObjectId));

    private bool TryTraceExists(AbilityCardSelection cards, out bool result)
    {
        if (TraceVillainExists(cards, next, cast, discarded) is { } found)
        {
            result = found;
            return true;
        }
        if (TryTraceCount(
            cards, next, cast, discarded, traitChanges, modifierChanges,
            engagementChanges, formsMayChange, out long count))
        {
            result = count > 0;
            return true;
        }
        result = false;
        return false;
    }

    private bool TryTraceComparison(
        AbilityCondition.AtLeast comparison, out bool result)
    {
        if (!TryTraceCountAmount(
                comparison.Value, next, cast, discarded, traitChanges,
                modifierChanges, engagementChanges, formsMayChange, out long value)
            || !TryTraceCountAmount(
                comparison.Count, next, cast, discarded, traitChanges,
                modifierChanges, engagementChanges, formsMayChange, out long count))
        {
            result = false;
            return false;
        }
        result = value >= count;
        return true;
    }

    private bool TryProjectedVillainTest(AbilityCondition test, out bool result)
    {
        if (test is AbilityCondition.CardText
            { Property: AbilityCardTextProperty.Title } text
            && IsProjectedVillainSelector(text.Card))
        {
            result = string.Equals(
                cast.World.Facts.Title(next.FaceId), text.Text, StringComparison.Ordinal);
            return true;
        }
        if (test is AbilityCondition.IsKind kind
            && IsProjectedVillainSelector(kind.Card))
        {
            result = cast.World.Facts.Kind(next.FaceId) == kind.Kind;
            return true;
        }
        result = false;
        return false;
    }

    internal bool AmountCanDiffer(AbilityNumber number, int depth) => number switch
    {
        AbilityNumber.CardValue { Property: AbilityCardNumberProperty.Threat } value =>
            ThreatCanDiffer(value.Card),
        AbilityNumber.Counters => throw new RulesNotImplementedException(
            $"'{cast.Source.FaceId}' reads all-purpose counters in a changing "
            + "constant modifier; projecting those counters is not implemented"),
        AbilityNumber.CardValue { Property: AbilityCardNumberProperty.Damage } value =>
            DamageCanDiffer(value.Card),
        AbilityNumber.CardValue
            { Property: AbilityCardNumberProperty.RemainingHealth } value =>
            HealthCanDiffer(value.Card, depth),
        AbilityNumber.Count count => CountCanDiffer(count.Cards),
        AbilityNumber.Modified modified => ModifiedCanDiffer(modified, depth),
        AbilityNumber.Minimum minimum => minimum.Operands.Any(child => AmountCanDiffer(child, depth)),
        AbilityNumber.Sum sum => sum.Operands.Any(child => AmountCanDiffer(child, depth)),
        AbilityNumber.Product product => product.Operands.Any(child => AmountCanDiffer(child, depth)),
        AbilityNumber.Conditional conditional => ConditionalAmountCanDiffer(conditional, depth),
        _ => false,
    };

    private bool ThreatCanDiffer(AbilityCardSelection cards) =>
        PotentialVillainSelector(cards, cast)
        || TraceEnteredCard(cards, discarded, cast) is not null
        || Find(cards, cast) is { } card
            && (discarded.Contains(card.ObjectId)
                || threatChanges.ContainsKey(card.ObjectId));

    private bool DamageCanDiffer(AbilityCardSelection cards) =>
        PotentialVillainSelector(cards, cast)
        || (Find(cards, cast) ?? TraceEnteredCard(cards, discarded, cast)) is { } card
            && (discarded.Contains(card.ObjectId)
                || damageChanges.ContainsKey(card.ObjectId));

    private bool HealthCanDiffer(AbilityCardSelection cards, int depth) =>
        PotentialVillainSelector(cards, cast)
        || TraceEnteredCard(cards, discarded, cast) is not null
        || Find(cards, cast) is { } card
            && (discarded.Contains(card.ObjectId)
                || damageChanges.ContainsKey(card.ObjectId)
                || modifierChanges.ContainsKey((card.ObjectId, "health"))
                || ConditionalModifierCanDiffer(card, "health", depth));

    private bool CountCanDiffer(AbilityCardSelection cards) =>
        !TryTraceCount(
            cards, next, cast, discarded, traitChanges, modifierChanges,
            engagementChanges, formsMayChange, out long traced)
        || traced != Every(cards, cast).Count;

    private bool ModifiedCanDiffer(AbilityNumber.Modified modified, int depth) =>
        PotentialVillainSelector(modified.Card, cast)
        || TraceEnteredCard(modified.Card, discarded, cast) is not null
        || Find(modified.Card, cast) is { } card
            && ModifiedCardCanDiffer(card, modified.Field, depth);

    private bool ModifiedCardCanDiffer(Card card, string field, int depth) =>
        discarded.Contains(card.ObjectId)
        || modifierChanges.ContainsKey((card.ObjectId, field))
        || damageChanges.ContainsKey(card.ObjectId)
        || traitChanges.ContainsKey(card.ObjectId)
        || statusChanges.Any(change => change.Card == card.ObjectId)
        || ConditionalModifierCanDiffer(card, field, depth);

    private bool ConditionalModifierCanDiffer(Card card, string field, int depth) =>
        AbilityPowerProjection.ConditionalModifierCanDiffer(
            card, field, current, next, cast, discarded, threatChanges,
            damageChanges, modifierChanges, traitChanges, statusChanges,
            engagementChanges, formsMayChange, traceFirstPlayer, depth);

    private bool ConditionalAmountCanDiffer(
        AbilityNumber.Conditional conditional, int depth) =>
        TestCanChange(conditional.Test, depth)
        || AmountCanDiffer(conditional.Then, depth)
        || AmountCanDiffer(conditional.Else, depth);

    internal bool TestCanChange(AbilityCondition test, int depth)
    {
        if (TryTraceEnteredCardTest(
            test, cast, discarded, traitChanges, statusChanges, out bool traced)
            && traced != Test(test, cast)) return true;
        if (test is AbilityCondition.CardText text)
            return CardTextCanChange(text);
        return test switch
        {
            AbilityCondition.All all => all.Operands.Any(child => TestCanChange(child, depth)),
            AbilityCondition.Any any => any.Operands.Any(child => TestCanChange(child, depth)),
            AbilityCondition.Negated negated => TestCanChange(negated.Operand, depth),
            AbilityCondition.InForm form => FormCanChange(form),
            AbilityCondition.TitleInPlay title => TitleCanChange(title.Title),
            AbilityCondition.Exists exists => TraceCardsInPlayMayDiffer(discarded, cast)
                || PotentialVillainSelector(exists.Cards, cast),
            AbilityCondition.IsKind kind => PotentialVillainSelector(kind.Card, cast)
                || Find(kind.Card, cast) is { } card
                    && discarded.Contains(card.ObjectId),
            AbilityCondition.AtLeast comparison => ComparisonCanChange(comparison, depth),
            _ => false,
        };
    }

    private bool FormCanChange(AbilityCondition.InForm form)
    {
        if (form.Player != AbilityPlayer.FirstPlayer)
            return SeatMayChange(formsMayChange, Seat(form.Player, cast));
        return FirstPlayerMayRebind(formsMayChange)
            || traceFirstPlayer != cast.World.FirstPlayer
            || SeatMayChange(formsMayChange, traceFirstPlayer);
    }

    private bool TitleCanChange(string title) =>
        !string.Equals(
            cast.World.Facts.Title(current.FaceId), cast.World.Facts.Title(next.FaceId),
            StringComparison.Ordinal)
        && (string.Equals(title, cast.World.Facts.Title(current.FaceId),
                StringComparison.Ordinal)
            || string.Equals(title, cast.World.Facts.Title(next.FaceId),
                StringComparison.Ordinal))
        || TraceTitlePresenceMayDiffer(title, discarded, cast);

    private bool CardTextCanChange(AbilityCondition.CardText text) =>
        text.Property switch
        {
            AbilityCardTextProperty.Status => PotentialVillainSelector(text.Card, cast)
                || Find(text.Card, cast) is { } card
                    && (discarded.Contains(card.ObjectId)
                        || statusChanges.Contains((card.ObjectId, text.Text))),
            AbilityCardTextProperty.Trait => PotentialVillainSelector(text.Card, cast)
                || Find(text.Card, cast) is { } card
                    && (discarded.Contains(card.ObjectId)
                        || traitChanges.TryGetValue(card.ObjectId, out var traits)
                            && traits.Contains(text.Text)),
            AbilityCardTextProperty.Title => PotentialVillainSelector(text.Card, cast)
                || Find(text.Card, cast) is { } card
                    && discarded.Contains(card.ObjectId),
            _ => false,
        };

    private bool ComparisonCanChange(AbilityCondition.AtLeast comparison, int depth) =>
        ValueReadsVillain(comparison.Value, cast)
        || ValueReadsVillain(comparison.Count, cast)
        || AmountCanDiffer(comparison.Value, depth)
        || AmountCanDiffer(comparison.Count, depth);

    private readonly record struct TestTrace(bool Known, bool Value);
}
